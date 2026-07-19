<br />
<div align="center">
  <h3 align="center">TinyWall</h3>

  <p align="center">
    A free, lightweight and non-intrusive firewall
    <br />
    <a href="https://tinywall.pados.hu"><strong>Website »</strong></a> | <a href="https://tinywall.pados.hu/download.php"><strong>Download »</strong></a>
  </p>
</div>

## About

TinyWall is a free, lightweight, and non-intrusive, secure by default firewall for Windows. Built to just simply sit in your system tray, quietly blocking any application you did not explicitly allow network access. TinyWall installs no kernel drivers, so it cannot negatively influence system stability. It also repects your privacy and collects absolutely no data about the user or their computer.

This repository houses the source code of TinyWall as found at its [official website](https://tinywall.pados.hu).

## How to build

### Necessary tools

- Microsoft Visual Studio 2026 (or 2022)
- [Wix v3.14 Toolset](https://github.com/wixtoolset/wix3/releases/tag/wix3141rtm)
- [Visual Studio extension for Wix v3 Toolset](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2022Extension)

### To build the application

1. Open the solution file in Visual Studio and compile the `TinyWall` project. The other projects referenced inside the solution need not be built separately as they will be statically compiled into the application.
1. Done.

### To build the database of known applications for debug and development

1. Adjust the individual JSON files in the `TinyWall\Database` folder. These are the source files for TinyWall's database of known applications, as well as for its built-in "Special Exceptions".
1. Use the embedded commandline utility in TinyWall to create a single-file database. A file called `profiles.json` will be created. Call it like:<br/>
`TinyWall.exe database-creator /source-folder <path to TinyWall\Database> /output-folder <any-folder>`
1. Copy the output file to `TinyWall\bin\Debug` to make use of it in Debug builds. For releases, see further below.
1. Done.

### To build the installer / releases

The setup is built by the WiX-based setup project. The setup project though needs its source files staged in a specific directory structure under `MsiSetup\Sources`, so all the binaries and assets like the database, the hosts file, docs, license etc. need to be copied there.

All these and further actions are taken care of by a fully automated build script. Besides correctly staging the files for you, it will make sure the resx language resources are in their correct form, it will take care of signing (can be skipped using --skip-sign) and will create a complete update package for server distribution for the built-in updater.

To use the automated build-script:
1. Copy `build-config.ini.template` to `build-config.ini`.
1. Edit `build-config.ini` to your liking.
1. Call `build.ps1` from a PowerShell.
1. Done. You'll find the built setup files in MsiSetup\bin.

The build script is commented, and following through its numbered steps makes it easy to work out what it is doing. Only if you're interested though, because you don't need to know any of that. Generally speaking, you can just call build.ps1 and enjoy its fully built outputs.

## Contributing

Feel free to open issues, feature- or pull-requests. I kindly ask for patience though, as TinyWall is in maintenance mode and my responses are often delayed. Nevertheless all issues and requests are looked at.

New features are more likely to get implemented if you provide the necessary code changes yourself. The process for that is the same as for most other projects here on GitHub, in short:
1. Fork the Project
1. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
1. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
1. Push to the Branch (`git push origin feature/AmazingFeature`)
1. Open a Pull Request on GitHub

For complex features or large changes, please contact me first if your changes are still within the vision of TinyWall staying small, efficient and simple.

If your goal with forking is not code contribution but to build and distribute your own binaries, please choose a name dissimilar to "TinyWall" to avoid confusing users.

## License

| Contents in                     | Maintainer   | Origin                                                                                                                                | License                  |
|---------------------------------|--------------|---------------------------------------------------------------------------------------------------------------------------------------|--------------------------|
| Microsoft.Samples\TaskDialog\   | KevinGre     | [link](https://www.codeproject.com/Articles/17026/TaskDialog-for-WinForms)  ([archive.org](https://web.archive.org/web/20250211033156/https://www.codeproject.com/Articles/17026/TaskDialog-for-WinForms))                                                          | Public Domain            |
| Microsoft.Samples\Privilege.cs  | Mark Novak   | [link](https://learn.microsoft.com/en-us/archive/msdn-magazine/2005/march/using-net-making-privileges-reliable-secure-and-efficient)  | see Privilege.cs.LICENSE |
| DarkModeCS.cs                   | BlueMystic   | [link](https://github.com/BlueMystical/Dark-Mode-Forms)                                                                               | MIT                      |
| Everything else                 | Károly Pados | [this repo](https://github.com/pylorak/TinyWall)                                                                                      | GPLv3                    |

## Contact

Károly Pados - find e-mail at the bottom of the project website

Website: <https://tinywall.pados.hu>

GitHub: <https://github.com/pylorak/tinywall>
