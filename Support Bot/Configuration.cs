using System;
using System.IO;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable once MemberCanBePrivate.Global

namespace Persiafighter.Applications.Support_Bot
{
    public sealed class Configuration
    {
        [JsonIgnore] public static string FileName { get; } = "SupportBot_config/configuration.json";

        public string Token { get; set; } = Guid.Empty.ToString();
        public ulong OwnerId { get; set; }
        public ulong SupportChannel { get; set; }
        public ulong LogChannel { get; set; }
        public ulong SupporterRole { get; set; }
        public ulong StaffRole { get; set; }

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

                var config = new Configuration();

                Console.WriteLine("Please enter your discord bot token: ");
                config.Token = Console.ReadLine();

                Console.WriteLine("Write the ID of the support channel: ");
                if (ulong.TryParse(Console.ReadLine(), out var m))
                    config.SupportChannel = m;
                else
                    Console.WriteLine("Not a valid ID.");

                Console.WriteLine("Write the ID of the log channel: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.LogChannel = m;
                else
                    Console.WriteLine("Not a valid ID.");

                Console.WriteLine("Write your discord ID: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.OwnerId = m;
                else
                    Console.WriteLine("Not a valid ID.");

                Console.WriteLine("Write the ID of the supporter role: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.SupporterRole = m;
                else
                    Console.WriteLine("Not a valid ID.");

                Console.WriteLine("Write the ID of the staff role: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.StaffRole = m;
                else
                    Console.WriteLine("Not a valid ID.");

                config.SaveJson();
                Console.WriteLine("First-Start Configuration Generated");
                return;
            }

            Console.WriteLine("Configuration Verified");
        }

        public static Configuration Load()
        {
            var file = Path.Combine(AppContext.BaseDirectory, FileName);
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(file));
        }
    }
}