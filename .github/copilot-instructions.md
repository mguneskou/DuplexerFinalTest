# DuplexerFinalTest — Copilot Project Instructions

## What This Project Is
C# .NET 8 WinForms application that replaces a LabVIEW-based RF duplexer final test system.
Runs automated multi-temperature sweep tests on duplexer hardware, saves results to SQL Server, and plots live measurement charts.

## Key Paths
- **Solution**: `DuplexerFinalTest.slnx` (VS 2022 `.slnx` format)
- **Project**: `DuplexerFinalTest/DuplexerFinalTest.csproj`
- **Live settings file (ONLY copy)**: `P:\MGunes\DuplexerTestSuite\Resources\Settings\SettingsGeneral.json`
  - NOT copied to output directory — always read from this network path at runtime
- **Change log**: `P:\MGunes\DuplexerTestSuite\ChangeLog\ChangeLog.txt`
  - Append every new version's changes here when committing; do NOT ask the user first

## Project Settings
- `<Nullable>disable</Nullable>`
- `<ImplicitUsings>disable</ImplicitUsings>`
- Target framework: `net8.0-windows`
- All `using` directives must be written explicitly — no global usings

## NuGet Packages
- `Microsoft.Data.SqlClient 5.2.2`
- `NationalInstruments.Visa 25.3.0.11` (resolves to 25.3.0.11 despite >= 22.0.0 spec — NU1603 warning is expected, do not fix)
- `Newtonsoft.Json 13.0.4`
- `System.Data.SqlClient 4.9.1`
- `System.Windows.Forms.DataVisualization 1.0.0-prerelease`

## Architecture

### Forms
| Form | Purpose |
|------|---------|
| `MainForm` | Live chart, elapsed time, chamber/DUT temperatures, equipment status, test controls |
| `StartForm` | DUT serial entry, test selection, sequence configuration |
| `SettingsForm` | 4-tab settings editor (General / Equipment / Paths / Database & Simulation) |
| `CalibrationForm` | Calibration workflow |
| `WaitForm` | Modal wait/progress display |
| `RetryCountdownForm` | Modal countdown when equipment comm fails — Resume Now or Cancel Test |

### Test Engine
- `Tests/TestRun.cs` — `BackgroundWorker` orchestrator; `RunTest()` has a `while(true)` retry loop catching `EquipmentCommunicationException`
- `Tests/IndividualTestRun.cs` — static methods: `RunBase_Z_IB_IOP`, `RunBase_Z_IPD`, `RunRemote_Z_IOP`, `RunRemote_Z_IPV`, `RunRemote_Z_VPV`
- `Tests/Pretest.cs` — pre-test checks

### Equipment Drivers (`Equipment/`)
`ClimaticChamber`, `ElectricalSwitch`, `OpticalSwitch`, `SMU`, `VisaController`

### Simulators (`EquipmentSim/`)
Full simulator equivalents for every piece of hardware — used when `USE_SIMULATORS = "true"` in settings

### Helpers
- `Shared.cs` — global state, paths, logger ref, `SoftwareVersion`
- `Logger.cs` — file-based logging
- `ProductionDatabase.cs` — SQL Server save logic
- `TestResultSaver.cs` — CSV / result file writer
- `EquipmentCommunicationException.cs` — dedicated exception for equipment comm failures (propagates up to `TestRun` retry loop)
- `Enums.cs` — `OverallPassFail`, `TestSequences`, `MessageType`, etc.

### Models
`CalibrationModel`, `ChamberModels`, `DUTModel`, `FinalTestSpecModel`, `GeneralSettingsModel`,
`InfoModel`, `MeasMainModel`, `SMUSettingsModel`, `TestFlowModel`, `TestResultModel`, `TestSequenceModel`

## Database
- Server: `TQYLMGUNES1\TQYLMGUNES1`
- Database: `JDS_Production`
- Auth: Integrated Security (SSPI), TrustServerCertificate=True
- Auto-save controlled by `SAVE_RESULTS_TO_DB_AUTO` in settings JSON

## Settings JSON Keys (SettingsGeneral.json)
`PC_NAME`, `BASE_ITEM_NUMBER`, `REMOTE_ITEM_NUMBER`, `SERIAL_NO_LENGTH`,
`PLOT_UPDATE_IN_MINUTES`, `USE_SIMULATORS`, `USE_LOCAL_DATABASE`,
`SAVE_RESULTS_TO_DB_AUTO`, `RESULTS_FOLDER`, `RESOURCES_FOLDER`,
`CONNECTION_STRING`, and 14 equipment resource keys (ELEC1…ELEC6, OPT1X4_1, OPT1X4_2,
OPT1X13_1, OPT1X13_2, SMU_MASTER, SMU_SLAVE, CHAMBER_IP, CHAMBER_PORT)

## Equipment Retry Behaviour
On `EquipmentCommunicationException` during a test step:
- Attempt 1 → 10-minute countdown, then auto-retry
- Attempt 2 → 15-minute countdown, then auto-retry
- Attempt 3+ → indefinite wait for operator ("Please fix and click Resume Now")
- Cancel Test at any point → `e.Cancel = true`, test aborts

## UI Threading Rule
Background worker code that needs to show a dialog MUST use:
```csharp
Application.OpenForms[0].Invoke((MethodInvoker)delegate { ... });
```

## Pre-Existing Warnings (Acceptable — Do Not Fix Unless Asked)
- `CS0168` — `ok` variable declared but unused in `Shared.cs`
- `NU1603` — NI.Visa version resolution (x2)
- `WFAC010` — High DPI manifest (app.manifest)

## Build Command
```powershell
dotnet build "D:\VSCodeRepo\DuplexerFinalTest\DuplexerFinalTest.slnx"
```
Expected clean output: **0 errors, 4 warnings** (the pre-existing ones above).

## Git & Versioning
- Remote: `https://github.com/mguneskou/DuplexerFinalTest.git`
- Branch: `main`
- Version is set in `DuplexerFinalTest.csproj` (`<AssemblyVersion>`, `<FileVersion>`, `<Version>`) and `app.manifest` (`<assemblyIdentity version="..."/>`)
- `MainForm.cs` reads version at startup: `Assembly.GetExecutingAssembly().GetName().Version`
- **Always update `ChangeLog.txt` before committing a new version**

## Version History
| Version | Date | Summary |
|---------|------|---------|
| 1.0.0.0 | 28-Apr-2026 | Initial release — full LabVIEW replacement |
| 1.1.0.0 | 29-Apr-2026 | Settings form redesign, retry countdown dialog, PASS/FAIL label, Cancel Test moved to equipment panel |
