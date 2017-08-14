using Newtonsoft.Json;
using Persiafighter.Applications.Support_Bot.Classes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Persiafighter.Applications.Support_Bot
{
    public class Configuration
    {
        [JsonIgnore]
        public static string FileName { get; private set; } = "config/configuration.json";
        public string Prefix { get; set; } = "/";
        public string Token { get; set; } = "";
        public List<ulong> Admins { get; set; } = new List<ulong>() { };
        public List<ServerSampleMessages> Servers { get; set; } = new List<ServerSampleMessages>();

        public static void EnsureExists()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(file))
            {
                string path = Path.GetDirectoryName(file);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var config = new Configuration();

                Console.WriteLine("Please enter your token: ");
                string token = Console.ReadLine();

                config.Token = token;
                config.SaveJson();
            }
            Console.WriteLine("Configuration Loaded");
        }
        
        public void SaveJson()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            File.WriteAllText(file, ToJson());
        }
        
        public static Configuration Load()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(file));
        }
        
        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}

namespace Persiafighter.Applications.Support_Bot.Classes
{
    public class ServerSampleMessages
    {
        public ulong ServerID { get; set; } = 0;
        public bool AnalyzePastebins { get; set; } = false;
        public List<SampleMessages> Messages { get; set; } = new List<SampleMessages>();
    }

    public class SampleMessages
    {
        public List<string> ValidKeyWords { get; set; } = new List<string>();
        public string Message { get; set; } = "";
    }
}
