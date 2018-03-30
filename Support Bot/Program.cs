using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Pustalorc.Applications.Support_Bot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pustalorc.Applications.Support_Bot
{
    public sealed class Entry
    {
        private DiscordSocketClient ClientInstance;
        private MSG ToCheck = null;

        private static void Main(string[] args) =>
            new Entry().StartAsync(args).GetAwaiter().GetResult();

        public async Task StartAsync(string[] args)
        {
            try
            {
                Configuration.EnsureExists();
                Learning.EnsureExists();

                ClientInstance = new DiscordSocketClient(new DiscordSocketConfig()
                {
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 1000,
                    DefaultRetryMode = RetryMode.AlwaysRetry
                });

                ClientInstance.Log += (l) => Console.Out.WriteLineAsync(l.ToString());

                await ClientInstance.LoginAsync(TokenType.Bot, Configuration.Load().Token);
                await ClientInstance.StartAsync();
                await ClientInstance.SetGameAsync("Helping people! ^3^");

                ClientInstance.MessageReceived += async (o) => await HandleMessage(new SocketCommandContext(ClientInstance, o as SocketUserMessage), false);
                ClientInstance.MessageUpdated += async (a, o, u) => await HandleMessage(new SocketCommandContext(ClientInstance, o as SocketUserMessage), true);

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        private async Task HandleMessage(SocketCommandContext Context, bool IsUpdate)
        {
            if (Context == null || Context.Message.Author.IsBot || Context.IsPrivate)
                return;

            if (IsCommand(Context.Message.Content))
            {
                HandleCommand(Context);
                return;
            }

            var config = Configuration.Load();

            if (Context.Channel.Id == config.GeneralChannel && !HasRole(config.StaffRole, Context.Message.Author.Id, Context.Guild.Id) && !HasRole(config.SupporterRole, Context.Message.Author.Id, Context.Guild.Id) &&
                (Context.Message.MentionedRoles.ToList().Exists(k => k.Id == config.SupporterRole) || SearchAnswer(Context.Message.Content).Similarity > (double)0.8))
            {
                await DeleteMessage(Context.Message);

                SendMessage(Context.Message.Author.Mention + " please ask a full question with a tag to the supporter role (ID: " + config.SupporterRole +
                  ") in <#" + config.SupportChannel + ">. You will not get help in channels like this one, since they are not support related.", Context.Guild.Id, Context.Channel.Id, 10);

                var a = SearchAnswer(Context.Message.Content);
                if (a.Similarity > (double)0.8)
                    SendMessage(Context.Message.Author.Mention + (a.Similarity == (double)1.0 ? ": " : ", **this may be incorrect**: ") + a.Answer, Context.Guild.Id,
                      config.SupportChannel);
                else
                {
                    var msg = Context.Message.Content.Split().ToList();

                    foreach (var s in msg.ToList())
                        if (s.StartsWith("<@"))
                            msg.Remove(s);

                    if (msg.Count == 0)
                        return;
                    await SendMessage("<@!" + config.SupporterRole + "> " + Context.Message.Author.Mention + " needs your help! Their message is: " +
                      string.Join(" ", msg), Context.Guild.Id, config.SupportChannel);
                }
            }
            else if (Context.Channel.Id == config.SupportChannel)
            {
                if (!HasRole(config.StaffRole, Context.Message.Author.Id, Context.Guild.Id))
                {
                    if (Context.Message.MentionedUsers.Count > 0)
                    {
                        await DeleteMessage(Context.Message);

                        var msg = Context.Message.Content.Split().ToList();

                        foreach (var s in msg.ToList())
                            if (s.StartsWith("<@"))
                                msg.Remove(s);

                        if (msg.Count == 0)
                            return;

                        SendMessage(Context.Message.Author + " please do not tag anyone in support channels. They will respond when they can. Thank you.",
                          Context.Guild.Id, Context.Channel.Id, 10);

                        SendMessage(Context.Message.Author + " Says: " + string.Join(" ", msg), Context.Guild.Id, Context.Channel.Id);
                        return;
                    }

                    if (!HasRole(config.SupporterRole, Context.Message.Author.Id, Context.Guild.Id))
                    {
                        var a = SearchAnswer(Context.Message.Content);

                        if (a.Similarity > (double)0.8)
                            SendMessage(Context.Message.Author.Mention + ": " + a.Answer, Context.Guild.Id, Context.Channel.Id);
                    }
                }

                var data = Learning.Load();
                if (!data.NotQuestions.Exists(k => string.Equals(k, Context.Message.Content, StringComparison.InvariantCultureIgnoreCase)))
                {
                    data.UndecidedQuestions.Add(new Deciding() { ID = Context.Message.Id, Question = Context.Message.Content });
                    data.SaveJson();
                }
            }
        }
        private async Task<bool> HandleCommand(SocketCommandContext Context)
        {
            var executed = Context.Message.Content.Split(' ').ToList();
            var comm = executed[0];
            executed.Remove(comm);
            var arguments = CheckDoubleQuotes(executed);
            var config = Configuration.Load();
            var learning = Learning.Load();

            if (Context.Message.Author.Id == config.OwnerID && comm.StartsWith("$"))
            {
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "shutdown":
                        await DeleteMessage(Context.Message);
                        await ClientInstance.LogoutAsync();
                        Environment.Exit(0);
                        return true;
                    case "game":
                        try
                        {
                            await ClientInstance.SetGameAsync(string.Join(" ", arguments));
                        }
                        catch
                        {
                            try
                            {
                                await ClientInstance.SetGameAsync(string.Join(" ", arguments));
                            }
                            catch { }
                        }
                        await DeleteMessage(Context.Message);
                        return true;
                }
            }
            if (HasRole(config.StaffRole, Context.Message.Author.Id, Context.Guild.Id) && comm.StartsWith("/"))
            {
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "status":
                        var d = DateTime.Now;
                        await ClientInstance.GetConnectionsAsync();
                        await SendMessage("----------- Support bot V4.0 status report -----------\nPing: " + (ulong)DateTime.Now.Subtract(d).TotalMilliseconds +
                          "ms.\nRunning on " + Environment.OSVersion + ".\n----------- Support bot V4.0 status report -----------", Context.Guild.Id,
                          Context.Channel.Id);
                        await DeleteMessage(Context.Message);
                        return true;
                    case "purge":
                        await DeleteMessage(Context.Message);
                        await Task.Delay(100);
                        int deleted = 0;
                        if (arguments.Count >= 2)
                        {
                            var u = GetUser(arguments[0], Context.Guild.Id);
                            var u2 = GetUser(arguments[1], Context.Guild.Id);
                            if (ulong.TryParse(arguments[0], out ulong amount))
                            {
                                if (u != null)
                                {
                                    if (int.TryParse(arguments[1], out int ammount))
                                    {
                                        var msgs = await Context.Channel.GetMessagesAsync(ammount).Flatten();
                                        var f = msgs.Where(k => k.Author.Id == u.Id);
                                        await Context.Channel.DeleteMessagesAsync(f);
                                        deleted = f.Count();
                                    }
                                    else
                                        await SendMessage(ammount + " is not a number.", Context.Guild.Id, Context.Channel.Id);
                                }
                                else
                                {
                                    if (int.TryParse(arguments[0], out int ammount))
                                    {
                                        if (u2 != null)
                                        {
                                            var msgs = await Context.Channel.GetMessagesAsync(ammount).Flatten();
                                            var f = msgs.Where(k => k.Author.Id == u2.Id);
                                            await Context.Channel.DeleteMessagesAsync(f);
                                            deleted = f.Count();
                                        }
                                        else
                                            await SendMessage("User " + arguments[0] + " not found.", Context.Guild.Id, Context.Channel.Id);
                                    }
                                    else
                                        await SendMessage(ammount + " is not a number.", Context.Guild.Id, Context.Channel.Id);
                                }
                            }
                            else
                            {
                                if (u != null)
                                {
                                    if (int.TryParse(arguments[1], out int ammount))
                                    {
                                        var msgs = await Context.Channel.GetMessagesAsync(ammount).Flatten();
                                        var f = msgs.Where(k => k.Author.Id == u.Id);
                                        await Context.Channel.DeleteMessagesAsync(f);
                                        deleted = f.Count();
                                    }
                                    else
                                        await SendMessage(ammount + " is not a number.", Context.Guild.Id, Context.Channel.Id);
                                }
                                else
                                    await SendMessage("User " + arguments[0] + " not found.", Context.Guild.Id, Context.Channel.Id);
                            }
                        }
                        else if (arguments.Count == 1)
                        {
                            var u = GetUser(arguments[0], Context.Guild.Id);
                            if (ulong.TryParse(arguments[0], out ulong amount))
                            {
                                if (u != null)
                                {
                                    var msgs = await Context.Channel.GetMessagesAsync(100).Flatten();
                                    var f = msgs.Where(k => k.Author.Id == u.Id);
                                    await Context.Channel.DeleteMessagesAsync(f);
                                    deleted = f.Count();
                                }
                                else if (int.TryParse(arguments[0], out int ammount))
                                {
                                    var msgs = await Context.Channel.GetMessagesAsync(ammount).Flatten();
                                    await Context.Channel.DeleteMessagesAsync(msgs);
                                    deleted = msgs.Count();
                                }
                                else
                                    await SendMessage("User " + arguments[0] + " not found or it is not a number.", Context.Guild.Id, Context.Channel.Id);

                            }
                            else
                            {
                                if (u != null)
                                {
                                    var msgs = await Context.Channel.GetMessagesAsync(100).Flatten();
                                    var f = msgs.Where(k => k.Author.Id == u.Id);
                                    await Context.Channel.DeleteMessagesAsync(f);
                                    deleted = f.Count();
                                }
                                else
                                    await SendMessage("User " + arguments[0] + " not found.", Context.Guild.Id, Context.Channel.Id);
                            }
                        }
                        else
                        {
                            var msgs = await Context.Channel.GetMessagesAsync(100).Flatten();
                            await Context.Channel.DeleteMessagesAsync(msgs);
                            deleted = msgs.Count();
                        }
                        var botms = await SendMessage("Deleted " + deleted + " messages.", Context.Guild.Id, Context.Channel.Id);
                        await Task.Delay(5000);
                        await DeleteMessage(botms);
                        return true;
                    case "udetails":
                        var user = GetUser(arguments[0], Context.Guild.Id);
                        if (user != null)
                            await SendMessage(user.Mention + "'s details:\nUsername - " + user.Username + "\nNickname - " + ((user.Nickname == string.Empty ||
                              user.Nickname == null) ? "N/A" : user.Nickname) + "\nID - " + user.Id + "\nStatus - " + user.Status + "\nCustom Status/Playing - " +
                              (user.Game.HasValue ? user.Game.Value.Name : "N/A") + "\nCreated - " + user.CreatedAt + "\nJoined - " + user.JoinedAt, Context.Guild.Id,
                              Context.Channel.Id);
                        else
                            await SendMessage("User " + arguments[0] + " not found", Context.Guild.Id, Context.Channel.Id);
                        await DeleteMessage(Context.Message);
                        return true;
                }
            }
            if (HasRole(config.StaffRole, Context.Message.Author.Id, Context.Guild.Id) && comm.StartsWith("*"))
            {
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "decide":
                        if (ToCheck == null && learning.UndecidedQuestions.Count > 0)
                        {
                            ToCheck = new MSG() { Decision = learning.UndecidedQuestions[0], Channel = Context.Channel.Id, Guild = Context.Guild.Id };
                            var m = await SendMessage("ID: " + ToCheck.Decision.ID + "\nQuestion: " + ToCheck.Decision.Question, ToCheck.Guild, ToCheck.Channel);
                            ToCheck.msg = m;
                        }
                        await DeleteMessage(Context.Message);
                        break;
                    case "stop":
                        if (ToCheck != null)
                        {
                            await DeleteMessage(ToCheck.msg);
                            ToCheck = null;
                        }
                        await DeleteMessage(Context.Message);
                        break;
                    case "answer":
                        if (ToCheck != null)
                        {
                            if (arguments.Count == 0)
                            {
                                var d = SearchAnswer(ToCheck.Decision.Question);
                                if (d.Similarity > 0.8)
                                {
                                    var a = learning.Answers.Find(k => k.Answer == d.Answer);
                                    var ind = learning.Answers.IndexOf(a);
                                    a.Questions.Add(ToCheck.Decision.Question);
                                    learning.Answers[ind] = a;
                                    learning.UndecidedQuestions.RemoveAll(k => k.ID == ToCheck.Decision.ID);
                                    learning.SaveJson();
                                    await DeleteMessage(ToCheck.msg);

                                    if (learning.UndecidedQuestions.Count > 0)
                                    {
                                        ToCheck.Decision = learning.UndecidedQuestions[0];
                                        var msg = await SendMessage("ID: " + ToCheck.Decision.ID + "\nQuestion: " + ToCheck.Decision.Question, ToCheck.Guild, ToCheck.Channel);
                                        ToCheck.msg = msg;
                                    }
                                    else
                                        ToCheck = null;
                                }
                            }
                            else if (arguments.Count > 0)
                            {
                                learning.Answers.Add(new Answered() { Answer = string.Join(" ", arguments), Questions = new List<string>() { ToCheck.Decision.Question } });
                                learning.UndecidedQuestions.RemoveAll(k => k.ID == ToCheck.Decision.ID);
                                learning.SaveJson();
                                await DeleteMessage(ToCheck.msg);

                                if (learning.UndecidedQuestions.Count > 0)
                                {
                                    ToCheck.Decision = learning.UndecidedQuestions[0];
                                    var msg = await SendMessage("ID: " + ToCheck.Decision.ID + "\nQuestion: " + ToCheck.Decision.Question, ToCheck.Guild, ToCheck.Channel);
                                    ToCheck.msg = msg;
                                }
                                else
                                    ToCheck = null;
                            }
                        }
                        await DeleteMessage(Context.Message);
                        break;
                    case "discard":
                        learning.NotQuestions.Add(ToCheck.Decision.Question);
                        learning.UndecidedQuestions.RemoveAll(k => k.ID == ToCheck.Decision.ID);
                        learning.SaveJson();
                        await DeleteMessage(ToCheck.msg);

                        if (learning.UndecidedQuestions.Count > 0)
                        {
                            ToCheck.Decision = learning.UndecidedQuestions[0];
                            var msg = await SendMessage("ID: " + ToCheck.Decision.ID + "\nQuestion: " + ToCheck.Decision.Question, ToCheck.Guild, ToCheck.Channel);
                            ToCheck.msg = msg;
                        }
                        else
                            ToCheck = null;

                        await DeleteMessage(Context.Message);
                        break;
                }
            }
            if ((HasRole(config.SupporterRole, Context.Message.Author.Id, Context.Guild.Id) || HasRole(config.StaffRole, Context.Message.Author.Id, Context.Guild.Id)) && Context.Channel.Id == config.SupportChannel && comm.StartsWith("&"))
            {
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "search":
                        if (arguments.Count > 0)
                        {
                            var a = SearchAnswer(string.Join(" ", arguments));
                            if (a.Similarity > 0.8)
                                await SendMessage(a.Answer, Context.Guild.Id, Context.Channel.Id);
                            else
                                await SendMessage("I did not find an answer for that question that was accurate enough.", Context.Guild.Id, Context.Channel.Id, 10);
                        }

                        await DeleteMessage(Context.Message);
                        break;
                }
            }
            return false;
        }

        private async Task<RestUserMessage> SendMessage(string Message, ulong GuildID, ulong ChannelID, int Delete = 0)
        {
            try
            {
                var msg = await ClientInstance.GetGuild(GuildID).GetTextChannel(ChannelID).SendMessageAsync(Message);
                if (Delete == 0)
                    return msg;
                else
                {
                    await Task.Delay(Delete * 1000);
                    await DeleteMessage(msg);
                    return null;
                }
            }
            catch
            {
                try
                {
                    var msg = await ClientInstance.GetGuild(GuildID).GetTextChannel(ChannelID).SendMessageAsync(Message);
                    if (Delete == 0)
                        return msg;
                    else
                    {
                        await Task.Delay(Delete * 1000);
                        await DeleteMessage(msg);
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }
        private async Task<IUserMessage> SendDirectMessage(string Message, SocketUser User)
        {
            try
            {
                return await User.SendMessageAsync(Message);
            }
            catch
            {
                try
                {
                    return await User.SendMessageAsync(Message);
                }
                catch
                {
                    return null;
                }
            }
        }
        private async Task DeleteMessage(IMessage Message)
        {
            try
            {
                await Message.DeleteAsync();
            }
            catch
            {
                try
                {
                    await Message.DeleteAsync();
                }
                catch { }
            }
        }
        private SocketGuildUser GetUser(string NameOrID, ulong GuildID)
        {
            try
            {
                return ClientInstance.GetGuild(GuildID).Users.ToList().Find(k =>
                {
                    if (ulong.TryParse(NameOrID, out ulong id))
                    {
                        bool? ID = k?.Id == id;
                        return ID.HasValue && ID.Value;
                    }
                    else
                    {
                        bool? username = k?.Username?.ToLowerInvariant().Contains(NameOrID.ToLowerInvariant());
                        bool? nickname = k?.Nickname?.ToLowerInvariant().Contains(NameOrID.ToLowerInvariant());
                        return (username.HasValue && username.Value) || (nickname.HasValue && nickname.Value);
                    }
                });
            }
            catch
            {
                try
                {
                    return ClientInstance.GetGuild(GuildID).Users.ToList().Find(k =>
                    {
                        if (ulong.TryParse(NameOrID, out ulong id))
                        {
                            bool? ID = k?.Id == id;
                            return ID.HasValue && ID.Value;
                        }
                        else
                        {
                            bool? username = k?.Username?.ToLowerInvariant().Contains(NameOrID.ToLowerInvariant());
                            bool? nickname = k?.Nickname?.ToLowerInvariant().Contains(NameOrID.ToLowerInvariant());
                            return (username.HasValue && username.Value) || (nickname.HasValue && nickname.Value);
                        }
                    });
                }
                catch
                {
                    return null;
                }
            }
        }
        private bool IsCommand(string message)
        {
            switch (message.Split()[0].ToLowerInvariant())
            {
                case "$shutdown":
                case "$game":
                case "/status":
                case "/purge":
                case "/udetails":
                case "&search":
                case "*decide":
                case "*stop":
                case "*answer":
                case "*discard":
                    return true;
                default:
                    return false;
            }
        }
        private bool HasRole(ulong RoleID, ulong UserID, ulong GuildID) =>
            UserID == Configuration.Load().OwnerID ? true : (ClientInstance?.GetGuild(GuildID)?.GetUser(UserID)?.Roles?.ToList()?.Exists(k => k.Id == RoleID) ?? false);

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

                    if (s.EndsWith("\"") && !s.EndsWith("\\\""))
                    {
                        string t = Combined.TrimEnd('"');
                        result.Add(t);
                        Combined = "";
                        continue;
                    }
                    else if (s.EndsWith("\\\""))
                        result.Add(s.Remove(s.Length - 2));

                    if (Combined == "")
                        result.Add(s);
                }
            }
            catch { }
            return result;
        }

        public Data SearchAnswer(string Question)
        {
            Learning.EnsureExists();

            var msg = Question.Split().ToList();
            foreach (var s in msg)
                if (s.StartsWith("<@") || s.StartsWith("<#") || s.StartsWith("<:"))
                    msg.Remove(s);

            Question = string.Join(" ", msg);

            Data d = new Data() { Similarity = 0.0, Phrase = "", Answer = "" };
            foreach (var s in Learning.Load().NotQuestions)
            {
                if (CalculateSimilarity(s, Question) > (double)0.8)
                {
                    d.Similarity = double.MinValue;
                    d.Phrase = "NOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWER";
                    d.Answer = "NOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWER";
                    return d;
                }
            }

            foreach (var StoredMSG in Learning.Load().Answers)
            {
                foreach (var q in StoredMSG.Questions)
                {
                    var sim = CalculateSimilarity(q, Question);

                    if (sim > d.Similarity)
                    {
                        d.Similarity = sim;
                        d.Phrase = Question;
                        d.Answer = StoredMSG.Answer;
                    }
                }
            }
            return d.Similarity == 0.0 && d.Phrase == "" && d.Answer == "" ? null : d;
        }

        private int ComputeLevenshteinDistance(string source, string target)
        {
            if ((source == null) || (target == null)) return 0;
            if ((source.Length == 0) || (target.Length == 0)) return 0;
            if (source == target) return source.Length;
            int sourceWordCount = source.Length;
            int targetWordCount = target.Length;
            if (sourceWordCount == 0)
                return targetWordCount;
            if (targetWordCount == 0)
                return sourceWordCount;
            int[,] distance = new int[sourceWordCount + 1, targetWordCount + 1];
            for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
            for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;
            for (int i = 1; i <= sourceWordCount; i++)
            {
                for (int j = 1; j <= targetWordCount; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
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
    }
}