using AtxSharp;
using SharpAdbClient;
using System.Text;

namespace AtxTestMultiDevice
{
    public static class AtxResourceManager
    {
        /// <summary>
        /// Đảm bảo có đủ atx-agent và app-uiautomator.apk trong thư mục appatx. Nếu thiếu sẽ tự động tải bằng HttpClient.
        /// </summary>
        public static async Task EnsureAtxResources()
        {
            var projectDir = Directory.GetCurrentDirectory();
            var atxDir = Path.Combine(projectDir, "appatx");
            var apkPath = Path.Combine(atxDir, "app-uiautomator.apk");
            var agentPath = Path.Combine(atxDir, "atx-agent");

            if (!Directory.Exists(atxDir))
            {
                Console.WriteLine($"[Init] Tạo thư mục lưu file: {atxDir}");
                Directory.CreateDirectory(atxDir);
            }

            var fileList = new[]
            {
                new { Path = apkPath, Url = "https://github.com/lowji194/AtxSharp/raw/refs/heads/main/app/app-uiautomator.apk", Label = "app-uiautomator.apk" },
                new { Path = agentPath, Url = "https://github.com/lowji194/AtxSharp/raw/refs/heads/main/app/atx-agent", Label = "atx-agent" }
            };

            using (var httpClient = new HttpClient())
            {
                foreach (var file in fileList)
                {
                    if (!File.Exists(file.Path))
                    {
                        Console.WriteLine($"[Setup] Đang tải {file.Label} về...");
                        try
                        {
                            var response = await httpClient.GetAsync(file.Url);
                            response.EnsureSuccessStatusCode();
                            var fileBytes = await response.Content.ReadAsByteArrayAsync();
                            await File.WriteAllBytesAsync(file.Path, fileBytes);
                            Console.WriteLine($"[OK] Đã tải xong: {file.Label}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Error] Lỗi tải {file.Label}: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Check] Đã có file: {file.Label}");
                    }
                }
            }
        }
    }

    class Program
    {
        private static AdbClient adbClient = new AdbClient();
        private static readonly Dictionary<string, (int Line, string Message)> deviceConsoleLines = new Dictionary<string, (int, string)>();
        private static readonly object consoleLock = new object();
        private static int nextLine = 0;

        private static string ATX_DIR = Path.Combine(Directory.GetCurrentDirectory(), "appatx");
        private static string ATX_AGENT_PATH = Path.Combine(ATX_DIR, "atx-agent");
        private static string ATX_APK_PATH = Path.Combine(ATX_DIR, "app-uiautomator.apk");
        private static int ATX_AGENT_PORT_BASE = 7912;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Khởi động tự động hóa atx-agent...");

            await AtxResourceManager.EnsureAtxResources();

            List<DeviceData> devices;
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Đang tải danh sách thiết bị...");
                devices = adbClient.GetDevices().Where(d => d.State == DeviceState.Online).ToList();
                if (devices.Count == 0)
                {
                    Console.WriteLine("[LỖI] Không tìm thấy thiết bị nào!");
                }
                else
                {
                    Console.WriteLine("[INFO] Danh sách thiết bị:");
                    for (int i = 0; i < devices.Count; i++)
                        Console.WriteLine($"[{i + 1}] {devices[i].Serial} - ({devices[i].State})");
                }

                Console.WriteLine("\nNhấn [1] để load lại, hoặc [Enter] để tiếp tục...");
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.D1 || key == ConsoleKey.NumPad1) continue;
                else if (key == ConsoleKey.Enter) break;
            }

            if (devices.Count == 0)
            {
                WriteToConsole("[LỖI] Không có thiết bị, thoát.", null!);
                return;
            }

            Dictionary<string, int> devicePortMap = new Dictionary<string, int>();
            for (int i = 0; i < devices.Count; i++)
                devicePortMap[devices[i].Serial] = ATX_AGENT_PORT_BASE + i;

