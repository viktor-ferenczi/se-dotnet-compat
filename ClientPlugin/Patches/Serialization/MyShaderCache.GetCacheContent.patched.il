// f18f560e
call static System.String VRage.FileSystem.MyFileSystem::get_ContentPath()
ldstr "ShaderCache"
ldarg.0
ldarg.1
call static System.String VRage.Render11.Shader.MyShaderCache::GetCacheFilepath(System.String basePath, System.String subdirName, System.String hashForFilename, VRageRender.MyShaderProfile profile)
call static System.IO.FileStream System.IO.File::OpenRead(System.String path)
stloc.0
[EX_BeginException]
ldloc.0
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
stloc.1
ldloc.1
conv.ovf.i
newarr System.Byte
stloc.2
ldloc.0
ldloc.2
ldc.i4.0
ldloc.1
conv.i4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
pop
ldloc.2
call static System.Byte[] VRage.Render11.Shader.MyShaderCache::SkipHeader(System.Byte[] sourceFile)
stloc.3
leave.s L0
[EX_BeginFinally]
ldloc.0
brfalse.s L1
ldloc.0
callvirt abstract virtual System.Void System.IDisposable::Dispose()
L1:
[EX_EndException]
endfinally
L0:
ldloc.3
ret
