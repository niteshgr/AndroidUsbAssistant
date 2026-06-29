# Android USB Assistant

A Windows system tray application that detects Android devices connected over USB and performs configurable automation actions, starting with enabling USB tethering via ADB.

This application is built in C# using Windows Forms, .NET 10, Dependency Injection, and the Microsoft Generic Host framework, following Clean Architecture principles.

## Technologies Used

* **Language/Framework**: .NET 10 & C#
* **UI Framework**: Windows Forms (WinForms)
* **Application Framework**: Microsoft.Extensions.Hosting (Generic Host)
* **Dependency Injection**: Microsoft.Extensions.DependencyInjection
* **Logging**: Microsoft.Extensions.Logging (Console & Debug)
* **Configuration**: System.Text.Json (Settings persisted in `%LOCALAPPDATA%\AndroidUsbAssistant\settings.json`)

## Architecture

The project adheres to Clean Architecture guidelines, separating business logic from infrastructure and presentation concerns:

* **AndroidUsbAssistant.Core**: Holds entities, business objects, and interface contracts. Contains no external dependencies.
* **AndroidUsbAssistant.Infrastructure**: Implements Core interfaces. Interacts with local files, serialization services, and hardware-specific APIs.
* **AndroidUsbAssistant.App**: The composition root. Handles user interface controls, UI lifecycles, tray icons, logging configuration, and generic host orchestration.

For a detailed review of the architecture, see [ARCHITECTURE.md](file:///c:/Users/kings/OneDrive/Desktop/AndroidUsbAssistant/ARCHITECTURE.md).

## Getting Started

### Prerequisites

* [.NET SDK 10](https://dotnet.microsoft.com/download)

### Build the Application

From the root directory:

```powershell
dotnet build
```

### Run the Application

To run the system tray application:

```powershell
dotnet run --project src/AndroidUsbAssistant.App/AndroidUsbAssistant.App.csproj
```

The application will start in your Windows system tray. Look for the custom circular teal icon.

* Right-click the icon to access the menu options:
  * **Status**: Open the Status dashboard.
  * **Settings**: Configure settings like your ADB path and startup choices.
  * **About**: View application context details.
  * **Exit**: Terminate both the UI and the underlying Generic Host cleanly.