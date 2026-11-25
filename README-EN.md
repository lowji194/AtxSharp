[![🇻🇳 Tiếng Việt](https://img.shields.io/badge/Language-Ti%E1%BA%BFng%20Vi%E1%BB%87t-1e293b?style=flat-square)](README.md)

# AtxSharp — C# Android Automation Library via atx-agent

**A lightweight C# client library for Android UI automation and multi-emulator testing via `atx-agent`, inspired by Selenium WebDriver.**

## Requirements

- Windows, Linux, or macOS with `adb`
- Target device or emulator running `atx-agent`
- Forwarded port via `adb`, for example:
  ```sh
  adb -s emulator-5556 forward tcp:7913 tcp:7912
  ```

## Installation

1. Add `AtxSharp.cs` to your C# project (`.NET 6+` or `.NET Framework`).
2. Install `Newtonsoft.Json` via NuGet.

## Quick Example

```csharp
using AtxSharp;

var driver = new AtxAgentDriver(7913, 60);

var loginBtn = await driver.FindElement(By.Text("Sign in"));
await loginBtn.Click();

var input = await driver.FindElement(By.Id("com.example:id/email"));
await input.SendKeys("test@example.com");

await driver.TakeScreenshot("screen.png");
await driver.OpenDeeplink("app://product?id=123", "com.example.app");
```

## Key Capabilities

- Element locators: `By.Id`, `By.ClassName`, `By.Text`, `By.XPath`, plus `FindElements(By.XPath(...))`
- Element actions: `Click()`, `SendKeys()`, `GetText()`
- Device & gesture controls: `Swipe()`, `Tap()`, `LongTap()`, `PressBack()`, `Home()`, `OpenRecentApps()`, `GetPageSource()`, `GetScreenSize()`, `Screenshot()`
- Reliable Unicode input via `HumanSendKeyWithAdbKeyboard()` (with ADB Keyboard installed)

See [README.md](README.md) for the complete Vietnamese API reference.

## License

Released under the [MIT License](LICENSE).
