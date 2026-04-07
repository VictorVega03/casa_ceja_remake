# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build                              # Build the project
dotnet run                                # Run the application
dotnet build --configuration Release      # Build for release
```

## Tech Stack

- **.NET 8.0 / C#** — Windows desktop app (WinExe target)
- **Avalonia 11.3** — Cross-platform XAML UI framework (Fluent Design theme)
- **CommunityToolkit.Mvvm** — MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **SQLite + sqlite-net-pcl** — Local database via repository pattern
- **ClosedXML** — Excel export
- **BCrypt.Net** — Password hashing

The app locale is forced to `es-MX` (Spanish Mexico) at startup in `Program.cs`.

## Architecture

### Navigation & Entry Points

`App.axaml.cs` is the orchestrator — it initializes all services, handles navigation between modules, and owns the `MainWindow`. Navigation is done by swapping `MainWindow.Content` with different views.

Flow: **Login → ModuleSelector → Sync loading → {POS | Inventory | Admin}**

### Layered Architecture

```
Views (AXAML + code-behind)
    ↓
ViewModels (CommunityToolkit.Mvvm, inherit ViewModelBase)
    ↓
Services (business logic, 17+ services in /Services/)
    ↓
Repositories (generic BaseRepository<T> + entity-specific in /Data/Repositories/)
    ↓
SQLite (DatabaseService initializes schema via DatabaseInitializer)
```

### Three Main Modules

- **POS** — Sales, cart, credits, layaway, cash close, payments (~24 ViewModels)
- **Inventory** — Stock entries/outputs, product management
- **Admin** — Users, branches, categories, reporting

### Key Services

| Service | Responsibility |
|---|---|
| `DatabaseService` | SQLite connection + schema migrations |
| `AuthService` | Login, session, BCrypt verification |
| `ConfigService` | `AppConfig` (branch, printer, API settings) |
| `SyncService` | Syncs local DB with remote API via `ApiClient` |
| `CartService` | Active sale/cart state during POS session |
| `CashCloseService` | End-of-day cash reconciliation |
| `PrintService` | Thermal ticket printing via `ThermalTicketTemplates` |
| `ExportService` | Excel exports via ClosedXML |
| `FolioService` | Sequential folio/receipt number generation |

### Data Models

Core entities live in `/Models/`: `Sale`, `SaleProduct`, `Credit`, `CreditPayment`, `Layaway`, `LayawayPayment`, `CashClose`, `CashMovement`, `Product`, `Customer`, `User`, `Branch`, etc. DTOs for API sync are separate classes (e.g., `SalesSyncDTO`, `CreditSyncDTO`).

### Helpers

`/Helpers/` contains converters used in XAML bindings (`BoolToColorConverter`, `DateTimeConverter`, `BoolToOpacityConverter`), `DialogHelper` for Avalonia dialogs, `AdminVerificationHelper` for privilege escalation, and `KeyboardShortcutHelper` for POS shortcuts.
