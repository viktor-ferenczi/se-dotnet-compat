# .NET 10.0 compatibility for Space Engineers

Provides some of the performance benefits of the newer .NET runtime without having to recompile the game.

This repository ships **two** plugins:
- [`ClientPlugin/`](ClientPlugin/) — for the Space Engineers **game client**, loaded by [Pulsar](https://github.com/SpaceGT/Pulsar).
- [`ServerPlugin/`](ServerPlugin/) — for the Space Engineers **Dedicated Server**, loaded by [Magnetar](https://github.com/CometWorks/magnetar).

Common patches, rewriters, and tools used by both plugin assemblies can be found in [`Shared/`](Shared/).

UI / audio / render-only patches are skipped on the server.

## Prerequisites

### Client

- [Space Engineers](https://store.steampowered.com/app/244850/Space_Engineers/)
- [.NET 10.0 Runtime or SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Pulsar](https://github.com/SpaceGT/Pulsar)

### Server

- Space Engineers Dedicated Server
- [.NET 10.0 Runtime or SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Magnetar](https://github.com/CometWorks/magnetar)

## How to use

- **Client**: run the `Interim.exe` binary of Pulsar.
- **Server**: run the `Interim.exe` binary of Magnetar.

## Development

Local build paths (`Bin64`, `DS64`, `Pulsar`, `Magnetar`) are empty in `Directory.Build.props`.
To override them, copy its first `PropertyGroup` into `Directory.Build.props.user` (git-ignored)
in the repo root, wrapped in a top-level `<Project>` element, and fill in your paths.

`Bin64` and `DS64` are auto-detected from Steam if left empty.

## Credits

- `SpaceGT` for his contribution in fixing a lot of issues with the .NET 10 port, especially mod support. 

## Bug reports

Please start a support thread on the [Pulsar Discord](https://discord.gg/z8ZczP2YZY)
