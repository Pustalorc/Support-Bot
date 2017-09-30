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

                _client.MessageReceived += _client_MessageReceived;
                _client.MessageUpdated += _client_MessageUpdated;
                _client.MessageDeleted += _client_MessageDeleted;

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        private async Task _client_MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            try
            {
                if (arg1.Value.Author.IsBot || arg1.Value == null)
                    return;

                var context = new SocketCommandContext(_client, arg1.Value as SocketUserMessage);

                if (string.Equals(context.Channel.Name, "bot-support", StringComparison.InvariantCultureIgnoreCase))
                    await Delete(arg1.Value.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private async Task _client_MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            try
            {
                if (arg2.Author.IsBot || arg2 == null)
                    return;

                var context = new SocketCommandContext(_client, arg2 as SocketUserMessage);

                if (string.Equals(context.Channel.Name, "bot-support", StringComparison.InvariantCultureIgnoreCase))
                    await Update(arg2.Content, arg2.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private async Task _client_MessageReceived(SocketMessage arg)
        {
            try
            {
                if (arg.Author.IsBot || arg == null)
                    return;

                var context = new SocketCommandContext(_client, arg as SocketUserMessage);

                if (_client.GetGuild(context.Guild.Id).Roles.FirstOrDefault(k => string.Equals(k.Name, "STAFF", StringComparison.InvariantCultureIgnoreCase)).Members.ToList().Exists(k => k.Id == arg.Author.Id))
                {
                    var command = arg.Content.Split(' ').ToList();
                    var comm = command[0];
                    command.Remove(command[0]);
                    var arguments = CheckDoubleQuotes(command);
                    if (comm.StartsWith(Configuration.Load().AdminCommandPrefix))
                    {
                        await arg.DeleteAsync();
                        AdminCommands(comm.TrimStart(Configuration.Load().AdminCommandPrefix.ToCharArray()), arguments, context.Channel.Id, context.Guild.Id);
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
                    UserCommands(comm.TrimStart('!'), arguments, context.Channel.Id, context.Guild.Id);
                    return;
                }

                if (string.Equals(context.Channel.Name, "bot-support", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (Configuration.Load().AnalyzePastebins)
                        if (arg.Content.ToLowerInvariant().Contains("https://pastebin.com/"))
                        {
                            var helper = new PastebinAnalyzerHelper(arg.Content.Split().First(k => k.Contains("https://pastebin.com/")));
                            if (helper.IsLogs())
                                helper.WriteAndExplainErrors(context);
                            return;
                        }

                    await AddToDecide(arg.Content, arg.Id, context.Channel.Id, context.Guild.Id);
                }
                else if (string.Equals(context.Channel.Name, "bot-decisions", StringComparison.InvariantCultureIgnoreCase))
                    BotDecisionCommands(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task Update(string newques, ulong qid)
        {
            try
            {
                var q = _undecidedquestions.Find(k => k.MessageID == qid);
                var answer = _mind.SearchAnswer(newques);
                q.Question = newques;
                if (answer != null)
                {
                    if (answer.Similarity == 1)
                    {
                        _undecidedquestions.Remove(q);
                        await q.AnswerMSG.ModifyAsync(k => k.Content = answer.Answer);
                        await q.DecisionMSG.DeleteAsync();
                    }
                    else if (answer.Similarity > (double)0.5)
                    {
                        await q.DecisionMSG.ModifyAsync(k => k.Content = "A possible answer was found, please specify if it's good with *y <index>, *d <index>, or *a <index> \"<answer>\". Here are the details:\nAnswer: " + answer.Answer + "\nQuestion: " + answer.Phrase + "\nSimilarity: " + (answer.Similarity * 100) + "%\nIndex: " + q.Index);
                        await q.AnswerMSG.ModifyAsync(k => k.Content = answer.Answer);
                    }
                    else
                    {
                        await q.DecisionMSG.ModifyAsync(k => k.Content = "This possible question requires an answer. To discard it use *d <index>, otherwise answer it with *a <index> \"<answer>\"\nQuestion: " + newques + "\nIndex: " + q.Index);
                        await q.AnswerMSG.ModifyAsync(k => k.Content = "I'm sorry, but I currently do not have an answer for you. Staff of this discord are currently trying to get you one, so please hold tight and check this message again in a few minutes.");
                    }
                }
                else
                {
                    await q.DecisionMSG.ModifyAsync(k => k.Content = "This possible question requires an answer. To discard it use *d <index>, otherwise answer it with *a <index> \"<answer>\"\nQuestion: " + newques + "\nIndex: " + q.Index);
                    await q.AnswerMSG.ModifyAsync(k => k.Content = "I'm sorry, but I currently do not have an answer for you. Staff of this discord are currently trying to get you one, so please hold tight and check this message again in a few minutes.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private async Task Delete(ulong questionID)
        {
            try
            {
                var q = _undecidedquestions.Find(k => k.MessageID == questionID);
                _undecidedquestions.Remove(q);
                await q.DecisionMSG.DeleteAsync();
                await q.AnswerMSG.DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private async Task AddToDecide(string question, ulong qid, ulong ChannID, ulong GuildID)
        {
            try
            {
                var channel = _client.GetGuild(GuildID).TextChannels.FirstOrDefault(k => string.Equals(k.Name, "bot-decisions", StringComparison.InvariantCultureIgnoreCase));
                var answer = _mind.SearchAnswer(question);
                if (answer != null)
                {
                    if (answer.Similarity == 1)
                    {
                        await _client.GetGuild(GuildID).GetTextChannel(ChannID).SendMessageAsync(answer.Answer);
                    }
                    else if (answer.Similarity >= (double)0.5)
                    {
                        var i = _undecidedquestions.Count == 0 ? 0 : _undecidedquestions.Last().Index + 1;
                        var decision = await channel.SendMessageAsync("A possible answer was found, please specify if it's good with *y <index>, *d <index>, or *a <index> \"<answer>\". Here are the details:\nAnswer: " + answer.Answer + "\nQuestion: " + answer.Phrase + "\nSimilarity: " + (answer.Similarity * 100) + "%\nIndex: " + i);
                        var tempanswer = await _client.GetGuild(GuildID).GetTextChannel(ChannID).SendMessageAsync(answer.Answer);
                        _undecidedquestions.Add(new Declare(i, question, qid, decision, tempanswer));
                    }
                    else
                    {
                        var i = _undecidedquestions.Count == 0 ? 0 : _undecidedquestions.Last().Index + 1;
                        var decision = await channel.SendMessageAsync("This possible question requires an answer. To discard it use *d <index>, otherwise answer it with *a <index> \"<answer>\"\nQuestion: " + question + "\nIndex: " + i);
                        var tempanswer = await _client.GetGuild(GuildID).GetTextChannel(ChannID).SendMessageAsync("I'm sorry, but I currently do not have an answer for you. Staff of this discord are currently trying to get you one, so please hold tight and check this message again in a few minutes.");
                        _undecidedquestions.Add(new Declare(i, question, qid, decision, tempanswer));
                    }
                }
                else
                {
                    var i = _undecidedquestions.Count == 0 ? 0 : _undecidedquestions.Last().Index + 1;
                    var decision = await channel.SendMessageAsync("This possible question requires an answer. To discard it use *d <index>, otherwise answer it with *a <index> \"<answer>\"\nQuestion: " + question + "\nIndex: " + i);
                    var tempanswer = await _client.GetGuild(GuildID).GetTextChannel(ChannID).SendMessageAsync("I'm sorry, but I currently do not have an answer for you. Staff of this discord are currently trying to get you one, so please hold tight and check this message again in a few minutes.");
                    _undecidedquestions.Add(new Declare(i, question, qid, decision, tempanswer));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private async Task Discard(string index, ulong GuildID)
        {
            try
            {
                if (int.TryParse(index, out int i) && _undecidedquestions.Exists(k => k.Index == i))
                {
                    var q = _undecidedquestions.Find(k => k.Index == i);
                    _undecidedquestions.Remove(q);
                    await q.DecisionMSG.DeleteAsync();
                    await q.AnswerMSG.ModifyAsync(k => k.Content = "Your question has been discarded by staff. This could be due to it not being an actual question or them not wanting to answer it.");
                }
                else if (string.Equals(index, "all", StringComparison.InvariantCultureIgnoreCase))
                {
                    List<RestUserMessage> msg = new List<RestUserMessage>();
                    foreach (var g in _undecidedquestions.ToList())
                    {
                        _undecidedquestions.Remove(g);
                        msg.Add(g.DecisionMSG);
                        await g.AnswerMSG.ModifyAsync(k => k.Content = "Your question has been discarded by staff. This could be due to it not being an actual question or them not wanting to answer it.");
                    }
                    await _client.GetGuild(GuildID).TextChannels.FirstOrDefault(k => string.Equals(k.Name, "bot-decisions", StringComparison.InvariantCultureIgnoreCase)).DeleteMessagesAsync(msg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private async Task Answer(int index, string answer = "")
        {
            try
            {
                if (_undecidedquestions.Exists(k => k.Index == index))
                {
                    var q = _undecidedquestions.Find(k => k.Index == index);
                    if (string.IsNullOrEmpty(answer))
                    {
                        _undecidedquestions.Remove(q);
                        await q.DecisionMSG.DeleteAsync();
                        await q.AnswerMSG.ModifyAsync(k => k.Content = _mind.SearchAnswer(q.Question).Answer);
                    }
                    else
                    {
                        _mind.AddAnswer(q.Question, answer);
                        _undecidedquestions.Remove(q);
                        await q.DecisionMSG.DeleteAsync();
                        await q.AnswerMSG.ModifyAsync(k => k.Content = answer);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private async void BotDecisionCommands(SocketCommandContext msg)
        {
            try
            {
                var command = msg.Message.Content.Split(' ').ToList();
                var comm = command[0];
                command.Remove(command[0]);
                var arguments = CheckDoubleQuotes(command);
                if (comm.StartsWith("*"))
                {
                    switch (comm.ToLower().TrimStart('*'))
                    {
                        case "d":
                            await Discard(arguments[0], msg.Guild.Id);
                            await msg.Message.DeleteAsync();
                            break;
                        case "a":
                            await Answer(int.Parse(arguments[0]), arguments[1]);
                            await msg.Message.DeleteAsync();
                            break;
                        case "y":
                            await Answer(int.Parse(arguments[0]));
                            await msg.Message.DeleteAsync();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async void AdminCommands(string command, List<string> args, ulong ChannelID, ulong GuildID)
        {
            try
            {
                switch (command.ToLowerInvariant())
                {
                    case "status":
                        var d = DateTime.Now;
                        await _client.GetConnectionsAsync();
                        await _client.GetGuild(GuildID).GetTextChannel(ChannelID).SendMessageAsync("----------- Support bot V2.0 status report -----------\nPing: " + (ulong)DateTime.Now.Subtract(d).TotalMilliseconds + "ms.\nRunning on " + Environment.OSVersion + ".\nHave been running for: " + DateTime.Now.Subtract(_start) + ".\n----------- Support bot V2.0 status report -----------");
                        break;
                    case "shutdown":
                        Environment.Exit(0);
                        break;
                    case "google":
                        await _client.GetGuild(GuildID).GetTextChannel(ChannelID).SendMessageAsync(WebRequest.CreateHttp(string.Format("https://www.google.es/search?q={0}", WebUtility.UrlEncode(args[0]))).GetResponse().ResponseUri.ToString());
                        break;
                    case "purge":
                        int deleted = 0;
                        if (args.Count >= 2)
                        {
                            bool i = ulong.TryParse(args[0], out ulong id);
                            if (i)
                            {
                                var u = _client.GetGuild(GuildID).Users.ToList().Find(k => k?.Id == id);
                                if (u == null)
                                {
                                    var m = await _client.GetGuild(GuildID).GetTextChannel(ChannelID).GetMessagesAsync((int)id).Flatten();
                                    await _client.GetGuild(GuildID).GetTextChannel(ChannelID).DeleteMessagesAsync(m);
                                    deleted = m.Count();
                                }
                                else
                                {
                                    var m = await _client.GetGuild(GuildID).GetTextChannel(ChannelID).GetMessagesAsync(int.Parse(args[1])).Flatten();
                                    var f = m.Where(k => k.Author.Id == u.Id);
                                    await _client.GetGuild(GuildID).GetTextChannel(ChannelID).DeleteMessagesAsync(f);
                                    deleted = f.Count();
                                }
                            }
                            else
                            {
                                var u = _client.GetGuild(GuildID).Users.ToList().Find(k =>
                                {
                                    bool? username = k?.Username?.ToLowerInvariant().Contains(args[0].ToLowerInvariant());
                                    bool? nickname = k?.Nickname?.ToLowerInvariant().Contains(args[0].ToLowerInvariant());
                                    if ((username.HasValue && username.Value) || (nickname.HasValue && nickname.Value))
                                        return true;
                                    else return false;
                                });
                                var m = await _client.GetGuild(GuildID).GetTextChannel(ChannelID).GetMessagesAsync(int.Parse(args[1])).Flatten();
                                var f = m.Where(k => k.Author.Id == u.Id);
                                await _client.GetGuild(GuildID).GetTextChannel(ChannelID).DeleteMessagesAsync(f);
                                deleted = f.Count();
                            }
                        }
                        else if (args.Count == 1)
                        {
                            bool i = ulong.TryParse(args[0], out ulong id);
                            if (i)
                            {
                                var u = _client.GetGuild(GuildID).Users.ToList().Find(k => k?.Id == id);
                                if (u == null)
                                {
                                    var m = await _client.GetGuild(GuildID).GetTextChannel(ChannelID).GetMessagesAsync((int)id).Flatten();
                                    await _client.GetGuild(GuildID).GetTextChannel(ChannelID).DeleteMessagesAsync(m);
                                    deleted = m.Count();
                                }
                                else
                                {
                                    var m = await _client.GetGuild(GuildID).GetTextChannel(ChannelID).GetMessagesAsync(100).Flatten();
                                    var f = m.Where(k => k.Author.Id == u.Id);
                                    await _client.GetGuild(GuildID).GetTextChannel(ChannelID).DeleteMessagesAsync(f);
                                    deleted = f.Count();
                                }
                            }
                            else
                            {
                                var u = _client.GetGuild(GuildID).Users.ToList().Find(k =>
                                {
                                    bool? username = k?.Username?.ToLowerInvariant().Contains(args[0].ToLowerInvariant());
                                    bool? nickname = k?.Nickname?.ToLowerInvariant().Contains(args[0].ToLowerInvariant());
                                    if ((username.HasValue && username.Value) || (nickname.HasValue && nickname.Value))
                                        return true;
                                    else return false;
                                });
                                var m = await _client.GetGuild(GuildID).GetTextChannel(ChannelID).GetMessagesAsync(100).Flatten();
                                var f = m.Where(k => k.Author.Id == u.Id);
                                await _client.GetGuild(GuildID).GetTextChannel(ChannelID).DeleteMessagesAsync(f);
                                deleted = f.Count();
                            }
                        }
                        else
                        {
                            var msgs = await _client.GetGuild(GuildID).GetTextChannel(ChannelID).GetMessagesAsync(100).Flatten();
                            await _client.GetGuild(GuildID).GetTextChannel(ChannelID).DeleteMessagesAsync(msgs);
                            deleted = msgs.Count();
                        }
                        var botms = await _client.GetGuild(GuildID).GetTextChannel(ChannelID).SendMessageAsync("Deleted " + deleted + " messages.");
                        await Task.Delay(5000);
                        await botms.DeleteAsync();
                        break;
                    case "udetails":
                        var user = _client.GetGuild(GuildID).Users.ToList().Find(k =>
                        {
                            bool? username = k?.Username?.ToLowerInvariant().Contains(args[0].ToLowerInvariant());
                            bool? nickname = k?.Nickname?.ToLowerInvariant().Contains(args[0].ToLowerInvariant());
                            if ((username.HasValue && username.Value) || (nickname.HasValue && nickname.Value))
                                return true;
                            else return false;
                        });
                        if (user == null && ulong.TryParse(args[0], out ulong ID))
                            user = _client.GetGuild(GuildID).GetUser(ID);
                        if (user != null)
                            await _client.GetGuild(GuildID).GetTextChannel(ChannelID).SendMessageAsync(user.Mention + "'s details:\nUsername - " + user.Username + "\nNickname - " + ((user.Nickname == string.Empty || user.Nickname == null) ? "N/A" : user.Nickname) + "\nID - " + user.Id + "\nStatus - " + user.Status + "\nCustom Status/Playing - " + (user.Game.HasValue ? user.Game.Value.Name : "N/A") + "\nCreated - " + user.CreatedAt + "\nJoined - " + user.JoinedAt);
                        else
                            await _client.GetGuild(GuildID).GetTextChannel(ChannelID).SendMessageAsync("User not found, did you even put a user? :thinking:");
                        break;
                    case "game":
                        await _client.SetGameAsync(args[0]);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private async void UserCommands(string command, List<string> args, ulong ChannelID, ulong GuildID)
        {
            switch (command.ToLowerInvariant())
            {
                case "flip":
                    if (_rnd.Next(0, 101) > 50)
                        await _client.GetGuild(GuildID).GetTextChannel(ChannelID).SendMessageAsync("You rolled heads.");
                    else
                        await _client.GetGuild(GuildID).GetTextChannel(ChannelID).SendMessageAsync("You rolled tails.");
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
    }
    public sealed class Declare
    {
        public int Index;
        public string Question;
        public ulong MessageID;
        public RestUserMessage AnswerMSG;
        public RestUserMessage DecisionMSG;

        public Declare(int i, string q, ulong id, RestUserMessage decide, RestUserMessage answer)
        {
            Index = i;
            Question = q;
            MessageID = id;
            DecisionMSG = decide;
            AnswerMSG = answer;
        }
    }
}
