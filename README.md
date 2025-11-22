# SimStarter

SimStarter is a Windows launcher for racing sims and their companion tools. It provides:

- A shared catalog of Sims and Addons (CrewChief, SimHub, etc.).
- “Starters” that pair one Sim with selected Addons.
- Desktop/Start Menu shortcuts per Starter (with composed icons).
- CLI launcher for quick starts.
- WPF UI with tabs for Sims, Addons, and Starters.

## Requirements
- Windows with .NET 8+ SDK installed.

## Projects
- `SimStarter.Core`: shared models, config, and runner.
- `SimStarter` (CLI): minimal console launcher.
- `SimStarter.UI` (WPF): tabbed UI to manage sims/addons/starters, create shortcuts, and start profiles.
- `SimStarter.IconTests`: icon verification tests.

## Build
```bash
dotnet build SimStarter.sln
```

## Run
- UI: `dotnet run --project SimStarter.UI/SimStarter.UI.csproj`
- CLI: `dotnet run --project SimStarter.csproj`

## Config
Profiles/config are stored alongside the built binaries as `profiles.json`. Use the UI to manage sims/addons/starters.

## Tests
Icon tests:
```bash
dotnet test SimStarter.IconTests/SimStarter.IconTests.csproj
```

## CI
GitHub Actions workflow builds the UI on push/PR to `main`: `.github/workflows/build-ui.yml`.
