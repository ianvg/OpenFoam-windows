# OpenFoam-windows

A Windows desktop prototype for setting up and running OpenFOAM simulations
through Windows Subsystem for Linux 2 (WSL2).

**Status: initial working prototype.** The Uno Windows app detects WSL distributions,
checks OpenFOAM, and runs the installed cavity tutorial with live logs and Stop
support. There is no installer yet. Configuration forms, saved projects, residual
charts, and ParaView launch integration remain planned.

## Build and run

Development prerequisites: Windows x64, .NET SDK 10.0.400 or a later 10.0.4xx patch,
and Visual Studio with the Windows/WinUI development tools. The repository pins
Uno.Sdk 6.7.18. Package restores require access to NuGet.

```powershell
./tools/build.ps1 -Test
./tools/run.ps1
```

For simulation, use a WSL2 Ubuntu distribution with OpenFOAM Foundation 13 installed.
The tested environment is Ubuntu 22.04 with `/opt/openfoam13/etc/bashrc`.
The prototype does not install or change WSL or OpenFOAM. Setup references:
[OpenFOAM on Windows](https://openfoam.org/download/windows/) and
[Microsoft WSL commands](https://learn.microsoft.com/windows/wsl/basic-commands).

In the app, select the distribution, check the OpenFOAM environment, and click
**Run cavity**. Each run creates a unique directory below
`~/.local/share/FoamStudio/runs/` in that distribution. The case includes `run.log`,
`mesh-check.log`, and, on success, `cavity.foam`. The existing tutorials are never
modified. The interface retains the last 120,000 characters; the case log contains
the full mesh/solver output. Closing during an operation requests cancellation and
waits for it before closing the window.

The cavity uses the installed tutorial's physics and numerical defaults, including
its turbulence settings. It is an integration example, not a validated design tool.

### Backend diagnostics

These commands exercise the same backend without the interface:

```powershell
dotnet run --project src/FoamStudio.Diagnostics -- probe Ubuntu-22.04
dotnet run --project src/FoamStudio.Diagnostics -- cavity Ubuntu-22.04
dotnet run --project src/FoamStudio.Diagnostics -- cancel-test Ubuntu-22.04
```

The cancellation check starts its own Linux job with a TERM-resistant child and
verifies that no matching child remains after cancellation. It does not stop the
distribution or other simulations. Ctrl+C requests cancellation in the CLI.

## Approach

- C#/.NET and Uno Platform for the Windows desktop interface.
- WSL2 with Ubuntu and a supported OpenFOAM distribution for meshing and solving.
- Standard OpenFOAM case folders, with working cases stored in the Linux filesystem.
- ParaView for viewing simulation results.

The initial supported solver environment is OpenFOAM Foundation 13 on WSL2 Ubuntu.
Other OpenFOAM versions are rejected by the environment check; adapters can follow.

## First milestone

Complete a lid-driven cavity simulation through a guided interface:

1. Create, save, reopen, and duplicate a project.
2. Configure cavity dimensions, mesh resolution, viscosity, lid velocity, and duration.
3. Generate the mesh and inspect mesh-quality checks.
4. Start and stop the solver, with live logs and residual plots.
5. Open the results in ParaView.

Acceptance requires an actual Windows/WSL run: a new project generates a valid
mesh, finishes its simulation, reopens successfully, and displays its results.

## Proposed architecture

| Component | Responsibility |
| --- | --- |
| App | Uno screens, navigation, forms, and charts |
| Core | Project models, validation, and workflow rules |
| OpenFoam | Case generation, templates, and version-specific commands |
| Infrastructure | WSL processes, files, settings, and logging |
| Diagnostics | CLI environment, simulation, and cancellation checks |

## Development sequence

1. Inspect reusable architecture patterns from RWHS Studio; establish the solution and toolchain. Reuse source only after checking its license.
2. Prove WSL integration: environment discovery, case creation, command execution, streamed logs, exit status, and cancellation of solver processes.
3. Build the guided cavity workflow using validated inputs and tested case templates.
4. Add durable project storage, run history, diagnostics, import/export, and identification of results from earlier settings.
5. Package a Windows installer with environment checks and verify the complete workflow.

Later scope includes STL import, snappyHexMesh, inlet/outlet flows, turbulence,
parallel solving, and engineering reports. These are outside the first milestone.

## Prototype limitations

- Single serial tutorial run at a time per app window; no custom case settings yet.
- No project save/open, charts, results viewer, or ParaView launch button yet.
- ParaView detection checks the selected Linux environment only.
- Startup environment checks time out after 45 seconds. A simulation cancellation
  waits for the WSL process handshake; an unresponsive WSL startup may delay Stop.
- Abrupt termination of the Windows app or Windows itself is not recovered yet.
- Windows binary distribution and a complete dependency notice inventory remain
  release work. See [third-party notes](THIRD_PARTY_NOTICES.md).

## License and independence

Original code and documentation in this repository are available under the
[MIT License](LICENSE).

OpenFOAM, ParaView, and other third-party software retain their respective licenses.
The plan is to invoke separately installed tools. Any third-party code, templates,
or assets introduced here must retain their applicable notices and licensing.

This is an independent project and is not affiliated with or endorsed by the
OpenFOAM Foundation or OpenCFD. OpenFOAM is a registered trademark of OpenCFD Ltd.
