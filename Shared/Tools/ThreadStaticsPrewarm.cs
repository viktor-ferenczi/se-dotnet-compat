using System;
using System.Threading;

namespace Shared.Tools;

// .NET 10 resolves thread statics through a managed QCall IL stub
// (StaticsHelpers.GetThreadStaticsByIndex). MonoMod's JIT hook runs managed
// code inside compileMethod and reads its own [ThreadStatic] entrancy guard,
// so compiling that stub with the hook installed re-enters the JIT for the
// same stub; one compilation frees the stub's token map and the other then
// dereferences the ILGeneratedAndFreed sentinel and segfaults
// (TokenLookupMap::LookupMethodDef in CEEInfo::resolveToken).
// Compiling the stub before the first Harmony patch installs the JIT hook
// removes the crash window for the rest of the process lifetime.
public static class ThreadStaticsPrewarm
{
    [ThreadStatic]
    private static object _gcSlot;

    [ThreadStatic]
    private static int _nonGcSlot;

    public static void Run()
    {
        try
        {
            // Only a thread with no thread-static bases allocated yet takes
            // the slow path that needs the stub, so use a fresh thread.
            var thread = new Thread(TouchThreadStatics) { IsBackground = true };
            thread.Start();
            thread.Join();

            Console.WriteLine("[DotNetCompat] Pre-warmed the thread-statics JIT stub");
        }
        catch (Exception ex)
        {
            // Failure is harmless, but leaves the JIT hook crash window open.
            Console.WriteLine(
                $"[DotNetCompat] Pre-warm of thread statics failed: {ex.GetType().Name}: {ex.Message}"
            );
        }
    }

    private static void TouchThreadStatics()
    {
        _gcSlot = new object();
        _nonGcSlot = _gcSlot.GetHashCode();
    }
}
