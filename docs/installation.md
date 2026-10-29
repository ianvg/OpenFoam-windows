# Windows installer

The initial installer is `Foam-Studio-0.1.0-win-x64-Setup.exe` (about 48 MB).
It targets Windows x64-compatible systems, Windows 10 version 2004 or later.
The current smoke test used Windows 11 x64. A clean-machine test remains outstanding.

## Install and remove

1. Double-click Setup and follow its license and prerequisite pages.
2. Choose a per-user installation folder. The default is
   `%LOCALAPPDATA%\Programs\Foam Studio`.
3. Optionally create a desktop shortcut. A Start menu shortcut is always created.
4. Launch Foam Studio and click **Check environment**.

No administrator access is requested by Setup. The self-contained payload includes
.NET and Windows App SDK runtimes. To run a simulation, separately install WSL2,
Ubuntu, and OpenFOAM Foundation 13. The tested environment uses Ubuntu 22.04 and
`/opt/openfoam13/etc/bashrc`. See the [OpenFOAM Windows instructions](https://openfoam.org/download/windows/).

The installer is unsigned. Windows may show an unknown-publisher or SmartScreen
warning. Production signing is not configured.

Close all Foam Studio windows before rerunning Setup or uninstalling. Setup blocks
maintenance while a packaged-build instance is running. Remove the application
through Windows Installed Apps or `unins000.exe` in the installation folder.
Uninstall removes its installed files, registration, and shortcuts. It preserves
unknown user files and does not remove simulation cases in WSL, WSL itself, or
OpenFOAM. There is no automatic updater or downgrade management in this prototype.

## Build Setup

Prerequisites: the normal Windows build tools, PowerShell 7, and Inno Setup 6.

```powershell
./tools/package.ps1 -Version 0.1.0
```

The output directory `artifacts/release/0.1.0` must not already exist. Failed builds
are retained for diagnosis. The script runs tests, publishes the app with bundled
runtimes, collects notices, compiles Setup, and writes `SHA256SUMS.txt`. It does not
publish a GitHub Release or install the application automatically.

Artifacts:

- `Foam-Studio-0.1.0-win-x64-Setup.exe`: the installer to copy to another machine.
- `SHA256SUMS.txt`: SHA-256 checksum for the installer.
- `app/`: the complete self-contained payload (also runnable directly).

The installer version and application's version are set from the build parameter.
`packages.ReleaseWindows.lock.json` records the publish dependency graph separately
from normal development builds.

## Verification

The initial 0.1.0 candidate passed eight automated tests. An isolated installation
in a path containing spaces verified bundled runtime files, notices, Start menu
shortcut, and the per-user uninstall registration. The installed executable was
launched interactively, and its OpenFOAM environment check was verified. Setup
refused maintenance while the app was running. Uninstall was checked for removal
of application files and registration while preserving an unknown user file.

The smoke-test helper refuses to replace an existing Foam Studio installation:

```powershell
./tools/test-installer.ps1 -Setup artifacts/release/0.1.0/Foam-Studio-0.1.0-win-x64-Setup.exe
# Launch the installed app for interactive verification, then close it.
./tools/test-installer.ps1 -Setup artifacts/release/0.1.0/Foam-Studio-0.1.0-win-x64-Setup.exe -Uninstall
```
