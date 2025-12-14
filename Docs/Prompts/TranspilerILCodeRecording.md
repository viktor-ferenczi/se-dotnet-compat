Add the `RecordOriginalCode` and `RecordPatchedCode` calls to the transpiler patches as shown on the example below:

```csharp
    // ReSharper disable once UnusedMember.Local
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(MyPhysics.LoadData))]
    private static IEnumerable<CodeInstruction> LoadDataTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod)
    {
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);
        
        // KEEP THE COMMENTS HERE

        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}
```

Make sure to add the `MethodBase patchedMethod` parameter if it is missing.

Do not implement the transpilers yet. Keep all the existing comments inside the transpilers at the marked position.
