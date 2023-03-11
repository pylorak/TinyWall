<br />
<div align="center">
  <h3 align="center">TinyWall</h3>

  <p align="center">
    A free, lightweight and non-intrusive firewall
    <br />
    <a href="https://tinywall.pados.hu"><strong>Website »</strong></a>
  </p>
</div>


## About this repository

This is the source code of TinyWall as found at its [website](https://tinywall.pados.hu). Upstream development is now largely inactive, but this repository is provided for anyone who would like to submit their own improvements or fork the project.

## How to build

### Necessary tools
- Microsoft Visual Studio 2019 (v16.11.xx)  
  (VS 2022 should also work but not tested)
- [Wix v3 Toolset](https://github.com/wixtoolset/wix3/releases/tag/wix3112rtm)
- [Visual Studio extension for Wix v3 Toolset](https://marketplace.visualstudio.com/items?itemName=WixToolset.WiXToolset)

### To build the application
1. Open the solution file in Visual Studio and compile the `TinyWall` project. The other projects referenced inside the solution need not be compiled separately as they will be statically compiled into the application.
1. Done.

### To update/build build the database of known applications
1. Adjust the individual JSON files in the `TinyWall\Database` folder.
1. Start the application with the `/develtool` flag.
1. Use the `Database creator` tab to create one combined database file in JSON format. The output file will be called `profiles.json`.
1. To use the new database in debug builds, copy the output file to the `TinyWall\bin\Debug` folder.
1. Done.

### To build the installer
1. Copy the compiled application files and all dependencies into the `MsiSetup\Sources\ProgramFiles\TinyWall` folder.
1. Update the files as necessary inside the `MsiSetup\Sources\CommonAppData\TinyWall` folder. See instructions above about creating the database.
1. Open the solution file in Visual Studio and compile the `MsiSetup` project.
1. Done.


## Contributing

Please don't open issues for feature requests or bug reports. Any changes you'd like you will need to implement yourself. If you have improvements that you would like to integrate into TinyWall, please fork the repo and create a pull request.

1. Fork the Project
1. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
1. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
1. Push to the Branch (`git push origin feature/AmazingFeature`)
1. Open a Pull Request

For complex features or large changes, please contact me first if your changes are still within the scope of the application.

If you prefer that, you can also build and distribute your own version of the binaries. In this case though you need to choose a different name other than TinyWall for your application.


## License

- TaskDialog wrapper (code in directory `pylorak.Windows\TaskDialog`) written by KevinGre ([link](https://www.codeproject.com/Articles/17026/TaskDialog-for-WinForms)) and placed under Public Domain.

- All other code in the repository is under the GNU GPLv3 License. See `LICENSE.txt` for more information.


## Contact

Károly Pados - find e-mail at the bottom of the project website

Website: [https://tinywall.pados.hu](https://tinywall.pados.hu)

GitHub: [https://github.com/pylorak/tinywall](https://github.com/pylorak/tinywall)
