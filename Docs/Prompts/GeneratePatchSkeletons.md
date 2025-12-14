Your task is to generate the skeletons of Harmony patches.

Implement all patches for each separate game class in a separate patch class.
Each patch class must go into its own source file.

You must generate patches for each modified method according to the
game code changes you can find in the `ChangesForPulsar.patch` file.

When the change does a null check (or `.?`) on a `MySomething.Static`,
then implement a prefix patch to skip the entire method.

Most crash reporting and statistics telemetry are disabled. These methods
ended up with empty body, only returning a default value or maybe setting
out variables. Patch these out with prefix patches, make sure to fill the
return value and any out variables accordingly.

Enclose each patch into an `#if` block. The symbols controlling those
blocks should correspond to the category of the fix. Put all patches
corrsponding to the same library or game subsystem under the control
of the same defined symbol.

Some known categories of patches will only be needed if we have to
upgrade these libraries to their newer versions:
- ProtoBuf (slight API changes)
- SharpDX (due to audio API change)
- SixLabors (image processing API changes)

If a method body is short and you would need a traspiler patch, then just
copy the original method body (you can clean it up), modify it according
to the change required, clean up and use as a complete method replacement
with a prefix patch.

If you absolutely must need a transpiler patch, then do not implement it,
just put a TODO comment into the traspiler method's body. You still need
to have a #define block and the skeleton for the patch ready to be filled.

Ignore the changes wrapped in `#if !DEBUG` or `#if DEBUG`.
