# Fixes Log

Record of fixes verified by the full test suite.

## 2026-05-26: Replication type-table off by two on DS (`System.Delegate` / `System.MulticastDelegate`)

**Symptom**: Multiplayer join fails immediately with the client log
line `Bad number of types from server. Received 712, have 714`. The
client's diagnostic dump (added in the `Serialize` prefix's mismatch
path) lists the two extras as exactly `System.Delegate` and
`System.MulticastDelegate`. The DS log shows no replication error of
its own — it serializes 712 entries and disconnects only when the
client throws.

**Diagnosis**: Both `IsSerializableClass` prefix and a postfix on
`MyReplicationLayerBase.RegisterFromAssembly` were tried first. The
diagnostic log lines added inside them confirmed the postfix fired on
the client (size `714 -> 714`, a no-op — the client already had
them) but never fired at all on the DS. Cross-referencing the DS log
timeline made the cause obvious:

| Time         | Event                                                           |
|--------------|-----------------------------------------------------------------|
| 00:28:30.886 | `MyMultiplayerBase - START/END` etc. — `MyTypeTable.RegisterType` log emissions during `Preallocate`. Type table populated to 712 entries. |
| 00:28:30.912 | `Preallocate - END` — type table is final.                      |
| 00:28:32.469 | `MySandboxGame.Initialize() - START`                            |
| 00:28:38.444 | `MySandboxGame.Initialize() - END`                              |
| 00:28:38.494 | `Plugin Init: Pulsar.Legacy.Loader.PluginLoader` — Harmony patches finally applied, ~8 s after `MyTypeTable.RegisterFromAssemblies` returned. |

So both the `IsSerializableClass` prefix and the `RegisterFromAssembly`
postfix have been silently inert on the DS the whole time. On the
client the `IsSerializableClass` prefix does work because Magnetar's
installer bootstraps plugins early enough to be in place before the
scan — that asymmetry is why the bug presents as "client has 2 more
than server", never the other way around.

This also closes the speculation in the 2026-05-01 entry that TC=0
was "freezing assembly enumeration to a smaller set". The real cause
isn't enumeration — it's that the DS plugin loader runs Init after
the type table is final, so nothing the plugin patches at scan time
can take effect. TC=0 only made the bug more visible by altering which
delegate-derived types `CreateBaseType` happened to walk up from.

**Fix**: Two cooperating patches in
[`ServerPlugin/Patches/Miscellaneous/MyTypeTablePatch.cs`](../ServerPlugin/Patches/Miscellaneous/MyTypeTablePatch.cs):

1. An `IsSerializableClass` prefix that returns `true` for
   `System.Delegate` / `System.MulticastDelegate`. Per the timeline
   above, this prefix is inert against the scan-time
   `RegisterFromGameAssemblies` pass on the DS — but it IS active for
   any later call into `MyTypeTable.Register`, which is what step 2
   relies on. (The first version of this fix shipped only step 2 on
   the DS and the lazy `Register` call no-op'd because the un-patched
   `IsSerializableClass` gate in
   [`MyTypeTable.Register`](../../.../VRage/Network/MyTypeTable.cs)
   rejected both types — the symptom in the DS log was
   `[DotNetCompat] Lazy-registered Delegate/MulticastDelegate in
   Serialize: typeTable size 712 -> 712` instead of `712 -> 714`.)

2. A `Serialize` prefix that, on the first wire write, calls
   `Register(typeof(Delegate))` and `Register(typeof(MulticastDelegate))`.
   The first `Serialize` happens at client-join time, well after
   plugin Init, so the `IsSerializableClass` prefix from step 1 is in
   place by then and `Register` accepts the two types into
   `m_idToType` / `m_hashLookup` / `m_typeLookup`. Once they're in
   `m_typeLookup`, subsequent joins skip the work via the
   `TryGetValue` short-circuit at the top of `Register`. A one-shot
   `_delegateTypesRegistered` flag just suppresses repeat log lines.

The wire format is a hash list and the client reorders its own
`m_idToType` to match server hash order, so it doesn't matter that
the appended entries land at indices 712/713 on the server while the
client has them at different positions.

The client-side `IsSerializableClass` prefix in
[`ClientPlugin/Patches/Miscellaneous/MyTypeTablePatch.cs`](../ClientPlugin/Patches/Miscellaneous/MyTypeTablePatch.cs)
is retained — that's the mechanism that puts the two types into the
client's table to begin with, and it does run early enough there. The
`Serialize` wrapper with `LogTypeTableMismatch` is also kept on the
client; it was the diagnostic that named the two missing types and is
cheap insurance for future drifts.

