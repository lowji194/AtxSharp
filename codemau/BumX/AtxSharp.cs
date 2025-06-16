// ========================
// AtxSharp - Thư viện điều khiển thiết bị Android kiểu Selenium thông qua atx-agent
// ========================
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace AtxSharp
{
    /// <summary>
    /// Enum đại diện cho kiểu tìm kiếm element giống Selenium: By.Id, By.ClassName, By.Text, By.XPath
    /// </summary>
    public enum ByType { Id, Class, Text, XPath }

    /// <summary>
    /// Lớp khởi tạo tiêu chí tìm kiếm element (giống Selenium By)
    /// </summary>
    public class By
    {
        public ByType Type { get; private set; }
        public string Value { get; private set; }

        private By(ByType type, string value)
        {
            Type = type;
            Value = value;
        }
        /// <summary>
        /// Tìm kiếm theo resource-id Android (By.Id)
        /// </summary>
        public static By Id(string value) => new By(ByType.Id, value);
        /// <summary>
        /// Tìm kiếm theo className Android (By.ClassName)
        /// </summary>
        public static By ClassName(string value) => new By(ByType.Class, value);
        /// <summary>
        /// Tìm kiếm theo text hiển thị của element (By.Text)
        /// </summary>
        public static By Text(string value) => new By(ByType.Text, value);
        /// <summary>
        /// Tìm kiếm theo biểu thức XPath (By.XPath)
        /// </summary>
        public static By XPath(string value) => new By(ByType.XPath, value);
    }

    /// <summary>
    /// Driver chính thao tác với atx-agent, mô phỏng Selenium WebDriver
    /// </summary>
    public class AtxAgentDriver
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;
        private static readonly HttpClient client = new HttpClient();

        /// <param name="port">Port mà bạn đã adb forward tới thiết bị (thường là 7912, 7913,...)</param>
        /// <param name="timeout">Thời gian timeout mặc định khi tìm element (giây)</param>
        public AtxAgentDriver(int port, int timeout = 30)
        {
            baseUrl = $"http://127.0.0.1:{port}";
            timeoutSeconds = timeout;
        }

        /// <summary>
        /// Tìm element theo tiêu chí By (giống Selenium). Hỗ trợ By.Id, By.ClassName, By.Text, By.XPath
        /// </summary>
        /// <param name="by">Tiêu chí tìm element</param>
        /// <returns>Đối tượng AtxElement (gần giống WebElement của Selenium)</returns>
        public async Task<AtxElement> FindElement(By by)
        {
            switch (by.Type)
            {
                case ByType.Id:
                    return await FindElementBySelector(new { resourceId = by.Value });
                case ByType.Class:
                    return await FindElementBySelector(new { className = by.Value });
                case ByType.Text:
                    return await FindElementBySelector(new { text = by.Value });
                case ByType.XPath:
                    var elements = await FindElementsByXPath(by.Value);
                    if (elements.Count > 0) return elements[0];
                    break;
            }
            throw new Exception("Không tìm thấy element với tiêu chí chỉ định.");
        }

        /// <summary>
        /// Tìm nhiều element theo XPath. (Chỉ hỗ trợ FindElements bằng XPath)
        /// </summary>
        /// <param name="by">By.XPath(...)</param>
        /// <returns>Danh sách AtxElement</returns>
        public async Task<List<AtxElement>> FindElements(By by)
        {
            switch (by.Type)
            {
                case ByType.XPath:
                    return await FindElementsByXPath(by.Value);
                default:
                    throw new NotSupportedException("FindElements chỉ hỗ trợ XPath trong atx-agent.");
            }
        }

        /// <summary>
        /// Lấy cấu trúc XML giao diện hiện tại (tương tự PageSource Selenium)
        /// </summary>
        public async Task<string> GetPageSource()
        {
            var res = await PostJson($"{baseUrl}/uiautomator", new
            {
                jsonrpc = "2.0",
                method = "dumpWindowHierarchy",
                @params = new { compressed = false },
                id = 9
            });
            return res["result"]?.ToString()!;
        }

        /// <summary>
        /// Lấy tên package ứng dụng hiện tại đang chạy trên thiết bị
        /// </summary>
        public async Task<string> GetCurrentPackage()
        {
            var res = await client.GetStringAsync($"{baseUrl}/info");
            var json = JObject.Parse(res);
            return json["currentPackage"]?.ToString() ?? "";
        }

        /// <summary>
        /// Mở deeplink hoặc URI intent trên thiết bị
        /// </summary>
        /// <param name="uri">Đường dẫn deeplink (ví dụ: app://...)</param>
        /// <param name="pkg">Tên package ứng dụng muốn mở</param>
        public async Task OpenDeeplink(string uri, string pkg)
        {
            var data = new { action = "android.intent.action.VIEW", uri, pkg };
            await PostJson($"{baseUrl}/app/intent", data);
        }

        /// <summary>
        /// Chụp ảnh màn hình hiện tại và lưu vào file
        /// </summary>
        /// <param name="path">Đường dẫn file ảnh .png</param>
        public async Task TakeScreenshot(string path)
        {
            var bytes = await client.GetByteArrayAsync($"{baseUrl}/screenshot");
            await System.IO.File.WriteAllBytesAsync(path, bytes);
        }

        /// <summary>
        /// Hàm nội bộ - tìm 1 element bằng selector (Id, Class, Text)
        /// Tự động retry chờ đến khi tìm thấy hoặc hết timeout
        /// </summary>
        private async Task<AtxElement> FindElementBySelector(object selector)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < timeoutSeconds)
            {
                try
                {
                    var rpc = new
                    {
                        jsonrpc = "2.0",
                        method = "getText",
                        @params = new { selector },
                        id = 1
                    };
                    var textRpc = await PostJson($"{baseUrl}/uiautomator", rpc);

                    var xpathRpc = new
                    {
                        jsonrpc = "2.0",
                        method = "xpath",
                        @params = new { expression = "//*" },
                        id = 2
                    };
                    var result = await PostJson($"{baseUrl}/uiautomator", xpathRpc);
                    var elements = result["result"] != null ? result["result"] as JArray : null;

                    if (elements != null)
                    {
                        foreach (var el in elements)
                        {
                            if (el["text"]?.ToString() == textRpc["result"]?.ToString())
                            {
                                var bounds = el["bounds"];
                                int x = ((bounds?["left"]?.Value<int>() ?? 0) + (bounds?["right"]?.Value<int>() ?? 0)) / 2;
                                int y = ((bounds?["top"]?.Value<int>() ?? 0) + (bounds?["bottom"]?.Value<int>() ?? 0)) / 2;
                                return new AtxElement(client, baseUrl, x, y, el["text"]?.ToString() ?? "");
                            }
                        }
                    }
                }
                catch { }
                await Task.Delay(500);
            }
            throw new Exception("Không tìm thấy element theo selector.");
        }

        /// <summary>
        /// Hàm nội bộ - tìm nhiều element theo XPath
        /// </summary>
        private async Task<List<AtxElement>> FindElementsByXPath(string xpath)
        {
            var rpc = new
            {
                jsonrpc = "2.0",
                method = "xpath",
                @params = new { expression = xpath },
                id = 10
            };
            var res = await PostJson($"{baseUrl}/uiautomator", rpc);
            var result = res["result"] != null ? res["result"] as JArray : null;
            var elements = new List<AtxElement>();

            if (result != null)
            {
                foreach (var el in result)
                {
                    var bounds = el["bounds"];
                    int x = ((bounds?["left"]?.Value<int>() ?? 0) + (bounds?["right"]?.Value<int>() ?? 0)) / 2;
                    int y = ((bounds?["top"]?.Value<int>() ?? 0) + (bounds?["bottom"]?.Value<int>() ?? 0)) / 2;
                    elements.Add(new AtxElement(client, baseUrl, x, y, el["text"]?.ToString() ?? ""));
                }
            }
            return elements;
        }

        /// <summary>
        /// Hàm nội bộ - gửi POST request với object JSON
        /// </summary>
        private async Task<JObject> PostJson(string url, object data)
        {
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }
        /// <summary>
        /// Vuốt màn hình từ (x1, y1) tới (x2, y2), duration tính bằng giây (ví dụ 0.2)
        /// </summary>
        public async Task Swipe(int x1, int y1, int x2, int y2, double duration = 0.2)
        {
            var swipeData = new { x1, y1, x2, y2, duration };
            var content = new StringContent(JsonConvert.SerializeObject(swipeData), Encoding.UTF8, "application/json");
            await client.PostAsync($"{baseUrl}/swipe", content);
        }

        /// <summary>
        /// Chạm vào 1 điểm trên màn hình (tap toạ độ)
        /// </summary>
        public async Task Tap(int x, int y)
        {
            var tapData = new { x, y };
            var content = new StringContent(JsonConvert.SerializeObject(tapData), Encoding.UTF8, "application/json");
            await client.PostAsync($"{baseUrl}/touch/click", content);
        }

        /// <summary>
        /// Nhấn giữ (long tap) vào 1 điểm trên màn hình (tính bằng giây)
        /// </summary>
        public async Task LongTap(int x, int y, double duration = 1.0)
        {
            var data = new { x, y, duration };
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            await client.PostAsync($"{baseUrl}/touch/long_click", content);
        }

        /// <summary>
        /// Nhấn nút Back của Android (giống nút trở về vật lý)
        /// </summary>
        public async Task PressBack()
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            await client.PostAsync($"{baseUrl}/press/back", content);
        }

        /// <summary>
        /// Mở recent apps (đa nhiệm), đôi lúc sẽ có ích
        /// </summary>
        public async Task OpenRecentApps()
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            await client.PostAsync($"{baseUrl}/press/recent", content);
        }

        /// <summary>
        /// Home - trở về màn hình chính
        /// </summary>
        public async Task Home()
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            await client.PostAsync($"{baseUrl}/press/home", content);
        }

        /// <summary>
        /// Chụp ảnh màn hình và trả về mảng byte (có thể dùng File.WriteAllBytes để lưu ra file)
        /// </summary>
        public async Task<byte[]> Screenshot()
        {
            return await client.GetByteArrayAsync($"{baseUrl}/screenshot");
        }

        /// <summary>
        /// Lấy kích thước màn hình hiện tại (width, height)
        /// </summary>
        public async Task<(int width, int height)> GetScreenSize()
        {
            var res = await client.GetStringAsync($"{baseUrl}/info");
            var info = JObject.Parse(res);
            int width = info["display"]?["width"]?.Value<int>() ?? 0;
            int height = info["display"]?["height"]?.Value<int>() ?? 0;
            return (width, height);
        }
    }

    /// <summary>
    /// Lớp đại diện cho element (giống WebElement của Selenium). Có thể click, nhập text, lấy text.
    /// </summary>
    public class AtxElement
    {
        private readonly HttpClient client;
        private readonly string baseUrl;
        private readonly int x, y;
        public string Text { get; private set; }

        public AtxElement(HttpClient httpClient, string baseUrl, int x, int y, string text)
        {
            this.client = httpClient;
            this.baseUrl = baseUrl;
            this.x = x;
            this.y = y;
            this.Text = text;
        }

        /// <summary>
        /// Click vào element hiện tại (dựa trên toạ độ trung tâm element)
        /// </summary>
        public async Task Click()
        {
            var data = new { x, y };
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            await client.PostAsync(baseUrl + "/touch/click", content);
        }

        /// <summary>
        /// Nhập text vào element hiện tại (chỉ hỗ trợ với EditText)
        /// </summary>
        public async Task SendKeys(string text)
        {
            var data = new
            {
                jsonrpc = "2.0",
                method = "setText",
                @params = new
                {
                    selector = new { text = this.Text },
                    text = text
                },
                id = 5
            };
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            await client.PostAsync(baseUrl + "/uiautomator", content);
        }

        /// <summary>
        /// Lấy text của element hiện tại
        /// </summary>
        public string GetText()
        {
            return this.Text;
        }
    }
}
