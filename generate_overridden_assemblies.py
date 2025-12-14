r""" Prompt was:

Write a new Python script in the project's root which does this:

Has these DLL mappings:

```
NUGET_PACKAGES = {...see below...}
```

Finds all DLL files under C:\Users\viktor\.nuget\package recursively and stores them in a mapping by name, both with and without the .dll extension.
Store them by name and version in a dictionary for lookup.

On stdout prints a mapping of all the assemblies listed in NUGET_PACKAGES in the following format:

```cs
assemblyOverrides["System.Management"] = @"C:\Users\viktor\.nuget\packages\system.management\4.5.0\lib\netstandard2.0\System.Management.dll";
```

Make sure to choose the DLL path with the requested version number. The version numbers were defined by the values of NUGET_PACKAGES.  

This mean one C# code line like above for each item in NUGET_PACKAGES. The path must point to the assembly found in the .nuget\packages folder.

"""

import re
from pathlib import Path

NUGET_PACKAGES = {
    'System.Buffers': (
        '<PackageReference Include="System.Buffers" Version="4.5.1" />',
    ),
    'System.Memory': (
        '<PackageReference Include="System.Memory" Version="4.5.5" />',
    ),
    'System.Management': (
        '<PackageReference Include="System.Management" Version="4.5.0" />',
    ),
    'System.Management.dll': (
        '<PackageReference Include="System.Management" Version="4.5.0" />',
    ),
    'System.ComponentModel.Annotations': (
        '<PackageReference Include="System.ComponentModel.Annotations" Version="4.6.0" />',
    ),
    'System.Runtime.CompilerServices.Unsafe': (
        '<PackageReference Include="System.Runtime.CompilerServices.Unsafe" Version="6.0.0" />',
    ),
    'System.Collections.Immutable': (
        '<PackageReference Include="System.Collections.Immutable" Version="8.0.0" />',
    ),
    'System.Configuration.Install': (
        '<PackageReference Include="Core.System.Configuration.Install" Version="1.1.0" />',
    ),
    'ProtoBuf.Net': (
        '<PackageReference Include="protobuf-net" Version="3.0.131" />',
    ),
    'ProtoBuf.Net.Core': (
        '<PackageReference Include="protobuf-net.Core" Version="3.0.131" />',
    ),
    'SharpDX': (
        '<PackageReference Include="SharpDX" Version="4.2.0" />',
    ),
    'SharpDX.XAudio2': (
        '<PackageReference Include="SharpDX.XAudio2" Version="4.2.0" />',
    ),
    'SharpDX.Desktop': (
        '<PackageReference Include="SharpDX.Desktop" Version="4.2.0" />',
    ),
    'SharpDX.Direct3D11': (
        '<PackageReference Include="SharpDX.Direct3D11" Version="4.2.0" />',
    ),
    'SharpDX.DirectInput': (
        '<PackageReference Include="SharpDX.DirectInput" Version="4.2.0" />',
    ),
    'SharpDX.DXGI': (
        '<PackageReference Include="SharpDX.DXGI" Version="4.2.0" />',
    ),
    'SharpDX.D3DCompiler': (
        '<PackageReference Include="SharpDX.D3DCompiler" Version="4.2.0" />',
    ),
    'SharpDX.XInput': (
        '<PackageReference Include="SharpDX.XInput" Version="4.2.0" />',
    ),
    'Microsoft.CodeAnalysis': (
        '<PackageReference Include="Microsoft.CodeAnalysis" Version="4.11.0" />',
    ),
    'Microsoft.CodeAnalysis.CSharp': (
        '<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.11.0" />',
    ),
    'DirectShowLib': (
        '<PackageReference Include="DirectShowLib" Version="1.0.0" />',
    ),
    'GameAnalytics.Mono': (
        '<PackageReference Include="GameAnalytics.Mono.SDK" Version="3.3.5" />',
    ),
    'RestSharp': (
        '<PackageReference Include="RestSharp" Version="106.6.10" />',
    ),
    'Steamworks.NET': (
        '<PackageReference Include="Steamworks.NET" Version="20.1.0" />',
    ),
    'SixLabors.Core': (
    ),
    'SixLabors.ImageSharp': (
        '<PackageReference Include="SixLabors.ImageSharp" Version="3.1.5" />',
    ),
    'Newtonsoft.Json': (
        '<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />',
    ),
}


def main():
    nuget_path = Path(r"C:\Users\viktor\.nuget\packages")
    dll_paths = {}

    # Find all DLL files recursively and store by name and version
    for dll_path in nuget_path.rglob('*.dll'):
        name = dll_path.name
        version = dll_path.parents[2].name  # version is the directory two levels up
        if name not in dll_paths:
            dll_paths[name] = {}
        dll_paths[name][version] = str(dll_path)
        if name.endswith('.dll'):
            name_no_ext = name[:-4]
            if name_no_ext not in dll_paths:
                dll_paths[name_no_ext] = {}
            dll_paths[name_no_ext][version] = str(dll_path)

    # Print the overriddenAssemblies mappings
    for key in sorted(NUGET_PACKAGES):
        package_refs = NUGET_PACKAGES[key]
        if not package_refs:
            print(f'// No package reference for {key}')
            continue
        package_ref = package_refs[0]
        match = re.search(r'Version="([^"]+)"', package_ref)
        if not match:
            print(f'// No version found in package reference for {key}')
            continue
        version = match.group(1)
        if key in dll_paths and version in dll_paths[key]:
            path = dll_paths[key][version]
            print(f'assemblyOverrides["{key}"] = @"{path}";')
        else:
            print(f'// DLL not found: {key} version {version}')


if __name__ == "__main__":
    main()