The earlier attempt — `MyReplicationLayerBaseDelegateRegistrationPatch`,
a postfix on `MyReplicationLayerBase.RegisterFromAssembly` — was
deleted from both sides. On the DS it never fired; on the client it
was a redundant `714 -> 714` no-op.

**Rule of thumb for the next maintainer**: on the DS,
`Pulsar.Legacy.Loader.PluginLoader` runs `Plugin Init` *after*
`MyTypeTable.RegisterFromAssemblies`. Patches on
`IsSerializableClass`, `MyTypeTable.RegisterType`, or
`MyReplicationLayerBase.RegisterFromAssembly` will not influence the
initial scan-time table on the DS — anything that needs to add
entries must drive a later `Register` call itself (e.g. from a
`Serialize` prefix). The `IsSerializableClass` prefix above is not
sufficient on its own on the DS, but it IS necessary alongside the
lazy `Register` call: without it the gate inside `MyTypeTable.Register`
rejects `Delegate` / `MulticastDelegate` and the lazy add silently
no-ops. The only place the failure becomes visible is at multiplayer
join when the client receives a hash list shorter than its own table.

**Verification**: After the fix, the DS log must contain
`[DotNetCompat] Lazy-registered Delegate/MulticastDelegate in Serialize: typeTable size 712 -> 714`
once per process lifetime, and the `Bad number of types from server`
exception must be gone. If the log instead shows `712 -> 712`, the
`IsSerializableClass` prefix isn't loaded on the DS Harmony instance
— most likely because the deployed plugin DLL is `ServerPlugin`-only
and someone removed the prefix from `ServerPlugin/Patches/Miscellaneous/MyTypeTablePatch.cs`,
or because the DS is running an older release that pre-dates this
two-part fix. Treat any future recurrence of "Received N, have N+2"
with `System.Delegate` / `System.MulticastDelegate` named in the
client's mismatch diagnostic as a regression of this fix.

## 2026-05-01: Intermittent JIT SIGSEGV during parallel preload (MonoMod hook on .NET 10)

**Symptom**: Game randomly fails to finish loading on startup. The
last log line is `Plugin Init: Pulsar.Legacy.Loader.PluginLoader` and
the process is killed by `SIGSEGV` a couple of seconds later. The
process appears as `[Interim] <defunct>` in `ps` because the IDE that
launched it (Rider's debugger host) hasn't reaped the child yet — a
zombie is just an exit-status placeholder, the process is already
dead. Reproduction is non-deterministic: it happens "from time to
time" on cold start, more often when launched from the IDE than from
a terminal.

**Diagnosis** (core dump from PID 167509, 2026-05-01 20:04:17, signal
11/SEGV). Crash thread managed stack from `dotnet-dump analyze`:

```
ParallelTasks worker thread (parallel preload of vanilla audio)
  Sandbox.MySandboxGame+<>c__DisplayClass196_0.<PerformPreloading>b__0(int)
  VRage.Audio.MyXAudio2.Preload(string)
  VRage.Audio.MyInMemoryWaveDataCache.Preload / Load
  VRage.FileSystem.MyFileSystem.OpenRead
  VRage.FileSystem.MyFileSystem.Open_Patch1            (Harmony stub)
  ClientPlugin.Patches.PathHandling.MyFileSystemOpenPatch.Prefix
  ClientPlugin.Patches.PathHandling.PathCache.ResolveAbsolute
  PathCache.WalkFromRoot / GetOrRefresh / Populate
  System.IO.Directory.InternalEnumeratePaths
  System.IO.Enumeration.FileSystemEnumerableFactory.UserEntries
  FileSystemEnumerable<T>..ctor
  FileSystemEnumerator<T>.Init
  System.Buffers.SharedArrayPool<char>.Rent
  StaticsHelpers.GetGCThreadStaticBase                 (lazy thread-static init)
  StaticsHelpers.GetGCThreadStaticsByIndexSlow
  StaticsHelpers.GetThreadStaticsByIndex
  <PrestubMethodFrame> StaticsHelpers.<GetThreadStaticsByIndex>g____PInvoke|0_0
                                                       (LibraryImportGenerator-emitted IL stub,
                                                        being JIT-compiled for the first time)
  ILStubClass.IL_STUB_ReversePInvoke                   (CoreCLR -> hook)
  MonoMod.Core.Platforms.Runtimes.Core60Runtime+JitHookDelegateHolder.CompileMethodHook
  MonoMod.Core.Interop.CoreCLR+V60.InvokeCompileMethod
  ILStubClass.IL_STUB_PInvoke                          (hook -> back into native compileMethod)
  ===> SIGSEGV inside libclrjit.so + 0x2e62cf
```

The `MyFileSystemOpenPatch` / `PathCache` frames belong to the
sibling `se-linux-compat` plugin and are incidental to this bug —
they happen to sit on top of `Directory.Enumerate*` in our Linux
build, but the underlying race is generic to .NET 10 + MonoMod and
not Linux-specific.

**Root cause**: MonoMod's JIT hook (the mechanism Harmony uses to
intercept method bodies before the JIT writes them) is built on
runtime-version-specific descriptors of CoreCLR's `ICorJitCompiler`
vtable. The newest descriptor MonoMod ships is `V60` (the .NET 6
layout). On .NET 10.0.5 — which is what Pulsar runs under — MonoMod
falls back to the V60 shim against a runtime whose internal
`compileMethod` ABI / synchronization contract has shifted, and the
JIT random-access SEGVs when called back through the hook.

