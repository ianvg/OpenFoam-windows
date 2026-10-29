# Initial prototype verification

Verified on Windows x64 on 2026-10-29 with .NET 10.0.401, Visual Studio MSBuild,
Uno.Sdk 6.7.18, and WSL2 Ubuntu 22.04.5 / OpenFOAM Foundation 13.

## Checks

- Windows application builds using `tools/build.ps1`.
- Automated tests cover stdout/stderr, nonzero exit, Windows process cancellation,
  environment readiness, WSL1 rejection, unsupported solver rejection, and WSL
  distribution output parsing.
- Diagnostics detect both installed WSL distributions and the supported OpenFOAM
  environment, including its tutorial and Linux ParaView executable.
- The real cavity workflow completes `blockMesh`, passes `checkMesh` (`Mesh OK.`),
  and completes `foamRun` at the installed tutorial's end time of 10 seconds.
- Cancellation of an isolated Linux session removes a child that ignores TERM,
  exercising the KILL fallback. No matching child remains after the test.
- Interactive Windows checks verify application launch, distribution selection,
  Check environment, Run cavity, live output, successful completion, and the case
  location display.

The automated Windows-process and live Linux-group cancellation checks exercise
the mechanisms behind Stop. Interactive Stop timing, closing during a live solver,
reboot recovery, missing-dependency scenarios on a clean machine, and installer
acceptance still require further testing. This verifies execution plumbing, not
the physical accuracy or suitability of a CFD setup.
