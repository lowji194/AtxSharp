Dưới đây là toàn bộ nội dung file README.md của repo lowji194/AtxSharp:

---

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
    Console.WriteLine(item.GetText());

// Chụp màn hình app
await driver.TakeScreenshot("man_hinh.png");

// Mở deeplink tới app (ví dụ: app mở trang chi tiết sản phẩm)
await driver.OpenDeeplink("app://product?id=123", "com.example.app");
```

---

## 4. API tham khảo nhanh

### 4.1. Tìm element

- `FindElement(By.Id(string id))`
- `FindElement(By.ClassName(string className))`
- `FindElement(By.Text(string text))`
- `FindElement(By.XPath(string xpath))`
- `FindElements(By.XPath(string xpath))` *(trả về danh sách element)*

### 4.2. Thao tác với element

- `Click()` – Click vào element
- `SendKeys(string)` – Nhập text vào element
- `GetText()` – Lấy text hiển thị của element

### 4.3. Thao tác với driver

- `GetPageSource()` – Lấy XML UI hiện tại
- `GetCurrentPackage()` – Lấy tên package đang chạy
- `TakeScreenshot(path)` – Chụp màn hình và lưu ra file
- `Screenshot()` – Chụp màn hình và trả về mảng byte[]
- `OpenDeeplink(uri, package)` – Mở deeplink/app
- `GetScreenSize()` – Lấy kích thước màn hình (width, height)

### 4.4. Thao tác thao tác cử chỉ & phím bấm

- `Swipe(x1, y1, x2, y2, duration)` – Vuốt màn hình từ điểm (x1, y1) đến (x2, y2) trong thời gian `duration` giây (mặc định 0.2s).
- `Tap(x, y)` – Chạm vào toạ độ (x, y) bất kỳ trên màn hình.
- `LongTap(x, y, duration)` – Nhấn giữ tại (x, y) trong thời gian `duration` giây (mặc định 1.0s).
- `PressBack()` – Nhấn nút Back (trở về).
- `OpenRecentApps()` – Mở menu đa nhiệm (Recent Apps).
- `Home()` – Trở về màn hình chính.

---

## 5. Ví dụ nâng cao

### Vuốt, chạm, nhấn giữ, thao tác hệ thống

```csharp
// Vuốt từ trái sang phải
await driver.Swipe(100, 500, 900, 500, 0.3);

// Chạm vào vị trí 200x400
await driver.Tap(200, 400);

// Nhấn giữ tại vị trí 300x800 trong 1.5 giây
await driver.LongTap(300, 800, 1.5);

// Nhấn Back
await driver.PressBack();

// Mở Recent Apps
await driver.OpenRecentApps();

// Trở về màn hình chính
await driver.Home();
```

### Lấy kích thước màn hình

```csharp
var (width, height) = await driver.GetScreenSize();
Console.WriteLine($"Kích thước màn hình: {width}x{height}");
```

### Chụp màn hình trả về mảng byte

```csharp
byte[] imageBytes = await driver.Screenshot();
System.IO.File.WriteAllBytes("screenshot.png", imageBytes);
```

---

## 6. Lưu ý sử dụng

- Nên forward mỗi giả lập/máy thật một cổng riêng:
  ```sh
  adb -s emulator-5556 forward tcp:7913 tcp:7912
  ```
- Timeout mặc định khi tìm element là **30s** (có thể chỉnh khi khởi tạo driver).
- `FindElements` chỉ hỗ trợ tìm qua `By.XPath`, các hàm còn lại chỉ hỗ trợ `FindElement`.
- Thư viện hỗ trợ chạy đa luồng, đa thiết bị song song.
- Các thao tác tìm kiếm element đã tích hợp tự động retry trong thời gian timeout, giúp ổn định hơn khi UI load chậm.
- Có thể sử dụng `Screenshot()` để lấy ảnh dạng byte[] và tự xử lý lưu file theo nhu cầu.

---

## 7. Liên hệ & đóng góp

- Mọi ý kiến/câu hỏi/vấn đề vui lòng tạo Issue hoặc PR trực tiếp trên repo.
- Tác giả: [lowji194](https://github.com/lowji194)

---
