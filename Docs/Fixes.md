# Fixes Log

Record of fixes verified by the full test suite.

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
fallback, also re-investigate the type-table mismatch — the working
hypothesis is that TC promotion drives lazy assembly loads that some
startup path depends on for assembly enumeration, and TC=0 freezes
that enumeration to a smaller set.

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