The reason the parallel audio preload reliably triggers it is
contingent rather than fundamental:

1. `MySandboxGame.PerformPreloading` runs an XAudio2 preload pass on
   `ParallelTasks` worker threads, so the first time `Directory.Enumerate*`
   is reached from a non-main thread happens here.
2. The first call into `Directory.InternalEnumeratePaths` from a worker
   triggers lazy JIT compilation of the `LibraryImportGenerator`-emitted
   P/Invoke IL stub for the runtime's thread-static helpers (CoreCLR's
   `<GetThreadStaticsByIndex>g____PInvoke|0_0`).
3. JIT-compiling that stub re-enters MonoMod's `CompileMethodHook` from
   the CoreCLR side via `IL_STUB_ReversePInvoke`, the hook then
   forwards to the native `compileMethod` — and the V60 shim's contract
   no longer matches what .NET 10's JIT expects, so the JIT faults.

Any first-time `Directory.Enumerate*` from a worker thread on .NET 10
with MonoMod's V60 hook installed is exposed; the call path through
`PathCache` is just one such trigger in the Linux build.

The IDE-vs-terminal asymmetry is a JIT timing artifact: the debugger
host adds breakpoint / symbol-load latency that nudges thread-static
init and `compileMethod` re-entry into a worse interleaving. Same
class of bug shows up under `taskset -c 0` and high-load cold starts
from a terminal.

**Fix**: JIT-prewarm the Directory.Enumerate* call chain on the main
thread before any parallel preload runs. See
[`Preloader.PrewarmDirectoryEnumerationStubs`](../ClientPlugin/Preloader.cs)
called as the first statement of `Preloader.Finish()`. A single
`Directory.EnumerateFiles(...).GetEnumerator().MoveNext()` is enough
to compile the entire chain (FileSystemEnumerator<T> generic
specialization → SharedArrayPool<char>.Rent → StaticsHelpers
thread-static init → the LibraryImportGenerator P/Invoke stub) on the
main thread. The JIT'd IL stub is process-wide, so subsequent
first-touches from worker threads run the already-compiled code and
never re-enter `compileMethod`. The race window is closed at its
source instead of papering over it with TC=0.

**Why not `DOTNET_TieredCompilation=0`** (the previous workaround,
removed): it caused unrelated assembly-scan timing changes that
broke client-server replication type registration — `System.Delegate`
and `System.MulticastDelegate` (replication TypeIds 0/1) and at least
one `MyObjectBuilder_*` type silently dropped out of the client's
type tables, which manifested at world-join time as a stack overflow
from infinite recursion in `MyRuntimeObjectBuilderId.ToString` (the
`KeyNotFoundException` formatter calls `ToString` on the missing
key). Bisected to commit `733144b8` (TC=0 introduction). Removing the
env var restored type registration; the pre-warm above keeps the
original SIGSEGV at bay without touching JIT tiering.

If `DOTNET_TieredCompilation=0` ever needs to come back as a
fallback, the type-table mismatch it triggered is now understood —
see the 2026-05-26 entry. The root cause is plugin-Init timing on
the DS (the loader runs after `MyTypeTable.RegisterFromAssemblies`),
not TC-driven assembly enumeration. TC=0 only changed which
delegate-derived types `CreateBaseType` walked up from. The lazy
`Register` in `MyTypeTable.Serialize` handles the symptom directly
and is independent of TC, so re-enabling TC=0 should not bring the
mismatch back; if it does, look for new scan-time patches added since
that assume plugin Init runs before `Preallocate`.

