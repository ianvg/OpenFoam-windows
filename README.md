# OpenFoam-windows

A planned Windows desktop interface for setting up and running OpenFOAM simulations
through Windows Subsystem for Linux 2 (WSL2).

**Status: planning.** This repository currently contains the project plan and
license. There is no runnable application or installer yet.

## Approach

- C#/.NET and Uno Platform for the Windows desktop interface.
- WSL2 with Ubuntu and a supported OpenFOAM distribution for meshing and solving.
- Standard OpenFOAM case folders, with working cases stored in the Linux filesystem.
- ParaView for viewing simulation results.

The supported OpenFOAM distribution, version, Ubuntu release, and .NET/Uno versions
will be selected and recorded during the initial environment prototype.

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

## Development sequence

1. Inspect reusable architecture patterns from RWHS Studio; establish the solution and toolchain. Reuse source only after checking its license.
2. Prove WSL integration: environment discovery, case creation, command execution, streamed logs, exit status, and cancellation of solver processes.
3. Build the guided cavity workflow using validated inputs and tested case templates.
4. Add durable project storage, run history, diagnostics, import/export, and identification of results from earlier settings.
5. Package a Windows installer with environment checks and verify the complete workflow.

Later scope includes STL import, snappyHexMesh, inlet/outlet flows, turbulence,
parallel solving, and engineering reports. These are outside the first milestone.

## License and independence

Original code and documentation in this repository are available under the
[MIT License](LICENSE).

OpenFOAM, ParaView, and other third-party software retain their respective licenses.
The plan is to invoke separately installed tools. Any third-party code, templates,
or assets introduced here must retain their applicable notices and licensing.

This is an independent project and is not affiliated with or endorsed by the
OpenFOAM Foundation or OpenCFD. OpenFOAM is a registered trademark of OpenCFD Ltd.
