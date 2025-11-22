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

## Getting it
- Download the latest UI ZIP from the [Releases](https://github.com/puter/SimStarter/releases) page.
- Extract and run `SimStarter.UI.exe`.
- (Optional) CLI: build/run locally with `dotnet run --project SimStarter.csproj`.

## What it does (and doesn’t)
- You bring your own sims and addons; SimStarter does not include them.
- Add sims and addons you already have installed, then create “Starters” that pair a single sim with selected addons.
- Launch starters from the app, or create desktop/Start Menu shortcuts whose icons reflect the sim and included addons.
- Right-click a sim or addon in the UI to open its location in Explorer.

## Config
Profiles/config are stored alongside the built binaries as `profiles.json`. Use the UI to manage sims/addons/starters.

## Tests
Icon tests:
```bash
dotnet test SimStarter.IconTests/SimStarter.IconTests.csproj
```

## CI
GitHub Actions workflow builds the UI on push/PR to `main`: `.github/workflows/build-ui.yml`.
