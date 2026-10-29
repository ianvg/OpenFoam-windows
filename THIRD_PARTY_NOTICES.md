# Third-party software

The MIT license applies to original Foam Studio code and documentation. Dependencies
keep their own licenses. The Windows installer includes a `ThirdPartyNotices`
directory containing the restored NuGet dependency inventory, package metadata,
license files, embedded third-party notices, and the .NET runtime's notices.
The inventory includes development/build dependencies as well as runtime packages.

| Software | Role | License information |
| --- | --- | --- |
| .NET | Runtime and development tools | https://github.com/dotnet/runtime/blob/main/LICENSE.TXT |
| Uno Platform | Application SDK | https://github.com/unoplatform/uno/blob/master/LICENSE |
| Windows App SDK | Windows interface/runtime | https://github.com/microsoft/WindowsAppSDK/blob/main/LICENSE |
| xUnit.net | Development tests | https://github.com/xunit/xunit/blob/main/LICENSE |

OpenFOAM Foundation 13 and ParaView are separately installed external tools. The
prototype copies the installed OpenFOAM cavity tutorial into a local working case
at runtime. Those case files keep their upstream OpenFOAM notices and GPL terms;
they are not relicensed under MIT and are not included in this repository.

Inno Setup is used to build the installer; its compiler is not installed with Foam
Studio. The resulting Setup executable is unsigned for this prototype release.
