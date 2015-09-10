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
        const string User = "conrad";
        const string Password = "P@ssw0rd";

        static void Main(string[] args)
        {
            var url = Connect(Server, User, Password);
            LoadScreen(url, "C");
            LoadRecord(url, "TES");

            while (true)
                Poll(url);
        }

        static string Connect(string server, string userId, string password)
        {
            var url = server + "/client";
            var req = JsonConvert.SerializeObject(new { userId = userId, password = password });

            using (var w = new WebClient())
            {
                var data = w.UploadString(url, req);

                var r = JObject.Parse(data);
                var user = r.Value<int>("user");
                var id = r.Value<string>("id");

                Console.WriteLine("Connected to user {0}, id: {1}", user, id);
                return url + "/" + Uri.EscapeDataString(id);
            }
        }

        static void LoadScreen(string url, string screen)
        {
            url += "/input";
            var req = JsonConvert.SerializeObject(new { action = "loadScreen", id = screen });

            using (var w = new WebClient())
                w.UploadString(url, req);
        }

        static void LoadRecord(string url, string id)
        {
            url += "/input";
            var req = JsonConvert.SerializeObject(new { action = "load", id = id });
            using (var w = new WebClient())
                w.UploadString(url, req);
        }

        static void Poll(string url)
        {
            url += "/poll";
            using (var w = new WebClient())
            {
                var content = w.UploadString(url, "");
                if (content.Length == 0)
                {
                    Console.WriteLine("Empty response");
                    return;
                }

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
    }
}
