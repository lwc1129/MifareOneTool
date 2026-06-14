# Dev Testing Guide

This document describes how to test the changes on the `claude/upbeat-wright-81stuu` branch.

## Prerequisites

- .NET 8 SDK (`dotnet --version` → 8.x)
- For macOS NFC testing: `brew install libnfc mfoc`

## 1. Build both projects

```bash
dotnet build src/MifareOneTool.Core/MifareOneTool.Core.csproj
dotnet build src/MifareOneTool.UI/MifareOneTool.UI.csproj
```

Both should report **Build succeeded. 0 Warning(s) 0 Error(s)**.

## 2. Run Core unit tests (manual smoke test)

```bash
dotnet run --project src/MifareOneTool.Core/ --no-build -- --test 2>/dev/null || true
```

(No test runner yet — see section 4 for what to verify manually.)

## 3. Launch Avalonia UI

```bash
dotnet run --project src/MifareOneTool.UI/
```

Expected: window opens titled **MifareOneTool**, left panel shows Device / Read / Write / UID / Language sections, right panel shows output log.

Click **Scan Devices** → log shows "Device scan not yet implemented". This is expected; NFC wiring is a next step.

## 4. Traditional Chinese (zh-TW) — Windows only

On a Windows machine with the original WinForms build:

1. Open the app.
2. In the language dropdown, select **繁體中文**.
3. The app restarts in zh-TW locale.
4. All labels should display correct Traditional Chinese (扇區, 裝置, 開始執行, etc.) with no garbled characters.

Compare against **中文** (Simplified) to confirm the difference.

## 5. Core model smoke test

Run a quick dotnet-script or copy this into a scratch `.cs` file:

```csharp
using MifareOneTool.Core.Models;

var s50 = new S50();
Console.WriteLine("Sectors: " + s50.Sectors.Count);    // → 16
Console.WriteLine("UID sector0: " + Utils.Hex2Str(s50.Sectors[0].Block[0].Take(4).ToArray()));
var info = s50.Sectors[0].Info(0, "Sector ", " (empty)", " (data)", " (error)");
Console.WriteLine(info);  // → Sector 0 (data)

s50.ExportToMctTxt("/tmp/test.mct");
Console.WriteLine("MCT exported: " + System.IO.File.Exists("/tmp/test.mct"));
```

## 6. AppSettings persistence

```csharp
using MifareOneTool.Core.Services;

var s = new AppSettings { AutoABN = false, Language = "zh-TW" };
s.Save();
var s2 = AppSettings.Load();
Console.WriteLine(s2.Language);  // → zh-TW
```

## 7. macOS — NFC tool resolution

```csharp
using MifareOneTool.Core.Services;

Console.WriteLine(PlatformInfo.IsMacOS);       // true on Mac
Console.WriteLine(PlatformInfo.DefaultBundledBinDir);
Console.WriteLine(PlatformInfo.ResolveToolPath("mfoc", "nfc-bin"));
// Should print /opt/homebrew/bin/mfoc (Apple Silicon) or /usr/local/bin/mfoc (Intel)
```
