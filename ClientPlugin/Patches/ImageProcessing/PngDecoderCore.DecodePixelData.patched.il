// b93d414f
IL_0000: br IL_0100
IL_0005: ldarg.1
IL_0006: ldarg.0
IL_0007: ldfld SixLabors.Memory.IManagedByteBuffer SixLabors.ImageSharp.Formats.Png.PngDecoderCore::scanline
IL_0012: callvirt System.Byte[] SixLabors.Memory.IManagedByteBuffer::get_Array()
IL_0017: ldarg.0
IL_0018: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::currentRowBytesRead
IL_0023: ldarg.0
IL_0024: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::bytesPerScanline
IL_0029: ldarg.0
IL_0030: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::currentRowBytesRead
IL_0035: sub
IL_0036: callvirt System.Int32 System.IO.Stream::Read(System.Byte[], System.Int32, System.Int32)
IL_0041: stloc.0
IL_0042: ldarg.0
IL_0043: ldarg.0
IL_0044: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::currentRowBytesRead
IL_0049: ldloc.0
IL_0050: add
IL_0051: stfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::currentRowBytesRead
IL_0056: ldarg.0
IL_0057: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::currentRowBytesRead
IL_0062: ldarg.0
IL_0063: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::bytesPerScanline
IL_0068: bge.s IL_0047
IL_0070: br IL_0005
IL_0071: ldarg.0
IL_0072: ldc.i4.0
IL_0073: stfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::currentRowBytesRead
IL_0078: ldarg.0
IL_0079: ldfld SixLabors.Memory.IManagedByteBuffer SixLabors.ImageSharp.Formats.Png.PngDecoderCore::scanline
IL_0084: call System.Span<T> SixLabors.ImageSharp.Memory.MemoryOwnerExtensions::GetSpan<System.Byte>(System.Buffers.IMemoryOwner<T>)
IL_0089: stloc.1
IL_0090: ldloca.s 1 (System.Span<System.Byte>)
IL_0092: ldc.i4.0
IL_0093: call !0& System.Span<System.Byte>::get_Item(System.Int32)
IL_0098: ldind.u1
IL_0099: stloc.2
IL_0100: ldloc.2
IL_0101: switch IL_00de, IL_0080, IL_008e, IL_00a1, IL_00ba
IL_0126: br.s IL_00d3
IL_0128: ldloc.1
IL_0129: ldarg.0
IL_0130: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::bytesPerPixel
IL_0135: call System.Void SixLabors.ImageSharp.Formats.Png.Filters.SubFilter::Decode(System.Span<System.Byte>, System.Int32)
IL_0140: br.s IL_00de
IL_0142: ldloc.1
IL_0143: ldarg.0
IL_0144: ldfld SixLabors.Memory.IManagedByteBuffer SixLabors.ImageSharp.Formats.Png.PngDecoderCore::previousScanline
IL_0149: call System.Span<T> SixLabors.ImageSharp.Memory.MemoryOwnerExtensions::GetSpan<System.Byte>(System.Buffers.IMemoryOwner<T>)
IL_0154: call System.Void SixLabors.ImageSharp.Formats.Png.Filters.UpFilter::Decode(System.Span<System.Byte>, System.Span<System.Byte>)
IL_0159: br.s IL_00de
IL_0161: ldloc.1
IL_0162: ldarg.0
IL_0163: ldfld SixLabors.Memory.IManagedByteBuffer SixLabors.ImageSharp.Formats.Png.PngDecoderCore::previousScanline
IL_0168: call System.Span<T> SixLabors.ImageSharp.Memory.MemoryOwnerExtensions::GetSpan<System.Byte>(System.Buffers.IMemoryOwner<T>)
IL_0173: ldarg.0
IL_0174: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::bytesPerPixel
IL_0179: call System.Void SixLabors.ImageSharp.Formats.Png.Filters.AverageFilter::Decode(System.Span<System.Byte>, System.Span<System.Byte>, System.Int32)
IL_0184: br.s IL_00de
IL_0186: ldloc.1
IL_0187: ldarg.0
IL_0188: ldfld SixLabors.Memory.IManagedByteBuffer SixLabors.ImageSharp.Formats.Png.PngDecoderCore::previousScanline
IL_0193: call System.Span<T> SixLabors.ImageSharp.Memory.MemoryOwnerExtensions::GetSpan<System.Byte>(System.Buffers.IMemoryOwner<T>)
IL_0198: ldarg.0
IL_0199: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::bytesPerPixel
IL_0204: call System.Void SixLabors.ImageSharp.Formats.Png.Filters.PaethFilter::Decode(System.Span<System.Byte>, System.Span<System.Byte>, System.Int32)
IL_0209: br.s IL_00de
IL_0211: ldstr "Unknown filter type."
IL_0216: newobj System.Void SixLabors.ImageSharp.ImageFormatException::.ctor(System.String)
IL_0221: throw
IL_0222: ldarg.0
IL_0223: ldloc.1
IL_0224: call System.ReadOnlySpan<!0> System.Span<System.Byte>::op_Implicit(System.Span<!0>)
IL_0229: ldarg.2
IL_0230: ldarg.3
IL_0231: call System.Void SixLabors.ImageSharp.Formats.Png.PngDecoderCore::ProcessDefilteredScanline<TPixel>(System.ReadOnlySpan<System.Byte>, SixLabors.ImageSharp.ImageFrame<TPixel>, SixLabors.ImageSharp.Formats.Png.PngMetaData)
IL_0236: ldarg.0
IL_0237: call System.Void SixLabors.ImageSharp.Formats.Png.PngDecoderCore::SwapBuffers()
IL_0242: ldarg.0
IL_0243: ldarg.0
IL_0244: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::currentRow
IL_0249: ldc.i4.1
IL_0250: add
IL_0251: stfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::currentRow
IL_0256: ldarg.0
IL_0257: ldfld System.Int32 SixLabors.ImageSharp.Formats.Png.PngDecoderCore::currentRow
IL_0262: ldarg.0
IL_0263: ldflda SixLabors.ImageSharp.Formats.Png.PngHeader SixLabors.ImageSharp.Formats.Png.PngDecoderCore::header
IL_0268: call System.Int32 SixLabors.ImageSharp.Formats.Png.PngHeader::get_Height()
IL_0273: blt IL_0005
IL_0278: ret
