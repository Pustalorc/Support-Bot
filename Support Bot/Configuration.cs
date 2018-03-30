using Discord;
using Newtonsoft.Json;
using Pustalorc.Applications.Support_Bot.Classes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Pustalorc.Applications.Support_Bot
{
    public sealed class Configuration
    {
        [JsonIgnore]
        public static string FileName { get; private set; } = "config/configuration.json";
        public string Token { get; set; } = "";
        public ulong OwnerID { get; set; } = 0;
        public ulong GeneralChannel { get; set; } = 0;
        public ulong SupportChannel { get; set; } = 0;
        public ulong SupporterRole { get; set; } = 0;
        public ulong StaffRole { get; set; } = 0;

        public static void EnsureExists()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(file))
            {
                string path = Path.GetDirectoryName(file);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var config = new Configuration();

                Console.WriteLine("Please enter your discord bot token: ");
                config.Token = Console.ReadLine();

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
    public sealed class Learning
    {
        [JsonIgnore]
        public static string FileName { get; private set; } = "config/MemoryAndLearning.json";
        public List<Answered> Answers { get; set; } = new List<Answered>();
        public List<Deciding> UndecidedQuestions { get; set; } = new List<Deciding>();
        public List<string> NotQuestions { get; set; } = new List<string>();

        public static void EnsureExists()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(file))
            {
                string path = Path.GetDirectoryName(file);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var config = new Learning();
                config.SaveJson();
            }
        }
        public void SaveJson()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            File.WriteAllText(file, ToJson());
        }
        public static Learning Load()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            return JsonConvert.DeserializeObject<Learning>(File.ReadAllText(file));
        }
        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}

namespace Pustalorc.Applications.Support_Bot.Classes
{
    public sealed class Answered
    {
        public List<string> Questions { get; set; } = new List<string>();
        public string Answer { get; set; } = "";
    }
    public sealed class Data
    {
        public double Similarity;
        public string Phrase, Answer;
    }
    public sealed class Deciding
    {
        public string Question { get; set; }
        public ulong ID { get; set; }
    }
    public sealed class MSG
    {
        public Deciding Decision;
        public IMessage msg;
        public ulong Channel;
        public ulong Guild;
    }
}
