using Newtonsoft.Json;
using Persiafighter.Applications.Support_Bot.Classes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Persiafighter.Applications.Support_Bot
{
    public sealed class Configuration
    {
        [JsonIgnore]
        public static string FileName { get; private set; } = "config/configuration.json";
        public string Prefix { get; set; } = "/";
        public string Token { get; set; } = "";
        public List<ulong> Admins { get; set; } = new List<ulong>() { };
        public bool AnalyzePastebins { get; set; } = false;
        public List<PastebinErrors> Pastebins { get; set; } = new List<PastebinErrors>();

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
    public sealed class PastebinErrors
    {
        public string Identifier { get; set; } = "";
        public string Answer { get; set; } = "";
        public int NameIndex { get; set; } = -1;
    }
}