            lock (consoleLock)
            {
                nextLine = Console.CursorTop + 1;
                foreach (var device in devices)
                    deviceConsoleLines[device.Serial] = (nextLine++, "");
            }

            var tasks = new Task[devices.Count];
            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                int port = devicePortMap[device.Serial];
                tasks[i] = Task.Run(() => ProcessDevice(device, port));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task ProcessDevice(DeviceData device, int atxPort)
        {
            try
            {
                bool ok = await PrepareAtxAgent(device, atxPort); // Cập nhật để gọi async
                if (!ok)
                {
                    WriteToConsole($"[LỖI] [{device.Serial}] Lỗi chuẩn bị atx-agent!", device.Serial);
                    return;
                }
                WriteToConsole($"[INFO] [{device.Serial}] Sẵn sàng atx-agent trên port {atxPort}", device.Serial);

                var atx = new AtxAgentDriver(atxPort, 30);
                int job = 0, money = 0;

                while (true)
                {
                    try
                    {
                        var popup = await atx.FindElements(By.XPath("//android.widget.ScrollView"));
                        if (popup.Count > 0)
                        {
                            var (w, h) = await atx.GetScreenSize();
                            await atx.Swipe(w / 2, (int)(h * 0.8), w / 2, (int)(h * 0.3), 0.35);
                            await Task.Delay(500);
                        }

                        var quaylai = await atx.FindElements(By.XPath("//android.widget.Button[@content-desc='Quay về'] | //android.widget.Button[@content-desc='Đồng ý quy định']"));
                        if (quaylai.Count > 0)
                            await quaylai[0].Click();

                        await Task.Delay(3000);

                        var lamjob = await atx.FindElements(By.XPath("//android.widget.Button[@content-desc='Làm']"));
                        if (lamjob.Count > 0)
                        {
                            WriteToConsole($"[JOB] [{device.Serial}] [{money} - {job + 1}] Bắt đầu Job!", device.Serial);
                            await lamjob[0].Click();
                            await Task.Delay(5000);

                            var xongBtn = await atx.FindElement(By.XPath("//android.widget.Button[@content-desc='Xong']"));
                            if (xongBtn != null) await xongBtn.Click();

                            var quayveBtn = await atx.FindElement(By.XPath("//android.widget.Button[@content-desc='Quay về']"));
                            if (quayveBtn != null) await quayveBtn.Click();

                            var log = await atx.FindElement(By.XPath("//android.view.View[@content-desc]"));
                            if (log != null)
                                WriteToConsole($"[JOB] [{device.Serial}] [{money} - {job + 1}] {log.Text}", device.Serial);
                        }
                        else
                        {
                            WriteToConsole($"[JOB] [{device.Serial}] [{money} - {job + 1}] Hết Job, tìm Job mới!", device.Serial);
                            var nhanjob = await atx.FindElements(By.XPath("//android.widget.ImageView[@content-desc='Comment Facebook, nhận 50đ']"));
                            if (nhanjob.Count > 0)
                            {
                                await nhanjob[0].Click();
                                var quayveBtn = await atx.FindElement(By.XPath("//android.widget.Button[@content-desc='Quay về']"));
                                if (quayveBtn != null) await quayveBtn.Click();
                            }
                        }

                        job++;
                    }
                    catch (Exception ex)
                    {
                        WriteToConsole($"[LỖI] [{device.Serial}] Lỗi thao tác: {ex.Message}", device.Serial);
                        await Task.Delay(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToConsole($"[LỖI] [{device.Serial}] Lỗi tổng: {ex.Message}", device.Serial);
            }
        }

        /// <summary>
        /// Chuẩn bị, push, cài đặt và khởi động atx-agent trên từng thiết bị
        /// </summary>
        private static async Task<bool> PrepareAtxAgent(DeviceData device, int atxPort)
        {
            try
            {
                var receiver = new ConsoleOutputReceiver();
                adbClient.ExecuteRemoteCommand("pm list packages", device, receiver);
                var packages = receiver.ToString().Split('\n').Select(l => l.Trim()).ToList();

                if (!packages.Any(pkg => pkg.Contains("com.github.uiautomator")))
                {
                    WriteToConsole($"[INFO] [{device.Serial}] Cài atx.apk...", device.Serial);
                    using (var apkStream = File.OpenRead(ATX_APK_PATH))
                    {
                        adbClient.Install(device, apkStream, "-r");
                    }
                }

                receiver = new ConsoleOutputReceiver();
                adbClient.ExecuteRemoteCommand("ls /data/local/tmp/atx-agent", device, receiver);
                if (receiver.ToString().ToLower().Contains("no such file"))
                {
                    WriteToConsole($"[INFO] [{device.Serial}] Push atx-agent...", device.Serial);
                    using (var sync = new SyncService(adbClient, device))
                    using (var stream = File.OpenRead(ATX_AGENT_PATH))
                    {
                        sync.Push(stream, "/data/local/tmp/atx-agent", 0755, DateTime.Now, null, CancellationToken.None);
                    }
                    adbClient.ExecuteRemoteCommand("chmod 755 /data/local/tmp/atx-agent", device, null);
                }

                WriteToConsole($"[INFO] [{device.Serial}] Khởi động uiautomator...", device.Serial);
                adbClient.ExecuteRemoteCommand("am instrument -w -r -e debug false -e class com.github.uiautomator.stub.Stub com.github.uiautomator.test/android.support.test.runner.AndroidJUnitRunner", device, null);

                WriteToConsole($"[INFO] [{device.Serial}] Khởi động atx-agent...", device.Serial);
                adbClient.ExecuteRemoteCommand("pkill atx-agent", device, null);
                adbClient.ExecuteRemoteCommand("nohup /data/local/tmp/atx-agent server --nouia --addr :7912 >/dev/null 2>&1 &", device, null);

                // Forward port đúng cách!
                adbClient.CreateForward(device, $"tcp:{atxPort}", "tcp:7912", true);

                // Kiểm tra agent đã chạy
                await Task.Delay(2000); // Thay Thread.Sleep(2000)
                using (var httpClient = new HttpClient())
                {
                    var url = $"http://127.0.0.1:{atxPort}/info";
                    for (int retry = 0; retry < 5; retry++)
                    {
                        try
                        {
                            var res = await httpClient.GetStringAsync(url);
                            if (!string.IsNullOrEmpty(res)) return true;
                        }
                        catch
                        {
                            await Task.Delay(1000); // Thay Thread.Sleep(1000)
                        }
                    }
                }
                WriteToConsole($"[LỖI] [{device.Serial}] Không kết nối được atx-agent trên port {atxPort}!", device.Serial);
                return false;
            }
            catch (Exception ex)
            {
                WriteToConsole($"[LỖI] [{device.Serial}] Lỗi chuẩn bị atx-agent: {ex.Message}", device.Serial);
                return false;
            }
        }

        private static void WriteToConsole(string message, string deviceSerial)
        {
            lock (consoleLock)
            {
                if (message.Contains("[INFO]"))
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else if (message.Contains("[JOB]"))
                    Console.ForegroundColor = ConsoleColor.Green;
                else if (message.Contains("[LỖI]"))
                    Console.ForegroundColor = ConsoleColor.Red;
                else
                    Console.ForegroundColor = ConsoleColor.White;

                if (string.IsNullOrEmpty(deviceSerial))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                    nextLine++;
                    Console.ResetColor();
                    return;
                }

                if (!deviceConsoleLines.ContainsKey(deviceSerial))
                    deviceConsoleLines[deviceSerial] = (nextLine++, "");

                var (line, _) = deviceConsoleLines[deviceSerial];
                var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                Console.SetCursorPosition(0, line);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, line);
                Console.WriteLine(formattedMessage);
                deviceConsoleLines[deviceSerial] = (line, formattedMessage);
                Console.SetCursorPosition(0, nextLine);
                Console.ResetColor();
            }
        }
    }
}