using DevOne.Security.Cryptography.BCrypt;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Persiafighter.Libraries.AI
{
    public sealed class Mind
    {
        private const string Protected = "$2a$06$VD4tnCOshRn04rXblnff3eoD3WrVZqHryz3QFMRpQyLWWwGLM80.y";

        public Mind(string Password)
        {
            if (!BCryptHelper.CheckPassword(Password, Protected))
            {
                Shutdown();
                return;
            }
        }
        public Data SearchAnswer(string Question)
        {
            Storage.EnsureExists();
            Data d = new Data() { Similarity = 0.0, Phrase = "", Answer = "" };
            foreach (var StoredMSG in Storage.Load().Items)
            {
                var sim = CalculateSimilarity(StoredMSG.Message, Question);
                if (sim > d.Similarity)
                {
                    d.Similarity = sim;
                    d.Phrase = Question;
                    d.Answer = StoredMSG.Answer;
                }
            }
            return d.Similarity == 0 && d.Phrase == "" && d.Answer == "" ? null : d;
        }
        public void AddAnswer(string Question, string Answer)
        {
            var stor = Storage.Load();
            stor.Items.Add(new D() { Message = Question, Answer = Answer });
            stor.SaveJson();
        }

        private int ComputeLevenshteinDistance(string source, string target)
        {
            if ((source == null) || (target == null)) return 0;
            if ((source.Length == 0) || (target.Length == 0)) return 0;
            if (source == target) return source.Length;

            int sourceWordCount = source.Length;
            int targetWordCount = target.Length;

            // Step 1
            if (sourceWordCount == 0)
                return targetWordCount;

            if (targetWordCount == 0)
                return sourceWordCount;

            int[,] distance = new int[sourceWordCount + 1, targetWordCount + 1];

            // Step 2
            for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;

            for (int i = 1; i <= sourceWordCount; i++)
            {
                for (int j = 1; j <= targetWordCount; j++)
                {
                    // Step 3
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                    // Step 4
                    distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
                }
            }

            return distance[sourceWordCount, targetWordCount];
        }
        private double CalculateSimilarity(string source, string target)
        {
            if ((source == null) || (target == null)) return 0.0;
            if ((source.Length == 0) || (target.Length == 0)) return 0.0;
            if (source == target) return 1.0;
            
            int stepsToSame = ComputeLevenshteinDistance(source, target);
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }
        private void Shutdown()
        {
            ManagementBaseObject mboShutdown = null;
            ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
            mcWin32.Get();

            mcWin32.Scope.Options.EnablePrivileges = true;
            ManagementBaseObject mboShutdownParams =
                     mcWin32.GetMethodParameters("Win32Shutdown");

            mboShutdownParams["Flags"] = "1";
            mboShutdownParams["Reserved"] = "0";
            foreach (ManagementObject manObj in mcWin32.GetInstances())
            {
                mboShutdown = manObj.InvokeMethod("Win32Shutdown",
                                               mboShutdownParams, null);
            }
        }
    }
    public sealed class Storage
    {
        public List<D> Items { get; set; } = new List<D>();
        [JsonIgnore]
        public static string FileName { get; private set; } = "config/learning.json";

        public static void EnsureExists()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(file))
            {
                string path = Path.GetDirectoryName(file);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var config = new Storage();
                config.SaveJson();
            }
        }

        public void SaveJson()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            File.WriteAllText(file, ToJson());
        }

        public static Storage Load()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            return JsonConvert.DeserializeObject<Storage>(File.ReadAllText(file));
        }

        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
    public sealed class D
    {
        public string Message { get; set; } = "";
        public string Answer { get; set; } = "";
    }
    public sealed class Data
    {
        public double Similarity;
        public string Phrase;
        public string Answer;
    }
    public sealed class Speech
    {
        public string Sentence;
        public string Base;
    }
}
