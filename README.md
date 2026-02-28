# Build Orchestrator

Build Orchestrator is a Unity tooling package for profile-driven builds:
- version bumping
- define symbols management
- build name templating
- build execution
- post-build cleanup and zip packaging
- custom build actions with pipeline stages

## Unity Compatibility

- Unity: `2022.3` or newer in the same LTS line

## Installation (Git URL)

Add the dependency to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.underdogg.build-orchestrator": "https://github.com/und3rd0gg/unity-build-orchestrator.git#v0.1.0"
  }
}
```

## First Setup

1. Open `Window/Package Manager`.
2. Select `Build Orchestrator`.
3. In `Samples`, import `Default BuildPipeline Config`.
4. Open menu `Tools/Build/Build Pipeline Manager`.
5. Review imported `BuildPipelineConfig.asset` and adjust profiles/flags.

## Menus

- `Tools/Build/Build Pipeline Manager`
- `File/Build Profiles/Build Pipeline Manager`
- `Tools/Build/Create Build Pipeline Config`
- `File/Build Profiles/Create Build Pipeline Config`

## Notes

- The package creates new config assets under `Assets/BuildPipeline/Config`.
- Define symbols are persisted in `PlayerSettings`.
- Dev version overlay appears only when `TL_BUILD_DEV` or `BUILD_DEV` is defined.

## Package Layout

- `Editor/` editor tooling and build pipeline services
- `Runtime/` runtime components
- `Samples~/DefaultConfig/` starter config sample

## License

MIT. See `LICENSE.md`.
