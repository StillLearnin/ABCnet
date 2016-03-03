using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace ABCNet
{
    class Program
    {
        const string Server = "http://localhost:3998";
        const string User = "test";
        const string Password = "pass123@Test";
        static string baseUrl;
        static void Main(string[] args)
        {
            baseUrl = Connect(Server, User, Password);

            LoadScreen("C");
            LoadRecord("HERMER");

            while (true)
                Poll();
        }

        static string Connect(string server, string userId, string password)
        {
            var url = server + "/client";
            var req = JsonConvert.SerializeObject(new { userId = userId, password = password });

            using (var w = new WebClient())
            {
                LogIt(Environment.NewLine + "Connect Request");
                LogIt("URL: " + url);
                LogIt("Post Data: " + req);
                var data = w.UploadString(url, req);
                LogIt("Response: " + data + Environment.NewLine);

                var r = JObject.Parse(data);
                var user = r.Value<int>("user");
                var id = r.Value<string>("id");

                Console.WriteLine("Connected to user {0}, id: {1}", user, id);
                return url + "/" + Uri.EscapeDataString(id);
            }
        }

        static void LoadScreen(string screen)
        {
            var req = JsonConvert.SerializeObject(new { action = "loadScreen", id = screen });
            PostRequest("Load Screen", req);
        }

        static void LoadRecord(string id)
        {
            var req = JsonConvert.SerializeObject(new { action = "load", id = id });
            PostRequest("Load Record", req);
        }

        private static void PostRequest(string action, string req)
        {
            try
            {
                string url = baseUrl + "/input";
                using (var w = new WebClient())
                {
                    LogIt(action);
                    LogIt("URL: " + url);
                    LogIt("Post Data: " + req + Environment.NewLine);
                    w.UploadString(url, req);
                }

            }
            catch (Exception e)
            {
                LogIt("Error: " + e.Message);
            }
        }

        static void Poll()
        {
            try
            {
                var url = baseUrl + "/poll";
                using (var w = new WebClient())
                {
                    LogIt("Poll for Response");
                    LogIt("URL: " + url);

                    var content = w.UploadString(url, "");

                    if (content.Length == 0)
                    {
                        LogIt("Response: Empty response" + Environment.NewLine);
                        Console.WriteLine("Empty response");
                        return;
                    }

                    LogIt("Response: " + JToken.Parse(content).ToString() + Environment.NewLine);

                    var resp = JArray.Parse(content);
                    foreach (var item in resp)
                    {
                        var obj = (JObject)item;
                        var type = obj.Value<string>("type");
                        var data = obj.Value<JToken>("data");
                        if (type == "command")
                            Command(obj.Value<string>("command"), data);
                        else if (type == "data")
                            Data((JObject)data);
                    }
                }

            }
            catch (Exception e)
            {
                LogIt("Error: " + e.Message);
            }
        }

        static void Command(string command, JToken data)
        {
            if (command == "screenChanged")
            {
                var screen = ((JObject)data).Value<string>("screen");
                Console.WriteLine("Screen changed to {0}", screen);
            }
        }

        static void Data(JObject data)
        {
            var name = data.Value<string>("tableName");
            Console.WriteLine("Received data for {0}", name);

            if (name == "Customer")
            {
                var header = data.Value<JObject>("header");
                var customerId = header.Value<string>("Id");
                var customerName = header.Value<string>("Name");
                Console.WriteLine("Name for {0}: {1}", customerId, customerName);
            }
        }

        static void LogIt(string data)
        {
            using (StreamWriter sw = File.AppendText("ABCnetLog.txt"))
            {
                sw.WriteLine(data);
            }
        }
    }
}
