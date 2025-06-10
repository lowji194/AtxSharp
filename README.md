# README.md chi tiết tiếng Việt
readme = """
# AtxSharp – Thư viện điều khiển Android tự động qua atx-agent (C#)
**Tự động hóa test app, thao tác UI, điều khiển đa giả lập/máy thật cực dễ kiểu Selenium/WebDriver!**

## 1. Yêu cầu hệ thống
- Windows (hoặc Linux/Mac cài adb)
- Cài sẵn `atx-agent` trên điện thoại/giả lập, đã khởi động
- Đã `adb forward` (ví dụ: `adb -s emulator-5556 forward tcp:7913 tcp:7912`)

## 2. Cài đặt & sử dụng
- Thêm file `AtxSharp.cs` vào project C# (.NET 6+ hoặc .NET Framework)
- Cài thư viện `Newtonsoft.Json` (qua NuGet)

## 3. Ví dụ sử dụng cơ bản
```csharp
using AtxSharp;

var driver = new AtxAgentDriver(7913); // Port bạn đã forward
// Tìm và click nút 'Đăng nhập' theo text
var loginBtn = await driver.FindElement(By.Text(\"Đăng nhập\"));
await loginBtn.Click();

// Tìm ô nhập email theo resource-id và nhập text
var input = await driver.FindElement(By.Id(\"com.example:id/email\"));
await input.SendKeys(\"test@example.com\");

// Lấy toàn bộ các item dạng TextView, duyệt từng item
var items = await driver.FindElements(By.XPath(\"//android.widget.TextView\"));
foreach (var item in items)
    Console.WriteLine(item.GetText());

// Chụp màn hình app
await driver.TakeScreenshot(\"man_hinh.png\");

// Mở deeplink tới app (ví dụ: app mở trang chi tiết sp)
await driver.OpenDeeplink(\"app://product?id=123\", \"com.example.app\");
4. API tham khảo nhanh
Tìm element:
FindElement(By.Id(string))

FindElement(By.ClassName(string))

FindElement(By.Text(string))

FindElement(By.XPath(string))

FindElements(By.XPath(string))

Thao tác với element
Click() – Click vào element

SendKeys(string) – Nhập text

GetText() – Lấy text hiển thị

Thao tác với driver
GetPageSource() – Lấy XML UI hiện tại

GetCurrentPackage() – Lấy package đang chạy

TakeScreenshot(path) – Chụp màn hình lưu ra file

OpenDeeplink(uri, package) – Mở deeplink/app

5. Lưu ý sử dụng
Nên forward mỗi giả lập/máy một cổng riêng: adb -s emulator-5556 forward tcp:7913 tcp:7912

Timeout mặc định khi tìm element là 30s (có thể chỉnh khi khởi tạo driver)

Chỉ FindElements(By.XPath), các hàm khác chỉ FindElement

Chạy tốt đa luồng, đa thiết bị
