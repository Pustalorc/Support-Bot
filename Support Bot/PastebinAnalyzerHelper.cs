using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Persiafighter.Applications.Support_Bot
{
    public class PastebinAnalyzerHelper
    {
        private List<string> Data;

        public PastebinAnalyzerHelper(string PastebinURI)
        {
            try
            {
                using (var wc = new WebClient())
                    Data = wc.DownloadString(WebRequest.CreateHttp(PastebinURI.StartsWith("https://pastebin.com/raw/") ? PastebinURI : "https://pastebin.com/raw/" + PastebinURI.TrimStart('h', 't', 'p', 's', ':').TrimStart('/').TrimStart('p', 'a', 's', 't', 'e', 'b', 'i', 'n', '.', 'c', 'o', 'm').TrimStart('/')).GetResponse().ResponseUri.ToString()).Split().ToList();
            }
            catch { }
        }

        public bool IsLogs()
        {
            try
            { 
                var r = new Regex("^\\d(\\d|(?<!/)/)*\\d$|^\\d$");
                return r.IsMatch(Data[0].TrimStart('[').TrimEnd(']'));
            }
            catch { return false; }
        }

        public async void WriteAndExplainErrors(SocketCommandContext context)
        {
            try
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

                    if (s.ToLower() == "[error]" || s.ToLower() == "[exception]")
                        CurrentError += s + " ";
                    else if (CurrentError != string.Empty)
                        CurrentError += s + " ";
                }
                List<string> MSGS = new List<string>() { "Errors:\n" };
                foreach (string s in errors.Distinct().ToList())
                {
                    List<string> ss = s.Split().ToList();
                    
                    bool Found = false;
                    foreach (var sss in Configuration.Load().Pastebins)
                        if (s.ToLower().Contains(sss.Identifier.ToLower()))
                        {
                            MSGS.Add(string.Format(sss.Answer, sss.NameIndex > -1 ? ss[sss.NameIndex] : "") + "\n");
                            Found = true;
                            break;
                        }

                    if (!Found)
                    {
                        for (int i = 0; i < ss.Count; i++)
                            Console.WriteLine("[" + i + "]: " + ss[i]);
                        MSGS.Add("Unknown error: " + (s.Length > 200 ? s.Remove(200, s.Length - 200) : s) + "\n");
                    }
                    else
                        return;
                }
                List<string> lines = new List<string>();
                string currentLine = "";
                int maxLength = 2000;
                foreach (var currentWord in MSGS.Distinct())
                {
                    if ((currentLine.Length > maxLength) || ((currentLine.Length + currentWord.Length) > maxLength))
                    {
                        lines.Add(currentLine);
                        currentLine = "";
                    }

                    if (currentLine.Length > 0)
                    {
                        if (currentLine.EndsWith("\n"))
                            currentLine += currentWord;
                        else
                            currentLine += " " + currentWord;
                    }
                    else
                        currentLine += currentWord;

                    if (currentWord == MSGS.Distinct().Last())
                        lines.Add(currentLine);
                }
                foreach (string line in lines)
                {
                    await context.Channel.SendMessageAsync(line);
                }
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message);
            }
        }
    }
}
