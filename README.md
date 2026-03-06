# Parcore

A hardware monitor for Windows built with WPF and LibreHardwareMonitor.

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

## What it shows

- CPU usage, per-core load, temperature per core
- RAM usage, virtual memory
- GPU usage, engine breakdown, temperatures
- Storage read/write speeds
- Fan RPMs
- Network download/upload speeds (picks the active adapter automatically)

## Requirements

- Windows 10 or 11 (x64)
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (if running the non-self-contained build)
- Must be run as **Administrator** — LibreHardwareMonitor needs elevated access to read hardware sensors

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

LibreHardwareMonitor reads sensors through low-level Windows APIs (WMI, ring0 driver) that require elevated privileges. The app manifest already requests this automatically — Windows will prompt you on launch.

## License

MIT