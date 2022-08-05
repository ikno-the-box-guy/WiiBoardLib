# WiiBoardLib
 A library made for easily interfacing with Nintendo's Wii Balance Board in C#.

## Usage examples
Creating and reading from the WiiBoard
```C#
var board = WiiBoard.FindWiiBoard(10, .1f);
var state = board.GetState();
```

## Supported Platforms
* Windows only
* .NET 6.0

## Dependencies
This library depends on the ``SharpDX.DirectInput`` library which [is available on NuGet:](https://www.nuget.org/packages/SharpDX.DirectInput)
``dotnet add package SharpDX.DirectInput --version 4.2.0``

This project also requires a special driver for the Wii Balance Board to correctly work on your device, [you can download the driver here](https://www.julianloehr.de/educational-work/hid-wiimote/). 