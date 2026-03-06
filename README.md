# Parcore

A hardware monitor for Windows built with WPF and LibreHardwareMonitor.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![License](https://img.shields.io/badge/license-GPLv3-green)

## What it shows

- CPU usage, per-core load, temperature per core
- RAM usage, virtual memory
- GPU usage, engine breakdown, temperatures
- Storage read/write speeds
- Fan RPMs
- Network download/upload speeds (picks the active adapter automatically)

## Requirements

- Windows 10 or 11 (x64)
- Must be run as **Administrator** (LibreHardwareMonitor needs elevated access to read hardware sensors)

## Download

Grab the `.exe` from Releases:

1. Check the [Releases](../../releases) tab on the right.
2. Download the latest `Parcore.exe`.
3. Run it wherever you saved it (it needs Admin rights to read the hardware sensors).

*Note: Since I haven't paid for a code signing certificate, Windows SmartScreen will probably yell at you when you first run it. Just click "More info" -> "Run anyway".*

## Running from source

```sh
git clone https://github.com/yourusername/parcore.git
cd parcore
dotnet run
```

Right-click the terminal and run as Administrator, or build the exe and run that directly.

## Building a standalone exe

```sh
dotnet publish -c Release
```

Output goes to `bin\Release\net10.0-windows\win-x64\publish\Parcore.exe`.  
Single file, self-contained, no .NET install needed on the target machine.

## Hardware compatibility

Tested on AMD Ryzen. Should work on any hardware LibreHardwareMonitor supports, including:

- Intel and AMD CPUs
- Nvidia, AMD, and Intel Arc GPUs
- NVMe and SATA drives
- Most motherboard fan controllers

## Why admin?

LibreHardwareMonitor reads sensors through low-level Windows APIs (WMI, ring0 driver) that require elevated privileges. The app manifest already requests this automatically, Windows will prompt you on launch.

## License

**GPLv3**

You are free to use this software however you like. If you decide to modify the code and distribute your own version, you **must** open-source your changes under this same GPLv3 license and credit the original project.
