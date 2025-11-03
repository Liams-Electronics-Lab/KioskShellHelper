# Kiosk Shell Helper

A Windows application for kiosk environments that launches applications with customizable startup overlays and process management upon closure.

I made this application to fix a specific problem I had, I have a Home Theater PC, where the Windows shell is replaced by a project called Flex launcher.
I needed a way to easily start Windows Explorer but also close it completly without using task manager. 
So I am sharing it in hopes it helps someone else.

## Features

- **Launch Any Application**: Configure any executable to run on startup
- **Startup Overlay**: Display text or image during application launch 
- **Persistent Close Button**: Always-visible button to terminate processes and restart applications
- **Process Management**: Close and restart configured processes when exiting

  
<img width="2560" height="1440" alt="Screenshot 2025-11-03 130507" src="https://github.com/user-attachments/assets/a8315ab9-7c71-4668-b297-ef711da3f719" />


## Quick Start

1. Run `KioskShellHelper.exe`
2. Edit `settings.ini` to configure your application and processes
3. Restart the helper to apply changes

## Configuration

All settings are in `settings.ini` (auto-created on first run):

### Startup Application
```ini
[StartupApp]
AppPath=C:\Windows\explorer.exe
AppArguments=
DelaySeconds=1
OpenMaximizedExplorer=true
```

### Startup Overlay
```ini
[OpenOverlay]
DisplayMode=Text
Text=Loading...
BackgroundColor=0,0,0
TextColor=255,255,255
ImagePath=
```

### Close Button
```ini
[CloseButton]
BackgroundColor=192,0,0
TextColor=255,255,255
Text=Close
X=0
Y=auto
Width=200
CleanupDelay=5000
```

### Process Cleanup (on close)
```ini
[ProcessCleanup]
Process1=explorer
Process1_KeepAlive=0
Process1_Delay=200
```
- **KeepAlive**: Number of process instances to keep running (0=kill all, 1=keep one instance and so on...)
- **Delay**: Wait time (ms) before moving to next process
- **process**: This is the process name as you would see it in task manager, extensions are trimmed. Explorer also kills processes called Windows Explorer 
### Process Startup (after cleanup)
```ini
[ProcessStart]
Process1=C:\Path\To\Application.exe
Process1_Delay=200
```

### Close Overlay
```ini
[CloseOverlay]
BackgroundColor=0,0,0
TextColor=255,255,255
Text=Please Wait...
```

## Use Cases

- **Kiosk Systems**: Launch and manage kiosk applications with easy restart, tested on Windows 11 iot with basic shell launcher config
- **HTPC**: Like in my use case, shell is replaced by an app launcher but I wanted to keep easy explorer access for mainatince 
- **Something custom**: An easy one click solution to launch and close a number of applications at once (using your own script for the startup executable)

## Requirements

- .NET 6.0 Runtime
- Windows OS x64

## Building from Source

```bash
dotnet publish ExplorerHTPCHelper.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output: `bin\Release\net6.0-windows\win-x64\publish\KioskShellHelper.exe`

## License

MIT

