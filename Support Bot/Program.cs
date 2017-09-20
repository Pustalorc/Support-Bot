using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Persiafighter.Libraries.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Persiafighter.Applications.Support_Bot
{
    public sealed class Entry
    {
        private static void Main(string[] args) =>
            new Entry().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private Mind _mind = new Mind("DeezNutz");
        private List<Declare> _undecidedquestions = new List<Declare>();
        private DateTime _start = DateTime.Now;
        private const ulong _gid = 238360723179175947;
        private Random _rnd = new Random();

        public async Task StartAsync()
        {
            try
            {
                Configuration.EnsureExists();
                _client = new DiscordSocketClient(new DiscordSocketConfig() { LogLevel = LogSeverity.Verbose, MessageCacheSize = 1000 });
                _client.Log += (l) => Console.Out.WriteLineAsync(l.ToString());

                await _client.LoginAsync(TokenType.Bot, Configuration.Load().Token);
                await _client.StartAsync();
                await _client.SetGameAsync("Helping People ^_^ #Bot-Support");

                _client.Ready += _client_Ready;

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }
        private Task _client_Ready()
        {
            _client.MessageReceived += _client_MessageReceived;
            return null;
        }
        private async Task _client_MessageReceived(SocketMessage arg)
        {
            if (arg.Author.IsBot || arg == null)
                return;

            var context = new SocketCommandContext(_client, arg as SocketUserMessage);

            if (IsAdmin(arg.Author.Id))
            {
                var command = arg.Content.Split(' ').ToList();
                var comm = command[0];
                command.Remove(command[0]);
                var arguments = CheckDoubleQuotes(command);
                if (comm.StartsWith(Configuration.Load().Prefix))
                {
                    await arg.DeleteAsync();
                    AdminCommands(comm.TrimStart(Configuration.Load().Prefix.ToCharArray()), arguments, context.Channel.Id);
                    return;
                }
            }
            
            if (arg.Content.Split(' ').ToList()[0].StartsWith("!"))
            {
                var command = arg.Content.Split(' ').ToList();
                var comm = command[0];
                command.Remove(command[0]);
                var arguments = CheckDoubleQuotes(command);
                await arg.DeleteAsync();
                UserCommands(comm.TrimStart('!'), arguments, context.Channel.Id);
                return;
            }

            if (context.Channel.Id == 238360723179175947)
            {
                if (Configuration.Load().AnalyzePastebins)
                    if (arg.Content.ToLowerInvariant().Contains("https://pastebin.com/"))
                    {
                        var helper = new PastebinAnalyzerHelper(arg.Content.Split().First(k => k.Contains("https://pastebin.com/")));
                        if (helper.IsLogs())
                            helper.WriteAndExplainErrors(context);
                        return;
                    }

                await AddToDecide(arg.Content, context.Channel.Id);
            }
            else if (context.Channel.Id == 359748732859711489)
                BotDecisionCommands(arg);
        }

        private async Task AddToDecide(string question, ulong ChannID)
        {
            var channel = _client.GetGuild(238360723179175947).GetTextChannel(359748732859711489);
            var answer = _mind.SearchAnswer(question);
            if (answer != null)
            {
                if (answer.Similarity == 1)
                {
                    await _client.GetGuild(238360723179175947).GetTextChannel(ChannID).SendMessageAsync(answer.Answer);
                }
                else
                {
                    var i = _undecidedquestions.Count == 0 ? 0 : _undecidedquestions.Last().Index + 1;
                    _undecidedquestions.Add(new Declare() { Index = i, Question = question, Message = await channel.SendMessageAsync("A possible answer was found, please specify if it's good with *yes <index>, *discard <index>, or *answer <index> \"<answer>\". Here are the details:\nAnswer: " + answer.Answer + "\nQuestion: " + answer.Phrase + "\nSimilarity: " + (answer.Similarity * 100) + "%\nIndex: " + i) });
                }
            }
            else
            {
                var i = _undecidedquestions.Count == 0 ? 0 : _undecidedquestions.Last().Index + 1;
                _undecidedquestions.Add(new Declare() { Index = i, Question = question, Message = await channel.SendMessageAsync("This possible question requires an answer. To discard it use *discard <index>, otherwise answer it with *answer <index> \"<answer>\"\nQuestion: " + question + "\nIndex: " + i) });
            }
        }
        private async Task Discard(string index)
        {
            if (int.TryParse(index, out int i) && _undecidedquestions.Exists(k => k.Index == i))
            {
                var q = _undecidedquestions.Find(k => k.Index == i);
                _undecidedquestions.Remove(q);
                await q.Message.DeleteAsync();
            }
            else if (string.Equals(index, "all", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var g in _undecidedquestions.ToList())
                {
                    _undecidedquestions.Remove(g);
                    await g.Message.DeleteAsync();
                }
            }
        }
        private async Task Answer(int index, string answer = "")
        {
            if (_undecidedquestions.Exists(k => k.Index == index))
            {
                var channel = _client.GetGuild(238360723179175947).GetTextChannel(359748732859711489);
                var q = _undecidedquestions.Find(k => k.Index == index);
                if (string.IsNullOrEmpty(answer))
                {
                    _undecidedquestions.Remove(q);
                    await q.Message.DeleteAsync();
                    await _client.GetGuild(238360723179175947).GetTextChannel(238360723179175947).SendMessageAsync(_mind.SearchAnswer(q.Question).Answer);
                }
                else
                {
                    _mind.AddAnswer(q.Question, answer);
                    _undecidedquestions.Remove(q);
                    await q.Message.DeleteAsync();
                    await _client.GetGuild(238360723179175947).GetTextChannel(238360723179175947).SendMessageAsync(answer);
                }
            }
        }
        private async void BotDecisionCommands(SocketMessage msg)
        {
            var command = msg.Content.Split(' ').ToList();
            var comm = command[0];
            command.Remove(command[0]);
            var arguments = CheckDoubleQuotes(command);
            if (comm.StartsWith("*"))
            {
                switch (comm.ToLower().TrimStart('*'))
                {
                    case "d":
                        await Discard(arguments[0]);
                        await msg.DeleteAsync();
                        break;
                    case "a":
                        await Answer(int.Parse(arguments[0]), arguments[1]);
                        await msg.DeleteAsync();
                        break;
                    case "y":
                        await Answer(int.Parse(arguments[0]));
                        await msg.DeleteAsync();
                        break;
                }
            }
        }

        private async void AdminCommands(string command, List<string> args, ulong ChannelID)
        {
            try
            {
                switch (command.ToLowerInvariant())
                {
                    case "status":
                        var d = DateTime.Now;
                        await _client.GetConnectionsAsync();
                        await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).SendMessageAsync("----------- Support bot V2.0 status report -----------\nPing: " + (ulong)DateTime.Now.Subtract(d).TotalMilliseconds + "ms.\nRunning on " + Environment.OSVersion + ".\nHave been running for: " + DateTime.Now.Subtract(_start) + ".\n----------- Support bot V2.0 status report -----------");
                        break;
                    case "shutdown":
                        Environment.Exit(0);
                        break;
                    case "google":
                        await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).SendMessageAsync(WebRequest.CreateHttp(string.Format("https://www.google.es/search?q={0}", WebUtility.UrlEncode(args[0]))).GetResponse().ResponseUri.ToString());
                        break;
                    case "purge":
                        int deleted = 0;
                        if (args.Count >= 2)
                        {
                            bool i = ulong.TryParse(args[0], out ulong id);
                            if (i)
                            {
                                var u = _client.GetGuild(238360723179175947).Users.ToList().Find(k => k.Id == id);
                                if (u == null)
                                {
                                    var m = await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).GetMessagesAsync((int)id).Flatten();
                                    await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).DeleteMessagesAsync(m);
                                    deleted = m.Count();
                                }
                                else
                                {
                                    var m = await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).GetMessagesAsync(int.Parse(args[1])).Flatten();
                                    var f = m.Where(k => k.Author.Id == u.Id);
                                    await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).DeleteMessagesAsync(f);
                                    deleted = f.Count();
                                }
                            }
                            else
                            {
                                var u = _client.GetGuild(238360723179175947).Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(args[0].ToLowerInvariant()) || (k.Nickname == null && k.Nickname.ToLowerInvariant().Contains(args[0].ToLowerInvariant())));
                                var m = await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).GetMessagesAsync(int.Parse(args[1])).Flatten();
                                var f = m.Where(k => k.Author.Id == u.Id);
                                await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).DeleteMessagesAsync(f);
                                deleted = f.Count();
                            }
                        }
                        else if (args.Count == 1)
                        {
                            bool i = ulong.TryParse(args[0], out ulong id);
                            if (i)
                            {
                                var u = _client.GetGuild(238360723179175947).Users.ToList().Find(k => k.Id == id);
                                if (u == null)
                                {
                                    var m = await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).GetMessagesAsync((int)id).Flatten();
                                    await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).DeleteMessagesAsync(m);
                                    deleted = m.Count();
                                }
                                else
                                {
                                    var m = await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).GetMessagesAsync(100).Flatten();
                                    var f = m.Where(k => k.Author.Id == u.Id);
                                    await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).DeleteMessagesAsync(f);
                                    deleted = f.Count();
                                }
                            }
                            else
                            {
                                var u = _client.GetGuild(238360723179175947).Users.ToList().Find(k =>
                                {
                                    bool? username = k?.Username?.ToLowerInvariant().Contains(args[0].ToLowerInvariant());
                                    bool? nickname = k?.Nickname?.ToLowerInvariant().Contains(args[0].ToLowerInvariant());
                                    if ((username.HasValue && username.Value) || (nickname.HasValue && nickname.Value))
                                        return true;
                                    else return false;
                                });
                                var m = await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).GetMessagesAsync(100).Flatten();
                                var f = m.Where(k => k.Author.Id == u.Id);
                                await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).DeleteMessagesAsync(f);
                                deleted = f.Count();
                            }
                        }
                        else
                        {
                            var msgs = await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).GetMessagesAsync(100).Flatten();
                            await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).DeleteMessagesAsync(msgs);
                            deleted = msgs.Count();
                        }
                        var botms = await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).SendMessageAsync("Deleted " + deleted + " messages.");
                        await Task.Delay(5000);
                        await botms.DeleteAsync();
                        break;
                    case "udetails":
                        var user = _client.GetGuild(238360723179175947).Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(args[0].ToLowerInvariant()));
                        if (user == null && ulong.TryParse(args[0], out ulong ID))
                            user = _client.GetGuild(238360723179175947).GetUser(ID);
                        if (user != null)
                            await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).SendMessageAsync(user.Mention + "'s details:\nUsername - " + user.Username + "\nNickname - " + ((user.Nickname == string.Empty || user.Nickname == null) ? "N/A" : user.Nickname) + "\nID - " + user.Id + "\nStatus - " + user.Status + "\nCustom Status/Playing - " + (user.Game.HasValue ? user.Game.Value.Name : "N/A") + "\nCreated - " + user.CreatedAt + "\nJoined - " + user.JoinedAt);
                        else
                            await _client.GetGuild(238360723179175947).GetTextChannel(ChannelID).SendMessageAsync("User not found, did you even put a user? :thinking:");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private async void UserCommands(string command, List<string> args, ulong ChannelID)
        {
            switch (command.ToLowerInvariant())
            {
                case "flip":
                    if (_rnd.Next(0, 101) > 50)
                        await _client.GetGuild(_gid).GetTextChannel(ChannelID).SendMessageAsync("You rolled heads.");
                    else
                        await _client.GetGuild(_gid).GetTextChannel(ChannelID).SendMessageAsync("You rolled tails.");
                    break;
            }
        }

        private List<string> CheckDoubleQuotes(List<string> Items)
        {
            List<string> result = new List<string>();
            try
            {
                string Combined = "";
                foreach (string s in Items)
                {
                    if (Combined != "")
                        Combined += " " + s;

                    if (s.StartsWith("\""))
                        Combined += s.TrimStart('"');

                    if (s.EndsWith("\""))
                    {
                        string t = Combined.TrimEnd('"');
                        result.Add(t);
                        Combined = "";
                        continue;
                    }

                    if (Combined == "")
                        result.Add(s);
                }
            }
            catch { }
            return result;
        }
        private bool IsAdmin(ulong ID)
        {
            if (ID == 181418012400812032 || ID == 236451517257875456)
                return true;
            else
                return Configuration.Load().Admins.Contains(ID);
        }
    }
    public sealed class Declare
    {
        public int Index;
        public string Question;
        public RestUserMessage Message;
    }
}
