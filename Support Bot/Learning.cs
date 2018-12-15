using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global

namespace Persiafighter.Applications.Support_Bot
{
    public sealed class Learning
    {
        [JsonIgnore] public static string FileName { get; } = "SupportBot_config/learning.json";

        public List<string> PreviousHelp { get; set; } = new List<string>();

        public void SaveJson()
        {
            var file = Path.Combine(AppContext.BaseDirectory, FileName);
            File.WriteAllText(file, ToJson());
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static void EnsureExists()
        {
            var file = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(file))
            {
                var path = Path.GetDirectoryName(file);
                if (!Directory.Exists(path) && path != null)
                    Directory.CreateDirectory(path);

                var config = new Learning();
                config.SaveJson();
                Console.WriteLine("Learning file created");
            }

            Console.WriteLine("Learning file verified");
        }

        public static Learning Load()
        {
            var file = Path.Combine(AppContext.BaseDirectory, FileName);
            return JsonConvert.DeserializeObject<Learning>(File.ReadAllText(file));
        }
    }
}