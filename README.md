# .NET 10.0 compatibility for Space Engineers

Provides some of the performance benefits of the newer .NET runtime without having to recompile the game.

This repository ships **two** plugins:

- [`ClientPlugin/`](ClientPlugin/) — for the Space Engineers **game client**, loaded by
  [Pulsar](https://github.com/SpaceGT/Pulsar).
- [`ServerPlugin/`](ServerPlugin/README.md) — for the Space Engineers **Dedicated Server**,
  loaded by Magnetar. UI / audio / render-only patches are skipped on the server. 
  See [ServerPlugin/README.md](ServerPlugin/README.md) for details.

Common patches, rewriters, and tools live in [`Shared/`](Shared/) and compile directly into both plugin assemblies.

## Prerequisites

### Client

- [Space Engineers](https://store.steampowered.com/app/244850/Space_Engineers/)
- [.NET 10.0 Runtime or SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Pulsar](https://github.com/SpaceGT/Pulsar)

### Dedicated Server

- Space Engineers Dedicated Server
- [.NET 10.0 Runtime or SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- TBD: Magnetar

## How to use

- **Client**: run the `Interim.exe` binary of Pulsar.
- **Server**: run the `Interim.exe` binary of Magnetar.

## Credits

- `SpaceGT` for his contribution in fixing a lot of issues with the .NET 10 port, especially mod support. 

## Bug reports

Please start a support thread on the [Pulsar Discord](https://discord.gg/z8ZczP2YZY)