**Upstream root cause** (what would actually fix this rather than
work around it):

The unsynchronized state is **not** anything in `Directory.Enumerate*`
itself — that API only walks kernel inodes via `getdents64` and is
fully thread-safe. The race is one layer down, in **CoreCLR's
per-method JIT compilation state machine** — specifically the
`MethodDesc` / `CodeVersionManager` transition `not-yet-compiled →
being-compiled → compiled` for each method. CoreCLR already
serializes that transition internally with a per-method lock taken
inside `EEJitManager` around the call to `compileMethod`.

The bug is that **MonoMod's `Core60Runtime.JitHookDelegateHolder`
interposes itself in front of `compileMethod` and is unaware of the
lock layout .NET 10 actually uses**. The holder keeps its own
bookkeeping (the `InvokeCompileMethod` thunk's state, vtable-slot
swap, managed-delegate trampoline) under the assumption of the V6-era
ordering: "vtable slot is patched once at install time → managed
delegate runs single-threaded relative to a given `MethodDesc` →
native callback returns synchronously to the same caller". On .NET 10
the JIT manager can call back into the hook re-entrantly across
threads at points the V6 contract didn't expose:

- During tier-1 promotion of a method while another thread is in the
  prestub of a different method that transitively triggers the same
  P/Invoke stub.
- During lazy generation of a `LibraryImportGenerator`-emitted
  `<MethodName>g____PInvoke|0_0` stub from a worker thread while the
  main thread holds an unrelated EE lock.
- During reverse-P/Invoke entry (`IL_STUB_ReversePInvoke`) where
  CoreCLR re-enters managed code from a callback path the V60 holder
  doesn't expect to be re-entered from.

The holder's bookkeeping isn't guarded against any of those, so the
hook returns control to the JIT in an inconsistent state and the JIT
faults inside `libclrjit` somewhere downstream of `compileMethodHook`.

**Concretely, the fix-it-properly target is upstream MonoMod:**

1. **A `Core100Runtime` subclass** with a `V100` interop layout
   reflecting .NET 10's actual `ICorJitCompiler` vtable, calling
   conventions, and synchronization expectations (the V60 layout is
   from the .NET 6 era and has drifted across every release since).
2. **A per-`ICorJitCompiler*` lock inside the `JitHookDelegateHolder`**
   matching what `EEJitManager` now expects, so reentrant calls from
   the JIT manager are serialized at the holder boundary instead of
   racing through the holder's mutable state.
3. Audit of every field on `JitHookDelegateHolder` for cross-thread
   visibility (volatile / Interlocked / explicit memory barriers
   where currently a plain field is read across threads).

Watch the MonoMod repo for `Core60Runtime` → `Core100Runtime` (or a
generalized version-aware base). Once that lands and Harmony picks
it up, the pre-warm in `Preloader.Finish()` becomes redundant and
should be pruned. Until then, **any** "compile a JIT stub for the
first time on a non-main thread" is an exposed race; the pre-warm
just closes the one path we know about (Directory enumeration via
PathCache via MyXAudio2). New first-touches added by future code can
re-expose the bug — so when adding plugin code that runs on a worker
thread and calls into a previously-unused chunk of BCL, prefer
adding a corresponding pre-warm line over hoping the chain happens
to be JIT'd elsewhere.

**Verification**: Pending — the original bug is intermittent on cold
start, so several days of rotated cold-starts from both IDE and
terminal are needed to call this verified. Treat any new SEGV with
`MonoMod.Core.Platforms.Runtimes.Core*Runtime+JitHookDelegateHolder.CompileMethodHook`
on the crash thread as a regression of this same root cause, and
verify the pre-warm log line `[DotNetCompat] Pre-warmed
Directory.Enumerate* JIT stubs on main thread` appeared before
`Plugin Init: Pulsar.Legacy.Loader.PluginLoader`.

**Diagnostic recipe** (for the next time a similar JIT crash lands):

```bash
coredumpctl list --since "1 hour ago"               # find the dump
coredumpctl info <PID>                              # native stacks
coredumpctl dump <PID> -o /tmp/core.<PID>           # extract raw core
dotnet-dump analyze /tmp/core.<PID> \
    -c "threads" \
    -c "setthread 0" \
    -c "clrstack" -c "exit"                         # crash-thread managed stack
```

The crash thread is index 0 in `dotnet-dump`. If `clrstack` shows
`MonoMod.Core.Platforms.Runtimes.Core*Runtime+JitHookDelegateHolder.CompileMethodHook`
above an `IL_STUB_PInvoke`, you're looking at this same bug.
