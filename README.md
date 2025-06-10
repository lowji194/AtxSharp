# AtxSharp – Thư viện điều khiển Android tự động qua atx-agent (C#)

**Tự động hóa test app, thao tác UI, điều khiển đa giả lập/máy thật cực dễ kiểu Selenium/WebDriver!**

---

## 1. Yêu cầu hệ thống

- Windows (hoặc Linux/Mac đã cài `adb`)
- Điện thoại/giả lập đã cài và chạy `atx-agent`
- Đã thực hiện lệnh `adb forward` để chuyển tiếp cổng 
  ```sh
  adb -s emulator-5556 forward tcp:7913 tcp:7912
  ```

---

## 2. Cài đặt & sử dụng

1. Thêm file `AtxSharp.cs` vào project C# (.NET 6+ hoặc .NET Framework).
2. Cài thư viện `Newtonsoft.Json` qua NuGet.

---

## 3. Ví dụ sử dụng cơ bản

```csharp
using AtxSharp;

var driver = new AtxAgentDriver(7913, 60); // Port bạn đã forward

// Tìm và click nút 'Đăng nhập' theo text
var loginBtn = await driver.FindElement(By.Text("Đăng nhập"));
await loginBtn.Click();

// Tìm ô nhập email theo resource-id và nhập text
var input = await driver.FindElement(By.Id("com.example:id/email"));
await input.SendKeys("test@example.com");

// Lấy toàn bộ các item dạng TextView, duyệt từng item
var items = await driver.FindElements(By.XPath("//android.widget.TextView"));
foreach (var item in items)
    Console.WriteLine(await item.GetText());

// Chụp màn hình app
await driver.TakeScreenshot("man_hinh.png");

// Mở deeplink tới app (ví dụ: app mở trang chi tiết sản phẩm)
await driver.OpenDeeplink("app://product?id=123", "com.example.app");
```

---

## 4. API tham khảo nhanh

### Tìm element

- `FindElement(By.Id(string id))`
- `FindElement(By.ClassName(string className))`
- `FindElement(By.Text(string text))`
- `FindElement(By.XPath(string xpath))`
- `FindElements(By.XPath(string xpath))` *(trả về danh sách element)*

### Thao tác với element

- `Click()` – Click vào element
- `SendKeys(string)` – Nhập text vào element
- `GetText()` – Lấy text hiển thị của element

### Thao tác với driver

- `GetPageSource()` – Lấy XML UI hiện tại
- `GetCurrentPackage()` – Lấy tên package đang chạy
- `TakeScreenshot(path)` – Chụp màn hình và lưu ra file
- `OpenDeeplink(uri, package)` – Mở deeplink/app

---

## 5. Lưu ý sử dụng

- Nên forward mỗi giả lập/máy thật một cổng riêng:
  ```sh
  adb -s emulator-5556 forward tcp:7913 tcp:7912
  ```
- Timeout mặc định khi tìm element là **30s** (có thể chỉnh khi khởi tạo driver).
- `FindElements` chỉ hỗ trợ tìm qua `By.XPath`, các hàm còn lại chỉ hỗ trợ `FindElement`.
- Thư viện hỗ trợ chạy đa luồng, đa thiết bị song song.

---

## 6. Liên hệ & đóng góp

- Mọi ý kiến/câu hỏi/vấn đề vui lòng tạo Issue hoặc PR trực tiếp trên repo.
- Tác giả: [lowji194](https://github.com/lowji194)
