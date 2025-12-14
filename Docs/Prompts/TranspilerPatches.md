Some patches have "Implement transpiler" in their comments, but they are _Prefix patches. Rename those to _Transpiler and add the parameters and infrastructure required for transpiler patches. Keep the comments already in the patch method's body intact.

Example transpiler patch:
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
