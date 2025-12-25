// 0324dcea
call SixLabors.ImageSharp.Configuration SixLabors.ImageSharp.Configuration::get_Default()
newobj System.Void SixLabors.Memory.SimpleGcMemoryAllocator::.ctor()
callvirt System.Void SixLabors.ImageSharp.Configuration::set_MemoryAllocator(SixLabors.Memory.MemoryAllocator)
ret
