using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Pustalorc.Applications.Support_Bot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Message = Pustalorc.Applications.Support_Bot.Classes.Message;

namespace Pustalorc.Applications.Support_Bot
{
    public sealed class Entry
    {
        private DiscordSocketClient ClientInstance;
        private const ulong OwnerID = 181418012400812032;
        private string SupporterRole = "Supporter";
        private string StaffRole = "USO Team";

        private static void Main(string[] args) =>
            new Entry().StartAsync(args).GetAwaiter().GetResult();
        
        public async Task StartAsync(string[] args)
        {
            try
            {
                Configuration.EnsureExists();
                ClientInstance = new DiscordSocketClient(new DiscordSocketConfig() { LogLevel = LogSeverity.Verbose, MessageCacheSize = 1000, DefaultRetryMode = RetryMode.AlwaysRetry });
                ClientInstance.Log += (l) => Console.Out.WriteLineAsync(l.ToString());

                await ClientInstance.LoginAsync(TokenType.Bot, Configuration.Load().Token);
                await ClientInstance.StartAsync();
                await ClientInstance.SetGameAsync("Answering questions at #bot-support!");

                ClientInstance.MessageReceived += async (o) => await HandleMessage(new SocketCommandContext(ClientInstance, o as SocketUserMessage), false);
                ClientInstance.MessageUpdated += async (a, o, u) => await HandleMessage(new SocketCommandContext(ClientInstance, o as SocketUserMessage), true);
                ClientInstance.MessageDeleted += _client_MessageDeleted;

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }
        
        private async Task _client_MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            if (!arg1.HasValue || arg1.Value == null || arg1.Value.Author.IsBot)
                return;

            if (string.Equals(arg2.Name, "bot-support", StringComparison.InvariantCultureIgnoreCase))
                await Delete(arg1.Id);
        }
        private async Task HandleMessage(SocketCommandContext Context, bool IsUpdate)
        {
            var Author = Context.Message.Author;
            if (Author.IsBot)
                return;

            if (IsCommand(Context.Message.Content))
            {
                HandleCommand(Context);
                return;
            }

            if (string.Equals(Context.Channel.Name, "bot-support", StringComparison.InvariantCultureIgnoreCase))
            {
                if (Context.Message.MentionedUsers.Count > 0 || Context.Message.MentionedRoles.Count > 0)
                {
                    await DeleteMessage(Context.Message);
                    return;
                }

                if (IsUpdate)
                    await Update(Context);
                else
                    await AddToDecide(Context);
            }
            else if (string.Equals(Context.Channel.Name, "general", StringComparison.InvariantCultureIgnoreCase) && (Context.Message.MentionedRoles.ToList().Exists(k => k.Name == SupporterRole ) || Context.Message.Content.Split().ToList().Exists(k => string.Equals(k, "help", StringComparison.InvariantCultureIgnoreCase) || SearchAnswer(Context.Message.Content).Similarity > (double)0.8) || MessageTagsHelper(Context)))
            {
                await DeleteMessage(Context.Message);
                SendMessage(Context.Message.Author.Mention + " please ask a full question with a tag to the helper role in #support or #support-2.", Context.Guild.Id, Context.Channel.Id, 5);
            }
            else if (string.Equals(Context.Channel.Name, "images", StringComparison.InvariantCultureIgnoreCase) && Context.Message.Attachments.Count == 0)
                await DeleteMessage(Context.Message);
            else if (string.Equals(Context.Channel.Name, "support", StringComparison.InvariantCultureIgnoreCase) || string.Equals(Context.Channel.Name, "support-2", StringComparison.InvariantCultureIgnoreCase))
            {
                if (MessageTagsStaff(Context) && !MessageTagsHelper(Context))
                {
                    await DeleteMessage(Context.Message);
                    SendMessage(Context.Message.Author + " please do not tag staff directly unless it's HIGHLY important. If so, leave a message in #general with their name. No tag.", Context.Guild.Id, Context.Channel.Id, 5);
                    return;
                }

                var a = SearchAnswer(Context.Message.Content);
                if (a.Similarity > (double)0.8)
                    SendMessage(Context.Message.Author.Mention + ": " + a.Answer, Context.Guild.Id, Context.Channel.Id);
            }
            else if (string.Equals(Context.Channel.Name, "advertisements", StringComparison.InvariantCultureIgnoreCase))
            {
                var ValidIpAddressRegex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
                var ValidHostnameRegex = new Regex(@"^(https:\/\/|http:\/\/)([a-zA-Z0-9-]+\.){0,5}[a-zA-Z0-9-][a-zA-Z0-9-]+\.[a-zA-Z]{2,64}?\/[a-zA-Z0-9-][a-zA-Z0-9-]+$");

                if (Context.Message.Content.Split(' ').ToList().Exists(l => l.Split('\n').ToList().Exists(k => ValidIpAddressRegex.IsMatch(k) || ValidHostnameRegex.IsMatch(k)) && Context.Message.Content.Split(' ').Length > 1))
                {
                    var config = Learning.Load();
                    if (config.AdvertCooldowns.ContainsKey(Context.Message.Author.Id))
                    {
                        var Cooldown = config.AdvertCooldowns[Context.Message.Author.Id];
                        if (Cooldown > DateTime.Now)
                        {
                            await DeleteMessage(Context.Message);
                            SendMessage(Context.Message.Author.Mention + " you cannot send any more advertisements today! Come back tomorrow!", Context.Guild.Id, Context.Channel.Id, 5);
                        }
                        else
                        {
                            config.AdvertCooldowns[Context.Message.Author.Id] = DateTime.Now.AddDays(1);
                            config.SaveJson();
                        }
                    }
                    else
                    {
                        config.AdvertCooldowns.Add(Context.Message.Author.Id, DateTime.Now.AddDays(1));
                        config.SaveJson();
                    }
                }
                else
                {
                    await DeleteMessage(Context.Message);
                    SendMessage(Context.Message.Author.Mention + " your advertisement doesn't include an IP or a website, or it is too short. Please post it with an IP or a website and/or make it longer.", Context.Guild.Id, Context.Channel.Id, 5);
                }
            }
        }
        private async Task<bool> HandleCommand(SocketCommandContext Context)
        {
            var executed = Context.Message.Content.Split(' ').ToList();
            var comm = executed[0];
            executed.Remove(comm);
            var arguments = CheckDoubleQuotes(executed);

            if (Context.Message.Author.Id == OwnerID && comm.StartsWith("$"))
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
                    case "say":
                        var guild = ulong.Parse(arguments[0]);
                        var channel = ulong.Parse(arguments[1]);
                        arguments.RemoveRange(0, 2);
                        await SendMessage(string.Join(" ", arguments), guild, channel);
                        return true;
                    case "clear":
                        var config = Learning.Load();
                        if (string.Equals(arguments[0], "warns", StringComparison.InvariantCultureIgnoreCase))
                        {
                            config.WarnedPeople.Clear();
                            config.SaveJson();
                        }
                        else if (string.Equals(arguments[0], "advertcooldowns", StringComparison.InvariantCultureIgnoreCase))
                        {
                            config.AdvertCooldowns.Clear();
                            config.SaveJson();
                        }
                        else if (string.Equals(arguments[0], "indecisions", StringComparison.InvariantCultureIgnoreCase))
                        {
                            config.UndecidedQuestions.Clear();
                            config.SaveJson();
                        }
                        await DeleteMessage(Context.Message);
                        return true;
                    case "set":
                        string type = arguments[0];
                        arguments.RemoveAt(0);

                        if (string.Equals(type, "support", StringComparison.InvariantCultureIgnoreCase))
                            SupporterRole = string.Join(" ", arguments);
                        else if (string.Equals(type, "staff", StringComparison.InvariantCultureIgnoreCase))
                            StaffRole = string.Join(" ", arguments);

                        await DeleteMessage(Context.Message);
                        return true;
                }
            }
            if (Context.Guild.Roles.FirstOrDefault(k => string.Equals(k.Name, StaffRole, StringComparison.InvariantCultureIgnoreCase)).Members.ToList().Exists(k => k.Id == Context.Message.Author.Id) && comm.StartsWith("/"))
            {
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "warn":
                        if (arguments.Count >= 2)
                        {
                            var u = GetUser(arguments[0], Context.Guild.Id);
                            if (u != null)
                            {
                                var config = Learning.Load();
                                arguments.RemoveRange(0, 1);
                                var reason = string.Join(" ", arguments);
                                config.WarnedPeople.Add(new Warns() { ID = u.Id, Reason = reason });
                                config.SaveJson();
                                await SendMessage(u.Mention + " has been warned for: " + reason + ". This is their warn #" + config.WarnedPeople.Where(k => k.ID == u.Id).Count() + ".", Context.Guild.Id, Context.Guild.TextChannels.FirstOrDefault(k => string.Equals(k.Name, "warns", StringComparison.InvariantCultureIgnoreCase)).Id);
                            }
                            else
                                await SendMessage("User " + arguments[0] + " not found.", Context.Guild.Id, Context.Channel.Id);
                        }
                        await DeleteMessage(Context.Message);
                        return true;
                    case "status":
                        var d = DateTime.Now;
                        await ClientInstance.GetConnectionsAsync();
                        await SendMessage("----------- Support bot V3.1 status report -----------\nPing: " + (ulong)DateTime.Now.Subtract(d).TotalMilliseconds + "ms.\nRunning on " + Environment.OSVersion + ".\n----------- Support bot V3.1 status report -----------", Context.Guild.Id, Context.Channel.Id);
                        await DeleteMessage(Context.Message);
                        return true;
                    case "google":
                        await SendMessage(string.Format("https://www.google.com/search?q={0}", WebUtility.UrlEncode(string.Join(" ", arguments))), Context.Guild.Id, Context.Channel.Id);
                        await DeleteMessage(Context.Message);
                        return true;
                    case "psearch":
                        await SendMessage(string.Format("https://hub.rocketmod.net/?s={0}", WebUtility.UrlEncode(string.Join(" ", arguments))), Context.Guild.Id, Context.Channel.Id);
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
                                        await Context.Channel.DeleteMessagesAsync(msgs);
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
                                            await Context.Channel.DeleteMessagesAsync(msgs);
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
                                        await Context.Channel.DeleteMessagesAsync(msgs);
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
                                    await Context.Channel.DeleteMessagesAsync(msgs);
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
                                    await Context.Channel.DeleteMessagesAsync(msgs);
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
                            await SendMessage(user.Mention + "'s details:\nUsername - " + user.Username + "\nNickname - " + ((user.Nickname == string.Empty || user.Nickname == null) ? "N/A" : user.Nickname) + "\nID - " + user.Id + "\nStatus - " + user.Status + "\nCustom Status/Playing - " + (user.Game.HasValue ? user.Game.Value.Name : "N/A") + "\nCreated - " + user.CreatedAt + "\nJoined - " + user.JoinedAt, Context.Guild.Id, Context.Channel.Id);
                        else
                            await SendMessage("User " + arguments[0] + " not found", Context.Guild.Id, Context.Channel.Id);
                        await DeleteMessage(Context.Message);
                        return true;
                }
            }
            if (string.Equals(Context.Channel.Name, "bot-decisions", StringComparison.InvariantCultureIgnoreCase) && comm.StartsWith("*"))
            {
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "d":
                        await Discard(arguments[0]);
                        await DeleteMessage(Context.Message);
                        return true;
                    case "a":
                        var index = arguments[0];
                        arguments.Remove(index);
                        await Answer(ulong.Parse(index), string.Join(" ", arguments));
                        await DeleteMessage(Context.Message);
                        return true;
                    case "y":
                        await Answer(ulong.Parse(arguments[0]));
                        await DeleteMessage(Context.Message);
                        return true;
                }
            }
            if (comm.StartsWith("!"))
            {
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "udetails":
                        try
                        {
                            var caller = GetUser(Context.Message.Author.Id.ToString(), Context.Guild.Id);
                            await SendDirectMessage("Your details:\nUsername - " + caller.Username + "\nNickname - " + (string.IsNullOrEmpty(caller.Nickname) ? "N/A" : caller.Nickname) + "\nID - " + caller.Id + "\nStatus - " + caller.Status + "\nCustom Status/Playing - " + (caller.Game.HasValue ? caller.Game.Value.Name : "N/A") + "\nCreated - " + caller.CreatedAt + "\nJoined - " + caller.JoinedAt, caller);
                        }
                        catch
                        {
                            if (!(await Context.Channel.GetMessagesAsync().Flatten()).FirstOrDefault().Content.Contains("I am unable to dm you your details! Please enable public direct messaging, otherwise you can't use this command."))
                            {
                                var a = await SendMessage(Context.Message.Author.Mention + " I am unable to dm you your details! Please enable public direct messaging, otherwise you can't use this command.", Context.Guild.Id, Context.Channel.Id);
                                await Task.Delay(5000);
                                await DeleteMessage(a);
                            }
                        }
                        await DeleteMessage(Context.Message);
                        return true;
                    case "google":
                        try
                        {
                            await SendDirectMessage(string.Format("https://www.google.com/search?q={0}", WebUtility.UrlEncode(string.Join(" ", arguments))), Context.Message.Author);
                        }
                        catch
                        {
                            if (!(await Context.Channel.GetMessagesAsync().Flatten()).FirstOrDefault().Content.Contains("I am unable to dm you your google search! Please enable public direct messaging, otherwise you can't use this command."))
                            {
                                var a = await SendMessage(Context.Message.Author.Mention + " I am unable to dm you your google search! Please enable public direct messaging, otherwise you can't use this command.", Context.Guild.Id, Context.Channel.Id);
                                await Task.Delay(5000);
                                await DeleteMessage(a);
                            }
                        }
                        await DeleteMessage(Context.Message);
                        return true;
                    case "psearch":
                        try
                        {
                            await SendDirectMessage(string.Format("https://hub.rocketmod.net/?s={0}", WebUtility.UrlEncode(string.Join(" ", arguments))), Context.Message.Author);
                        }
                        catch
                        {
                            if (!(await Context.Channel.GetMessagesAsync().Flatten()).FirstOrDefault().Content.Contains("I am unable to dm you your google search! Please enable public direct messaging, otherwise you can't use this command."))
                            {
                                var a = await SendMessage(Context.Message.Author.Mention + " I am unable to dm you your google search! Please enable public direct messaging, otherwise you can't use this command.", Context.Guild.Id, Context.Channel.Id);
                                await Task.Delay(5000);
                                await DeleteMessage(a);
                            }
                        }
                        await DeleteMessage(Context.Message);
                        return true;
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
                    await msg.DeleteAsync();
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
                        await msg.DeleteAsync();
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
                case "$say":
                case "$clear":
                case "$set":
                case "/warn":
                case "/status":
                case "/google":
                case "/psearch":
                case "/purge":
                case "/udetails":
                case "!udetails":
                case "!google":
                case "!psearch":
                case "*d":
                case "*a":
                case "*y":
                    return true;
                default:
                    return false;
            }
        }
        private bool MessageTagsStaff(SocketCommandContext message)
        {
            foreach (var a in message.Message.MentionedUsers)
            {
                if (message.Guild.GetUser(a.Id).Roles.ToList().Exists(k => string.Equals(k.Name, StaffRole, StringComparison.InvariantCultureIgnoreCase)))
                    return true;
            }
            return false;
        }
        private bool MessageTagsHelper(SocketCommandContext message)
        {
            foreach (var a in message.Message.MentionedUsers)
            {
                if (message.Guild.GetUser(a.Id).Roles.ToList().Exists(k => string.Equals(k.Name, SupporterRole, StringComparison.InvariantCultureIgnoreCase)))
                    return true;
            }
            return false;
        }

        private async Task AddToDecide(SocketCommandContext context)
        {
            try
            {
                var config = Learning.Load();
                var channel = context.Guild.TextChannels.FirstOrDefault(k => string.Equals(k.Name, "bot-decisions", StringComparison.InvariantCultureIgnoreCase));
                var answer = SearchAnswer(context.Message.Content);
                if (answer != null)
                {
                    if (answer.Similarity == double.MinValue && answer.Phrase == "NOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWER" && answer.Answer == "NOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWER")
                    {
                        await DeleteMessage(context.Message);
                        var msg = await SendMessage(context.Message.Author.Mention + " Not a valid question.", context.Guild.Id, context.Channel.Id);
                        await Task.Delay(5000);
                        await DeleteMessage(msg);
                    }
                    else if (answer.Similarity == 1)
                        await SendMessage(context.Message.Author.Mention + ": " + answer.Answer, context.Guild.Id, context.Channel.Id);
                    else if (answer.Similarity >= (double)0.5)
                    {
                        var decision = await SendMessage("A possible answer was found, please specify if it's good with *y <index>, *d <index>, or *a <index> \"<answer>\". Here are the details:\nAnswer: " + answer.Answer + "\nQuestion: " + answer.Phrase + "\nSimilarity: " + (answer.Similarity * 100) + "%\nIndex: " + context.Message.Id, context.Guild.Id, channel.Id);
                        var tempanswer = await SendMessage(context.Message.Author.Mention + " (The following may not be correct): " + answer.Answer, context.Guild.Id, context.Channel.Id);
                        config.UndecidedQuestions.Add(new Deciding(context.Message.Content, context.Message.Id, context.Message.Author.Mention, new Message(decision.Id, channel.Id, context.Guild.Id), new Message(tempanswer.Id, context.Channel.Id, context.Guild.Id)));
                    }
                    else
                    {
                        var decision = await SendMessage("This possible question requires an answer. To discard it use *d <index>, otherwise answer it with *a <index> \"<answer>\"\nQuestion: " + context.Message.Content + "\nIndex: " + context.Message.Id, context.Guild.Id, channel.Id);
                        var tempanswer = await SendMessage("I'm sorry, but I currently do not have an answer for you. Staff of this discord are currently trying to get you one, so please hold tight and check this message again in a few minutes.", context.Guild.Id, context.Channel.Id);
                        config.UndecidedQuestions.Add(new Deciding(context.Message.Content, context.Message.Id, context.Message.Author.Mention, new Message(decision.Id, channel.Id, context.Guild.Id), new Message(tempanswer.Id, context.Channel.Id, context.Guild.Id)));
                    }
                }
                else
                {
                    var decision = await SendMessage("This possible question requires an answer. To discard it use *d <index>, otherwise answer it with *a <index> \"<answer>\"\nQuestion: " + context.Message.Content + "\nIndex: " + context.Message.Id, context.Guild.Id, channel.Id);
                    var tempanswer = await SendMessage("I'm sorry, but I currently do not have an answer for you. Staff of this discord are currently trying to get you one, so please hold tight and check this message again in a few minutes.", context.Guild.Id, context.Channel.Id);
                    config.UndecidedQuestions.Add(new Deciding(context.Message.Content, context.Message.Id, context.Message.Author.Mention, new Message(decision.Id, channel.Id, context.Guild.Id), new Message(tempanswer.Id, context.Channel.Id, context.Guild.Id)));
                }
                config.SaveJson();
            }
            catch { }
        }
        private async Task Answer(ulong index, string answer = "")
        {
            var config = Learning.Load();
            if (config.UndecidedQuestions.Exists(k => k.ID == index))
            {
                var q = config.UndecidedQuestions.Find(k => k.ID == index);
                var i = config.UndecidedQuestions.IndexOf(q);
                if (string.IsNullOrEmpty(answer))
                {
                    var a = SearchAnswer(q.Question).Answer;
                    await DeleteMessage(await ClientInstance.GetGuild(q.DecidingMessage.GuildID).GetTextChannel(q.DecidingMessage.ChannelID).GetMessageAsync(q.DecidingMessage.MessageID));
                    await (await ClientInstance.GetGuild(q.NotFoundMessage.GuildID).GetTextChannel(q.NotFoundMessage.ChannelID).GetMessageAsync(q.NotFoundMessage.MessageID) as SocketUserMessage).ModifyAsync(k => k.Content = q.Mention + ": " + a);
                    config.UndecidedQuestions.RemoveAt(i);
                    var b = config.Answers.Find(k => string.Equals(k.Answer, a, StringComparison.InvariantCultureIgnoreCase));
                    var ind = config.Answers.FindIndex(k => string.Equals(k.Answer, a, StringComparison.InvariantCultureIgnoreCase));
                    if (b != null)
                    {
                        b.Questions.Add(q.Question);
                        config.Answers[ind] = b;
                        config.SaveJson();
                    }
                    else
                    {
                        config.Answers.Add(new Answered() { Questions = new List<string>() { q.Question }, Answer = a });
                        config.SaveJson();
                    }
                }
                else
                {
                    await DeleteMessage(await ClientInstance.GetGuild(q.DecidingMessage.GuildID).GetTextChannel(q.DecidingMessage.ChannelID).GetMessageAsync(q.DecidingMessage.MessageID));
                    await (await ClientInstance.GetGuild(q.NotFoundMessage.GuildID).GetTextChannel(q.NotFoundMessage.ChannelID).GetMessageAsync(q.NotFoundMessage.MessageID) as SocketUserMessage).ModifyAsync(k => k.Content = q.Mention + ": " + answer);
                    config.UndecidedQuestions.RemoveAt(i);
                    var a = config.Answers.Find(k => string.Equals(k.Answer, answer, StringComparison.InvariantCultureIgnoreCase));
                    var ind = config.Answers.FindIndex(k => string.Equals(k.Answer, answer, StringComparison.InvariantCultureIgnoreCase));
                    if (a != null)
                    {
                        a.Questions.Add(q.Question);
                        config.Answers[ind] = a;
                        config.SaveJson();
                    }
                    else
                    {
                        config.Answers.Add(new Answered() { Questions = new List<string>() { q.Question }, Answer = answer });
                        config.SaveJson();
                    }
                }
            }
            config.SaveJson();
        }
        private async Task Discard(string index)
        {
            var config = Learning.Load();
            if (ulong.TryParse(index, out ulong i) && config.UndecidedQuestions.Exists(k => k.ID == i))
            {
                var q = config.UndecidedQuestions.Find(k => k.ID == i);
                var ind = config.UndecidedQuestions.IndexOf(q);
                await DeleteMessage(await ClientInstance.GetGuild(q.DecidingMessage.GuildID).GetTextChannel(q.DecidingMessage.ChannelID).GetMessageAsync(q.DecidingMessage.MessageID));
                await DeleteMessage(await ClientInstance.GetGuild(q.NotFoundMessage.GuildID).GetTextChannel(q.NotFoundMessage.ChannelID).GetMessageAsync(q.NotFoundMessage.MessageID));
                await DeleteMessage(await ClientInstance.GetGuild(q.NotFoundMessage.GuildID).GetTextChannel(q.NotFoundMessage.ChannelID).GetMessageAsync(q.ID));
                config.NotQuestions.Add(q.Question);
                config.UndecidedQuestions.RemoveAt(ind);
            }
            else if (string.Equals(index, "all", StringComparison.InvariantCultureIgnoreCase))
            {
                List<IMessage> msg = new List<IMessage>();
                foreach (var q in config.UndecidedQuestions.ToList())
                {
                    var ind = config.UndecidedQuestions.IndexOf(q);
                    await DeleteMessage(await ClientInstance.GetGuild(q.DecidingMessage.GuildID).GetTextChannel(q.DecidingMessage.ChannelID).GetMessageAsync(q.DecidingMessage.MessageID));
                    await DeleteMessage(await ClientInstance.GetGuild(q.NotFoundMessage.GuildID).GetTextChannel(q.NotFoundMessage.ChannelID).GetMessageAsync(q.NotFoundMessage.MessageID));
                    await DeleteMessage(await ClientInstance.GetGuild(q.NotFoundMessage.GuildID).GetTextChannel(q.NotFoundMessage.ChannelID).GetMessageAsync(q.ID));
                    config.NotQuestions.Add(q.Question);
                    config.UndecidedQuestions.RemoveAt(ind);
                }
            }
            config.SaveJson();
        }
        private async Task Update(SocketCommandContext context)
        {
            var config = Learning.Load();
            var q = config.UndecidedQuestions.Find(k => k.ID == context.Message.Id);
            var achan = ClientInstance.GetGuild(q.NotFoundMessage.GuildID).GetTextChannel(q.NotFoundMessage.ChannelID);
            var dchan = ClientInstance.GetGuild(q.DecidingMessage.GuildID).GetTextChannel(q.DecidingMessage.ChannelID);
            var answer = SearchAnswer(context.Message.Content);
            q.Question = context.Message.Content;
            if (answer != null)
            {
                if (answer.Similarity == double.MinValue && answer.Phrase == "NOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWER" && answer.Answer == "NOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWERNOANSWER")
                {
                    await DeleteMessage(context.Message);
                    var msg = await achan.GetMessageAsync(q.NotFoundMessage.MessageID) as SocketUserMessage;
                    await msg.ModifyAsync(k => k.Content = context.Message.Author.Mention + " Not a valid question.");
                    await Task.Delay(5000);
                    await DeleteMessage(msg);
                }
                else if (answer.Similarity == 1)
                {
                    await (await achan.GetMessageAsync(q.NotFoundMessage.MessageID) as SocketUserMessage).ModifyAsync(k => k.Content = context.Message.Author.Mention + ": " + answer.Answer);
                    await DeleteMessage(await dchan.GetMessageAsync(q.DecidingMessage.MessageID));
                    config.UndecidedQuestions.Remove(q);
                }
                else if (answer.Similarity > (double)0.5)
                {
                    await (await dchan.GetMessageAsync(q.DecidingMessage.MessageID) as SocketUserMessage).ModifyAsync(k => k.Content = "A possible answer was found, please specify if it's good with *y <index>, *d <index>, or *a <index> \"<answer>\". Here are the details:\nAnswer: " + answer.Answer + "\nQuestion: " + answer.Phrase + "\nSimilarity: " + (answer.Similarity * 100) + "%\nIndex: " + config.UndecidedQuestions.IndexOf(q));
                    await (await achan.GetMessageAsync(q.NotFoundMessage.MessageID) as SocketUserMessage).ModifyAsync(k => k.Content = context.Message.Author.Mention + " (The following may not be correct): " + answer.Answer);
                }
                else
                {
                    await (await dchan.GetMessageAsync(q.DecidingMessage.MessageID) as SocketUserMessage).ModifyAsync(k => k.Content = "This possible question requires an answer. To discard it use *d <index>, otherwise answer it with *a <index> \"<answer>\"\nQuestion: " + context.Message.Content + "\nIndex: " + config.UndecidedQuestions.IndexOf(q));
                    await (await achan.GetMessageAsync(q.NotFoundMessage.MessageID) as SocketUserMessage).ModifyAsync(k => k.Content = "I'm sorry, but I currently do not have an answer for you. Staff of this discord are currently trying to get you one, so please hold tight and check this message again in a few minutes.");
                }
            }
            else
            {
                await (await dchan.GetMessageAsync(q.DecidingMessage.MessageID) as SocketUserMessage).ModifyAsync(k => k.Content = "This possible question requires an answer. To discard it use *d <index>, otherwise answer it with *a <index> \"<answer>\"\nQuestion: " + context.Message.Content + "\nIndex: " + config.UndecidedQuestions.IndexOf(q));
                await (await achan.GetMessageAsync(q.NotFoundMessage.MessageID) as SocketUserMessage).ModifyAsync(k => k.Content = "I'm sorry, but I currently do not have an answer for you. Staff of this discord are currently trying to get you one, so please hold tight and check this message again in a few minutes.");
            }
            config.SaveJson();
        }
        private async Task Delete(ulong questionID)
        {
            var config = Learning.Load();
            var q = config.UndecidedQuestions.Find(k => k.NotFoundMessage.MessageID == questionID);
            if (q != null)
            {
                var dchan = ClientInstance.GetGuild(q.DecidingMessage.GuildID).GetTextChannel(q.DecidingMessage.ChannelID);
                await DeleteMessage(await dchan.GetMessageAsync(q.DecidingMessage.MessageID));
                var achan = ClientInstance.GetGuild(q.NotFoundMessage.GuildID).GetTextChannel(q.NotFoundMessage.ChannelID);
                await DeleteMessage(await achan.GetMessageAsync(q.NotFoundMessage.MessageID));
                config.UndecidedQuestions.Remove(q);
            }
            config.SaveJson();
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

                    if (s.EndsWith("\"") && !s.EndsWith("\\\""))
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

        public Data SearchAnswer(string Question)
        {
            Learning.EnsureExists();
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
            return d.Similarity == 0 && d.Phrase == "" && d.Answer == "" ? null : d;
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
    }
}
