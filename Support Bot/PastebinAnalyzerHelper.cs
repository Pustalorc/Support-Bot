using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Persiafighter.Applications.Support_Bot
{
    public class PastebinAnalyzerHelper
    {
        private List<string> Data;

        public PastebinAnalyzerHelper(string PastebinURI)
        {
            using (var wc = new WebClient())
                Data = wc.DownloadString(WebRequest.CreateHttp(PastebinURI.StartsWith("https://pastebin.com/raw/") ? PastebinURI : "https://pastebin.com/raw/" + PastebinURI.TrimStart('h', 't', 'p', 's', ':').TrimStart('/').TrimStart('p', 'a', 's', 't', 'e', 'b', 'i', 'n', '.', 'c', 'o', 'm').TrimStart('/')).GetResponse().ResponseUri.ToString()).Split().ToList();
        }

        public bool IsLogs()
        {
            var r = new Regex("^\\d(\\d|(?<!/)/)*\\d$|^\\d$");
            return r.IsMatch(Data[0].TrimStart('[').TrimEnd(']'));
        }

        public async void WriteAndExplainErrors(SocketCommandContext context)
        {
            var r = new Regex("^\\d(\\d|(?<!/)/)*\\d$|^\\d$");
            List<string> errors = new List<string>();
            string CurrentError = "";
            foreach (string s in Data)
            {
                if (CurrentError != string.Empty && r.IsMatch(s.TrimStart('[').TrimEnd(']')) && s.StartsWith("["))
                {
                    errors.Add(CurrentError);
                    CurrentError = "";
                }

                if (s.ToLower() == "[error]")
                    CurrentError += s + " ";
                else if (CurrentError != string.Empty)
                    CurrentError += s + " ";
            }
            string FinalMSG = "Errors:\n";
            foreach (string s in errors.Distinct().ToList())
            {
                List<string> ss = s.Split().ToList();
                if (s.Contains("Could not find dependency:"))
                    FinalMSG += "A library with name " + ss[5] + " is not found in your libraries folder for your server." + "\n";
                else if (s.Contains("Invalid or outdated plugin assembly:"))
                    FinalMSG += "A library with name " + ss[6] + " is in your plugins directory. Rocket complains about it as it doesn't recognize it as a plugin." + "\n";
                else if (s.Contains("Failed to generate an item with ID"))
                    FinalMSG += "The spawn tables in your server are attempting to spawn an item with ID " + ss[8] + ", but said item does not exist in either unturned or a workshop mod." + "\n";
                else if (s.Contains("Error in MulticastDelegate PlayerDeath:"))
                    FinalMSG += "The plugin " + ss[32].Split('.')[0] + " had an issue in its code for when a player died. If this issue is common, please disable or remove the plugin as it is most likely broken" + "\n";
            }
            await context.Channel.SendMessageAsync(FinalMSG);
        }
    }
}
