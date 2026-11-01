# Monster Prom Accessibility: developer notes

A BepInEx 5 plugin (32-bit, .NET Framework 4.6) for Monster Prom. Speech goes through Prism, loaded from `prism.dll` next to the plugin.

## Building

You need the .NET SDK (version 6 or later), and Monster Prom installed with BepInEx, because the build uses the game's own files and BepInEx's files from the game folder.

From the repository folder:

```
dotnet build MonsterPromAccessibility.csproj -c Release
```

If the game isn't in the default Steam folder, add the game folder:

```
dotnet build MonsterPromAccessibility.csproj -c Release -p:GameDir="D:\Games\Monster Prom"
```

The built mod is in `bin\Release\net46`.

The game's Mono runtime is old (Unity 2017, .NET 2.0 profile): `[DllImport("prism.dll")]` fails even after the library is loaded, so the Prism wrapper binds its exports with `GetProcAddress` delegates.

## Packaging and releasing

Build the release zip:

```
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1
```

Add `-GameDir` followed by the game folder if it isn't in the default Steam folder. The zip is written to the `dist` folder.

To publish a release:

1. Update the version in `Plugin.cs`, `changes-latest.md` and `CHANGELOG.md`.
2. Build the zip.
3. Create a draft GitHub release with the zip attached.
4. Run the **Release** workflow from the Actions tab with the same version tag. It builds the installer, writes `loadstone-release.json`, and publishes the release.

The installer's package files are in `loadstone-packages`, and its settings are in `installer\profile.json`. The installer reads the package files straight from this repository, so changes to them reach players a few minutes after they're pushed.
