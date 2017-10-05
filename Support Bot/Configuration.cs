using Discord.Rest;
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
        public string AdminCommandPrefix { get; set; } = "/";
        public string Token { get; set; } = "";
        public List<PastebinErrors> Pastebins { get; set; } = new List<PastebinErrors>();
        public List<Declare> UndecidedQuestions { get; set; } = new List<Declare>();
        public List<Answer> AnsweredQuestions { get; set; } = new List<Answer>();

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
    public sealed class Declare
    {
        public int Index { get; set; }
        public string Question { get; set; }
        public Answer Answer { get; set; }
        public Message DecisionMSG { get; set; }

        public Declare() { }
        public Declare(int i, string q, ulong id, Message decide, Message a)
        {
            Index = i;
            Question = q;
            Answer = new Answer(a, id);
            DecisionMSG = decide;
        }
    }
    public sealed class Answer
    {
        public ulong MessageID { get; set; }
        public Message AnswerMSG { get; set; }

        public Answer() { }
        public Answer(Message a, ulong i)
        {
            AnswerMSG = a;
            MessageID = i;
        }
    }
    public sealed class Message
    {
        public ulong MSG { get; set; }
        public ulong Channel { get; set; }
        public ulong Guild { get; set; }

        public Message() { }
        public Message(ulong m, ulong c, ulong g)
        {
            MSG = m;
            Channel = c;
            Guild = g;
        }
    }
}
