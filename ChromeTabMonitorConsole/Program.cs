using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;


namespace ChromeTabMonitorConsole
{
    class Program
    {
        private static Timer timer;
        private static Hashtable threads = new Hashtable();
        private static Hashtable sentmessage_deduplication = new Hashtable();
        private static Dictionary<string, string> settings = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            loadSettings();

            Console.WriteLine("ChromeTabMonitor started. Press any key to exit...");


            timer = new Timer(TimerCallback, null, 0, 5000); // Run every 5 seconds

            Console.ReadKey();

            // Stop and clean up all threads
            foreach (DictionaryEntry entry in threads)
            {
                ((Thread)entry.Value).Abort();
            }
            threads.Clear();

            timer.Dispose();
        }

        /// <summary>
        /// Load "settings.ini" file in the application directory
        /// </summary>
        private static void loadSettings()
        {
            // Get the path to the INI file in the application folder
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Read all lines from the INI file
                string[] lines = File.ReadAllLines(filePath);

                // Parse each line of the INI file
                foreach (string line in lines)
                {
                    // Split each line into key-value pairs using '=' as delimiter
                    string[] parts = line.Split('=');

                    // Ensure that the line is in correct format (key=value)
                    if (parts.Length == 2)
                    {
                        // Trim whitespace from key and value
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        // Add key-value pair to the dictionary
                        settings[key] = value;
                    }
                }
            }
            else
            {
                Console.WriteLine("ERROR : file '" + filePath + "' not found.");
            }

        }


        private static void TimerCallback(object state)
        {
            // Use Chrome Remote Debugging Protocol to get the list of open tabs
            var tabs = GetOpenTabs();
            foreach (var tab in tabs)
            {
                string tabId = tab.id;
                string url = tab.url;
                string mytype = tab.type;
                string title = tab.title;
                string webSocketDebuggerUrl = tab.webSocketDebuggerUrl;


                // URL FILTERING
                bool to_monitor = false;
                if (settings.ContainsKey("filter_urls"))
                {
                    string[] urls = settings["filter_urls"].Split(";");
                    for (int u = 0; u < urls.Length; u++)
                    {
                        if (url.IndexOf(urls[u]) > 0)
                        {
                            to_monitor = true;
                            break;
                        }
                    }
                }

                if (!to_monitor)
                {
                    Console.WriteLine("WARNING : url '" + url + "' filtered, not monitored.");
                    continue;
                }
                else
                {
                    Console.WriteLine("INFO : start monitoring url '" + url + "' .");
                }

                // INSTANTIATING MONITORING THREADS

                if ((mytype == "page" || mytype == "iframe") && !threads.ContainsKey(tabId + "_CONSOLE") && (settings.ContainsKey("monitor_console") && settings["monitor_console"].ToLower() == "y"))
                {
                    //if (true) { 


                    // Spin a new thread for each tab
                    Thread thread_console = new Thread(() =>
                    {
                        try
                        {
                            ConnectAndReceive_ConsoleMessages(tabId, url, title, webSocketDebuggerUrl);

                        }
                        catch (Exception ex)
                        {
                            // Log errors to console
                            Console.WriteLine($"Error processing tab {tabId} - CONSOLE: {ex.Message}");
                        }


                    });



                    // Start the thread
                    thread_console.Start();

                    // Add the thread to the hashtable
                    threads[tabId + "_CONSOLE"] = thread_console;

                    // Log informational message
                    Console.WriteLine($"New thread created for tab {tabId}_CONSOLE");

                }


                if ((mytype == "page" || mytype == "iframe") && !threads.ContainsKey(tabId + "_PAGE") && (settings.ContainsKey("monitor_page") && settings["monitor_page"].ToLower() == "y"))
                {
                    Thread thread_page = new Thread(() =>
                    {

                        try
                        {

                            ConnectAndReceive_PageMessages(tabId, url, title, webSocketDebuggerUrl);
                        }
                        catch (Exception ex)
                        {
                            // Log errors to console
                            Console.WriteLine($"Error processing tab {tabId} - PAGE: {ex.Message}");
                        }


                    });

                    // Start the thread
                    thread_page.Start();

                    // Add the thread to the hashtable
                    threads[tabId + "_PAGE"] = thread_page;

                    // Log informational message
                    Console.WriteLine($"New thread created for tab {tabId}_PAGE");
                }


                if ((mytype == "page" || mytype == "iframe") && !threads.ContainsKey(tabId + "_PAGELAYOUT") && (settings.ContainsKey("monitor_pagelayout") && settings["monitor_pagelayout"].ToLower() == "y"))
                {
                    Thread thread_pagelayout = new Thread(() =>
                    {

                        try
                        {

                            ConnectAndReceive_PageLayoutMessages(tabId, webSocketDebuggerUrl);
                        }
                        catch (Exception ex)
                        {
                            // Log errors to console
                            Console.WriteLine($"Error processing tab {tabId} - PAGELAYOUT: {ex.Message}");
                        }
                    });
                    // Start the thread
                    thread_pagelayout.Start();

                    // Add the thread to the hashtable
                    threads[tabId + "_PAGELAYOUT"] = thread_pagelayout;

                    // Log informational message
                    Console.WriteLine($"New thread created for tab {tabId}_PAGELAYOUT");
                }


                if ((mytype == "page" || mytype == "iframe") && !threads.ContainsKey(tabId + "_PERFORMANCE") && (settings.ContainsKey("monitor_performance") && settings["monitor_performance"].ToLower() == "y"))
                {
                    Thread thread_performance = new Thread(() =>
                    {
                        try
                        {

                            ConnectAndReceive_PerformanceMessages(tabId, webSocketDebuggerUrl);
                        }
                        catch (Exception ex)
                        {
                            // Log errors to console
                            Console.WriteLine($"Error processing tab {tabId} - PERFORMANCE: {ex.Message}");
                        }
                    });
                    // Start the thread
                    thread_performance.Start();

                    // Add the thread to the hashtable
                    threads[tabId + "_PERFORMANCE"] = thread_performance;

                    // Log informational message
                    Console.WriteLine($"New thread created for tab {tabId}_PERFORMANCE");

                }


                if ((mytype == "page" || mytype == "iframe") && !threads.ContainsKey(tabId + "_PERFORMANCETIMELINE") && (settings.ContainsKey("monitor_performancetimeline") && settings["monitor_performancetimeline"].ToLower() == "y"))
                {
                    Thread thread_performancetimeline = new Thread(() =>
                {
                    try
                    {

                        ConnectAndReceive_PerformanceTimelineMessages(tabId, webSocketDebuggerUrl);
                    }
                    catch (Exception ex)
                    {
                        // Log errors to console
                        Console.WriteLine($"Error processing tab {tabId} - PERFORMANCETL: {ex.Message}");
                    }
                });

                    // Start the thread
                    thread_performancetimeline.Start();

                    // Add the thread to the hashtable
                    threads[tabId + "_PERFORMANCETIMELINE"] = thread_performancetimeline;

                    // Log informational message
                    Console.WriteLine($"New thread created for tab {tabId}_PERFORMANCETIMELINE");

                }



            }

            try
            {
                // Check and remove terminated threads
                foreach (DictionaryEntry entry in threads)
                {
                    if (!((Thread)entry.Value).IsAlive)
                    {
                        threads.Remove(entry.Key);
                        Console.WriteLine($"Thread for tab {entry.Key} terminated");
                    }
                }
            }
            catch (Exception e)
            {
                //NOP
            }
        }

        private static List<TabInfo> GetOpenTabs()
        {
            List<TabInfo> tabs = new List<TabInfo>();
            using (HttpClient client = new HttpClient())
            {
                string url = "http://localhost:9227/json";
                HttpResponseMessage response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = response.Content.ReadAsStringAsync().Result;
                    tabs = JsonSerializer.Deserialize<List<TabInfo>>(jsonResponse);
                }
                else
                {
                    // Log error if HTTP request fails
                    Console.WriteLine($"Failed to fetch open tabs: {response.StatusCode}");
                }
            }
            return tabs;
        }

        /// <summary>
        /// Connect to the tab websocket to receive console messages.
        /// </summary>
        /// <param name="tabId">ID of the tab (hash)</param>
        /// <param name="webSocketDebuggerUrl">The websocket of the RDP to connect to</param>
        static void ConnectAndReceive_ConsoleMessages(string tabId, string url, string title, string webSocketDebuggerUrl)
        {
            using (var clientWebSocket = new ClientWebSocket())
            {
                Uri uri = new Uri(webSocketDebuggerUrl);
                clientWebSocket.ConnectAsync(uri, CancellationToken.None).Wait();

                // Send Console.enable command
                string message = "{\"id\":1,\"method\":\"Console.enable\"}";
                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                // Receive messages
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);
                    WebSocketReceiveResult result = clientWebSocket.ReceiveAsync(buffer, CancellationToken.None).Result;
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        // Print in console the message
                        Console.WriteLine($"Received message for tab {tabId} - CONSOLE : {receivedMessage}");
                        // write on console dedicated file
                        AppendToFile("c:\\temp\\" + tabId + "_CONSOLE.txt", receivedMessage);

                        if (IsValidJson(message))
                        {
                            //is complete, send to elastic

                            SendToBonsaiWrapper("Console", url, title, receivedMessage);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Connect to the tab websocket to receive Page messages
        /// </summary>
        /// <param name="tabId">ID of the tab (hash)</param>
        /// <param name="webSocketDebuggerUrl">The websocket of the RDP to connect to</param>
        static void ConnectAndReceive_PageMessages(string tabId, string url, string title, string webSocketDebuggerUrl)
        {
            using (var clientWebSocket = new ClientWebSocket())
            {
                Uri uri = new Uri(webSocketDebuggerUrl);
                clientWebSocket.ConnectAsync(uri, CancellationToken.None).Wait();

                // Send Console.enable command
                string message = "{\"id\":1,\"method\":\"Page.enable\"}";
                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                // Receive messages
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);
                    WebSocketReceiveResult result = clientWebSocket.ReceiveAsync(buffer, CancellationToken.None).Result;
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        // Print in console the message
                        Console.WriteLine($"Received message for tab {tabId} - PAGE : {receivedMessage}");
                        // write on console dedicated file
                        AppendToFile("c:\\temp\\" + tabId + "_PAGE.txt", receivedMessage);

                        if (IsValidJson(message))
                        {
                            //is complete, send to elastic

                            SendToBonsaiWrapper("Page", url, title, receivedMessage);
                        }
                        Thread.Sleep(100);
                    }
                }


            }
        }

        /// <summary>
        /// Connect to the tab websocket to receive Page messages
        /// </summary>
        /// <param name="tabId">ID of the tab (hash)</param>
        /// <param name="webSocketDebuggerUrl">The websocket of the RDP to connect to</param>
        static void ConnectAndReceive_PageLayoutMessages(string tabId, string webSocketDebuggerUrl)
        {
            using (var clientWebSocket = new ClientWebSocket())
            {
                Uri uri = new Uri(webSocketDebuggerUrl);
                clientWebSocket.ConnectAsync(uri, CancellationToken.None).Wait();


                string message = "{\"id\":1,\"method\":\"Page.enable\"}";
                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();


                string message2 = "{\"id\":2,\"method\":\"Page.getLayoutMetrics\"}";
                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message2)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                // Receive messages
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);
                    WebSocketReceiveResult result = clientWebSocket.ReceiveAsync(buffer, CancellationToken.None).Result;
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        // Print in console the message
                        Console.WriteLine($"Received message for tab {tabId} - PAGE LAYOUT : {receivedMessage}");
                        // write on console dedicated file
                        AppendToFile("c:\\temp\\" + tabId + "_PAGELAYOUT.txt", receivedMessage);
                    }
                    Thread.Sleep(100);
                }
            }
        }


        /// <summary>
        /// Connect to the tab websocket to receive Performance
        /// </summary>
        /// <param name="tabId">ID of the tab (hash)</param>
        /// <param name="webSocketDebuggerUrl">The websocket of the RDP to connect to</param>
        static void ConnectAndReceive_PerformanceMessages(string tabId, string webSocketDebuggerUrl)
        {
            using (var clientWebSocket = new ClientWebSocket())
            {
                Uri uri = new Uri(webSocketDebuggerUrl);
                clientWebSocket.ConnectAsync(uri, CancellationToken.None).Wait();



                string message = "{\"id\":1,\"method\":\"Performance.enable\"}";
                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();


                string message2 = "{\"id\":2,\"method\":\"Performance.getMetrics\"}";
                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message2)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                // Receive messages
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2048]);
                    WebSocketReceiveResult result = clientWebSocket.ReceiveAsync(buffer, CancellationToken.None).Result;
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        // Print in console the message
                        if (receivedMessage != "{\"id\":2,\"result\":{\"metrics\":[]}}")
                        {
                            Console.WriteLine($"Received message for tab {tabId} - PERFORMANCE : {receivedMessage}");
                            // write on console dedicated file
                            AppendToFile("c:\\temp\\" + tabId + "_PERFORMANCE.txt", receivedMessage);
                        }
                    }
                    Thread.Sleep(100);
                }
            }
        }


        /// <summary>
        /// Connect to the tab websocket to receive PerformanceTimeline
        /// </summary>
        /// <param name="tabId">ID of the tab (hash)</param>
        /// <param name="webSocketDebuggerUrl">The websocket of the RDP to connect to</param>
        static void ConnectAndReceive_PerformanceTimelineMessages(string tabId, string webSocketDebuggerUrl)
        {
            //webSocketDebuggerUrl = "ws://localhost:9227/devtools/browser/6418786a-7de4-4a57-8424-13c88b22917a";
            using (var clientWebSocket = new ClientWebSocket())
            {
                Uri uri = new Uri(webSocketDebuggerUrl);
                clientWebSocket.ConnectAsync(uri, CancellationToken.None).Wait();



                //                string message = "{\"id\":1,\"method\":\"PerformanceTimeline.enable\"}";
                //                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                string[] events = new string[] { "navigation", "mark", "measure" };
                events = new string[] { "ResourceTiming" };

                events = new string[] {
                    "Frame",
                    "FrameStartedLoading",
                    "FrameNavigated",
                    "FrameStoppedLoading",
                    "FrameScheduledNavigation",
                    "FrameClearedScheduledNavigation",
                    "FrameResized",
                    "ScriptParsed",
                    "ScriptFailedToParse",
                    "TimeStamp",
                    "ConsoleProfile",
                    "Layout",
                    "Paint",
                    "Composite",
                    "RenderingFrame",
                    "RenderingFrameCanceled",
                    "RenderingFrameScheduled",
                    "TimerFire",
                    "TimerInstall",
                    "TimerRemove",
                    "TimerPaused",
                    "TimerSnapshot",
                    "FrameRequestedNavigation",
                    "LayoutShift",
                    "FirstInputDelay",
                    "LongTask",
                    "MainFrameReadyForNavigation",
                    "FrameScheduledReload"
                };

                events = new string[] { "TimelineEvent" };

                var messageObject = new
                {
                    id = 1,
                    method = "PerformanceTimeline.enable",
                    @params = new
                    {
                        eventTypes = events
                    }
                };
                // Serialize the message object to JSON
                string messageJson = JsonSerializer.Serialize(messageObject);

                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(messageJson)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                // Serialize the message object to JSON
                //string messageJson = Newtonsoft.Json.JsonConvert.SerializeObject(messageObject);

                //                string message2 = "{\"id\":2,\"method\":\"Performance.getMetrics\"}";
                //                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message2)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                // Receive messages
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult result = clientWebSocket.ReceiveAsync(buffer, CancellationToken.None).Result;
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        // Print in console the message
                        if (receivedMessage != "{\"id\":1,\"result\":{\"metrics\":[]}}")
                        {
                            Console.WriteLine($"Received message for tab {tabId} - PERFORMANCETL : {receivedMessage}");
                            // write on console dedicated file
                            AppendToFile("c:\\temp\\" + tabId + "_PERFORMANCETL.txt", receivedMessage);
                        }
                    }
                    Thread.Sleep(100);
                }
            }
        }


        static void AppendToFile(string filePath, string content)
        {
            // Check if the file exists, if not, create it
            if (!File.Exists(filePath))
            {
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    sw.WriteLine(content);
                }
            }
            else
            {
                // Append the content to the existing file
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.WriteLine(content);
                }
            }
        }




        static bool IsValidJson(string jsonString)
        {
            try
            {
                JsonDocument.Parse(jsonString);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }



        static void SendToBonsaiWrapper(string payload_type, string url, string page_title, string jsonPayload)
        {
            // purge useless answer from Chrome RDP
            //{ "id":1,"result":{ } }
            if (jsonPayload.Contains(",\"result\":{}}"))
            {
                Console.WriteLine("SKIPPED - not sent to elasticsearch : " + jsonPayload);
                return;
            }



            // dedupe already sent message for this instance
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(jsonPayload);
                byte[] hashBytes = md5.ComputeHash(inputBytes);


                if (sentmessage_deduplication.ContainsKey(Convert.ToHexString(hashBytes)))
                {
                    Console.WriteLine("SKIPPED - already sent to elasticsearch : " + jsonPayload);
                    return;
                }
                else
                {
                    sentmessage_deduplication[Convert.ToHexString(hashBytes)] = "x";
                }
            }

            int retry = 4;

            // default is to anonymize
            string hostName = "xxxxx";
            string userName = "xxxxx";
            if (settings.ContainsKey("anonymize_user") & settings["anonymize_user"].ToLower() == "n")
                hostName = Dns.GetHostName();
            if (settings.ContainsKey("anonymize_host") & settings["anonymize_user"].ToLower() == "n")
                userName = Environment.UserName;



            string tags = "";
            if (settings.ContainsKey("tags"))
            {
                tags = JsonSerializer.Serialize(settings["tags"].Split(";"));
            }


            string jsonData = "{  \"username\" : \"" + userName + "\",  \"hostname\" : \"" + hostName + "\", \"tags\" : " + tags + ", \"page_title\" : \"" + page_title + "\", \"payload_type\" : \"" + payload_type + "\", \"ts\" : " + DateTimeOffset.Now.ToUnixTimeMilliseconds() + ", \"payload\" : " + jsonPayload + "  } ";

            for (int i = retry; i >= 0; i--)
            {
                int retval = SendToBonsai(jsonData);
                if (retval == 0)
                    return;
                if (retval == 1)
                    return;

                Thread.Sleep((int)new Random().Next(2000, 6000));

            }

            Console.WriteLine("SendToBonsai - no more retry");


        }
        static int SendToBonsai(string jsonData)
        {
            /*
            // Your Bonsai.io endpoint URL
            string bonsaiUrl = "https://7pemvjg1fo:6j97b8dud4@boonzai-2020995173.eu-central-1.bonsaisearch.net:443";
            string index = "chrome_rdp";

            // Your Bonsai.io access key and secret
            string accessKey = "7pemvjg1fo";
            string secret = "6j97b8dud4"; */

            string bonsaiUrl = "";
            string index = "";

            // Your Bonsai.io access key and secret
            string accessKey = "";
            string secret = "";

            if (settings.ContainsKey("opensearch_url") & settings.ContainsKey("opensearch_index") & settings.ContainsKey("opensearch_accesskey") & settings.ContainsKey("opensearch_secret"))
            {

                index = settings["opensearch_index"];
                accessKey = settings["opensearch_accesskey"];
                secret = settings["opensearch_secret"];

                bonsaiUrl = settings["opensearch_url"];
                bonsaiUrl = bonsaiUrl.Substring(0, bonsaiUrl.IndexOf("//") + 2) + accessKey + ":" + secret + "@" + bonsaiUrl.Substring(bonsaiUrl.IndexOf("//") + 2);

            }
            else
            {
                return 9;
            }


            // Create an HttpClient instance
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                // Concatenate the access key and secret with a colon separator
                string credentials = $"{accessKey}:{secret}";

                // Convert the credentials to base64
                string base64Credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));

                // Set the Elasticsearch access key and secret in the request headers
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {base64Credentials}");

                // Create the content to send
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                try
                {
                    // Send the POST request to Elasticsearch
                    HttpResponseMessage response = httpClient.PutAsync(bonsaiUrl + "/" + index + "/doc/" + DateTimeOffset.Now.ToUnixTimeMilliseconds(), content).Result;

                    // Read the response content
                    string responseContent = response.Content.ReadAsStringAsync().Result;

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Data sent successfully to target! : " + jsonData);
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to send data '" + jsonData + "'. Status code: {response.StatusCode}, Reason: {responseContent}");
                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return 2;
                }
            }
        }


    }

    public class TabInfo
    {
        public string id { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public string webSocketDebuggerUrl { get; set; }
    }
}
