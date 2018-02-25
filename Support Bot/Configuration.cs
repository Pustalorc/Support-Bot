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
    public sealed class Learning
    {
        [JsonIgnore]
        public static string FileName { get; private set; } = "config/MemoryAndLearning.json";
        public List<Answered> Answers { get; set; } = new List<Answered>();
        public List<string> NotQuestions { get; set; } = new List<string>();
        public List<Deciding> UndecidedQuestions { get; set; } = new List<Deciding>();

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
        public string Mention { get; set; }
        public Message DecidingMessage { get; set; }
        public Message NotFoundMessage { get; set; }

        public Deciding() { }
        public Deciding(string q, ulong id, string mention, Message dms, Message nfm)
        {
            Question = q;
            ID = id;
            Mention = mention;
            DecidingMessage = dms;
            NotFoundMessage = nfm;
        }
    }
    public sealed class Message
    {
        public ulong MessageID { get; set; }
        public ulong ChannelID { get; set; }
        public ulong GuildID { get; set; }

        public Message() { }
        public Message(ulong mid, ulong cid, ulong gid)
        {
            MessageID = mid;
            ChannelID = cid;
            GuildID = gid;
        }
    }
}
