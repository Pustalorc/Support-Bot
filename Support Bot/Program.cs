using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Pustalorc.Applications.SupportBot_;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace Pustalorc.Applications
{
    public sealed class SupportBot
    {
        private DiscordSocketClient ClientInstance;
        private Timer Timer = null;
        private bool FirstStart = false;
        private ushort Requests = 0;
        private List<AntiSpam> Spam = new List<AntiSpam>();
        private List<ulong> WarnedSpammers = new List<ulong>();

        private static void Main(string[] args) =>
            new SupportBot().StartAsync(args).GetAwaiter().GetResult();

        public async Task StartAsync(string[] args)
        {
            try
            {
                FirstStart = Configuration.EnsureExists();
                Learning.EnsureExists();

                ClientInstance = new DiscordSocketClient(new DiscordSocketConfig()
                {
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 1000,
                    DefaultRetryMode = RetryMode.AlwaysFail
                });

                ClientInstance.Log += (l) => Console.Out.WriteLineAsync(l.ToString());
                ClientInstance.MessageReceived += async (o) => await HandleMessage(new SocketCommandContext(ClientInstance, o as SocketUserMessage));
                ClientInstance.ReactionAdded += async (a, i, o) => await HandleReaction(o);
                ClientInstance.Ready += HandleReady;

                await ClientInstance.LoginAsync(TokenType.Bot, Configuration.Load().Token);
                await ClientInstance.StartAsync();
                await ClientInstance.SetGameAsync("Stuff, prob memes");

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        private async Task HandleReady()
        {
            var conf = Configuration.Load();
            var guild = ClientInstance.Guilds.ToList()[0];

            if (FirstStart)
            {
                var emotes = guild.Emotes.ToList();
                var emoteUp = emotes.Find(k => k.Name.ToLowerInvariant() == conf.RequestAcceptEmote.ToLowerInvariant());
                var emoteDown = emotes.Find(k => k.Name.ToLowerInvariant() == conf.RequestDownvoteEmote.ToLowerInvariant());
                var emoteBan = emotes.Find(k => k.Name.ToLowerInvariant() == conf.RequestBanEmote.ToLowerInvariant());
                var emoteAdminBan = emotes.Find(k => k.Name.ToLowerInvariant() == conf.AdminBanEmote.ToLowerInvariant());
                var emoteAdminDeny = emotes.Find(k => k.Name.ToLowerInvariant() == conf.AdminDenyEmote.ToLowerInvariant());

                await SendMessage("This channel was created to provide a better place for people to ask for support." +
                    "\n\nKeep in mind that this channel is **ONLY** dedicated to serious help topics." +
                    " People wanting to help a user should press the "
                    + (emoteUp != null ? emoteUp.ToString() : conf.RequestAcceptEmote) + 
                    " emote, and the person who made the request will receive a notification in DM of someone wanting to answer his" +
                    " request.\n\nSpamming this channel with help or a lot of comments is not allowed, and anyone found abusing the" +
                    " system will be banned from this channel entirely.\n\n**How does this work?**\n- The user who wants help posts" +
                    " their message using the format down below. The bot will automatically add the 3 emotes to be used for" +
                    " accepting/denying/ban voting. For this server, those emotes are: "
                    + (emoteUp != null ? emoteUp.ToString() : conf.RequestAcceptEmote) + " to accept, "
                    + (emoteDown != null ? emoteDown.ToString() : conf.RequestDownvoteEmote) + " to deny, "
                    + (emoteBan != null ? emoteBan.ToString() : conf.RequestBanEmote) +
                    " to ban. If a message receives " + conf.BanvotesForRemoval + " ban votes, the creator of the message will be" +
                    " banned from the channel. If a message receives " + conf.DownvotesForDenial + " denial votes, the message" +
                    " will be deleted and no support will be provided.\nStaff and supporters may instantly ban ("
                    + (emoteAdminBan != null ? emoteAdminBan.ToString() : conf.AdminBanEmote) + ") or deny ("
                    + (emoteAdminDeny != null ? emoteAdminDeny.ToString() : conf.AdminDenyEmote) + ") a message with special emotes.\n\nExample Message: ```css\nIssue:\n\"My server keeps closing" +
                    " without me doing anything! I will provide error logs for whoever wants to help me!\"```\n\n**If your message" +
                    " is not removed and you have received help, please delete it manually.**\n\nDisclaimer: This channel is purged" +
                    " every " + conf.SupportRequestClearMilliseconds + "ms.", guild.Id, conf.SupportRequestsChannel);
            }
            
            if (Timer == null)
            {
                Timer = new Timer(conf.SupportRequestClearMilliseconds);
                Timer.Elapsed += async (o, p) =>
                {
                    try
                    {
                        if (Requests > 0)
                        {
                            var chann = guild.GetTextChannel(conf.SupportRequestsChannel);
                            var msgs = await chann.GetMessagesAsync(Requests).Flatten();
                            msgs.ToList().RemoveAll(k => k.Author.IsBot);
                            await chann.DeleteMessagesAsync(msgs);
                        }

                        if (Requests != 0)
                            Requests = 0;
                    }
                    catch { }
                };
                Timer.Start();
            }
        }

        private async Task HandleMessage(SocketCommandContext Context)
        {
            if (Context == null || Context.Message.Author.IsBot || Context.IsPrivate)
                return;
            
            if (IsCommand(Context.Message.Content))
            {
                HandleCommand(Context);
                return;
            }

            var config = Configuration.Load();

            if (IsSpam(Context.Message, out string res) &&
                !HasRole(config.SupporterRole, Context.Message.Author.Id, Context.Guild.Id) &&
                !HasRole(config.StaffRole, Context.Message.Author.Id, Context.Guild.Id))
            {
                if (WarnedSpammers.Exists(k => k == Context.Message.Author.Id))
                {
                    await Context.Guild.AddBanAsync(Context.Message.Author, 7, "Spamming. Anti-Spam ban.", new RequestOptions() { AuditLogReason = "Anti-spam ban" });
                    await SendMessage(Context.Message.Author.Mention + " banned for spamming.", Context.Guild.Id, Context.Channel.Id);
                    await SendMessage("Anti-spam triggered on " + Context.Message.Author.Id + ".\nReason: " + (res ?? "N/A") + ".\nMessage sent: " + Context.Message.Content + ".\nMessage ID (For discord reports if needed):" + Context.Message.Id + ".\nAction: Banned.", Context.Guild.Id, config.LogChannel);
                    WarnedSpammers.RemoveAll(k => k == Context.Message.Author.Id);
                }
                else
                {
                    await SendMessage(Context.Message.Author.Mention + " cease your spam immediately. Failure to do so will result in a ban. This is your first **AND FINAL** warning.", Context.Guild.Id, Context.Channel.Id);
                    await SendMessage("Anti-spam triggered on " + Context.Message.Author.Id + ".\nReason: " + (res ?? "N/A") + ".\nMessage sent: " + Context.Message.Content + ".\nMessage ID (For discord reports if needed):" + Context.Message.Id + ".\nAction: Warned.", Context.Guild.Id, config.LogChannel);
                    WarnedSpammers.Add(Context.Message.Author.Id);
                }

                return;
            }

            if (Context.Channel.Id == config.GeneralChannel)
            {
                if (Context.Message.MentionedRoles.ToList().Exists(k => k.Id == config.SupporterRole))
                {
                    await DeleteMessage(Context.Message);
                    await SendMessage(Context.Message.Author.Mention + ", please do not tag the supporter role in this channel. Only tag them in <#" + config.SupportChannel + ">. Thank you in advance.", Context.Guild.Id, Context.Channel.Id);
                }
                var learning = Learning.Load();
                if (learning.NoHelp.Exists(k => CalculateSimilarity(Context.Message.Content, k) > 0.80))
                    await SendMessage(Context.Message.Author.Mention + ", please don't ask for help in this channel.\nWe do not provide help/support in this channel since it is meant for talking about general stuff.\n If you want help, go in <#" + config.SupportChannel + "> and ask there for help after tagging the support role.\n\nThank you in advance.", Context.Guild.Id, Context.Channel.Id);
            }
            else if (Context.Channel.Id == config.SupportChannel)
            {
                if (!HasRole(config.StaffRole, Context.Message.Author.Id, Context.Guild.Id) && (config.SupporterAntiTagBypass && !HasRole(config.SupporterRole, Context.Message.Author.Id, Context.Guild.Id)))
                {
                    if (Context.Message.MentionedUsers.Count > 0)
                    {
                        await DeleteMessage(Context.Message);

                        var msg = Context.Message.Content.Split().ToList();

                        foreach (var s in msg.ToList())
                        {
                            if (s.StartsWith("<@"))
                                msg.Remove(s);
                            else if (string.Equals(s, "@everyone"))
                                msg.Remove(s);
                        }

                        if (msg.Count == 0)
                            return;

                        SendMessage(Context.Message.Author + " please do not tag anyone in support channels. They will respond when they can. Thank you.",
                          Context.Guild.Id, Context.Channel.Id, 10);

                        SendMessage(Context.Message.Author + " Says: " + string.Join(" ", msg), Context.Guild.Id, Context.Channel.Id);
                        return;
                    }
                }
            }
            else if (Context.Channel.Id == config.SupportRequestsChannel)
            {
                var emotes = Context.Guild.Emotes.ToList();
                var emoteUp = emotes.Find(k => k.Name.ToLowerInvariant() == config.RequestAcceptEmote.ToLowerInvariant());
                var emoteDown = emotes.Find(k => k.Name.ToLowerInvariant() == config.RequestDownvoteEmote.ToLowerInvariant());
                var emoteBan = emotes.Find(k => k.Name.ToLowerInvariant() == config.RequestBanEmote.ToLowerInvariant());
                var emoteAdminBan = emotes.Find(k => k.Name.ToLowerInvariant() == config.AdminBanEmote.ToLowerInvariant());
                var emoteAdminDeny = emotes.Find(k => k.Name.ToLowerInvariant() == config.AdminDenyEmote.ToLowerInvariant());

                if (emoteUp != null)
                    await Context.Message.AddReactionAsync(emoteUp);
                else
                    await Context.Message.AddReactionAsync(new Emoji(config.RequestAcceptEmote));
                
                if (emoteDown != null)
                    await Context.Message.AddReactionAsync(emoteDown);
                else
                    await Context.Message.AddReactionAsync(new Emoji(config.RequestDownvoteEmote));
                
                if (emoteBan != null)
                    await Context.Message.AddReactionAsync(emoteBan);
                else
                    await Context.Message.AddReactionAsync(new Emoji(config.RequestBanEmote));

                if (emoteAdminBan != null)
                    await Context.Message.AddReactionAsync(emoteAdminBan);
                else
                    await Context.Message.AddReactionAsync(new Emoji(config.AdminBanEmote));

                if (emoteAdminDeny != null)
                    await Context.Message.AddReactionAsync(emoteAdminDeny);
                else
                    await Context.Message.AddReactionAsync(new Emoji(config.AdminDenyEmote));

                Requests++;
            }
        }
        private async Task HandleReaction(SocketReaction Reaction)
        {
            if (Reaction.User.Value.IsBot || Reaction.UserId == Reaction.Message.Value.Author.Id)
                return;

            var conf = Configuration.Load();

            if (Reaction.Channel.Id == conf.SupportRequestsChannel)
            {
                if (Reaction.Emote.Name.ToLowerInvariant() == conf.RequestAcceptEmote.ToLowerInvariant())
                {
                    await DeleteMessage(Reaction.Message.Value);
                    Requests--;

                    await SendDirectMessage(Reaction.User.Value.Mention + " reacted to your request for support and he is willing to help! You can talk with him on Direct Messages or in <#" + conf.SupportChannel + ">", Reaction.Message.Value.Author);
                    await SendDirectMessage(Reaction.Message.Value.Content, Reaction.User.Value);

                    await SendMessage(Reaction.User.Value.Mention + " accepted to help " + Reaction.Message.Value.Author.Mention + ".\nThe help topic is: \n" + Reaction.Message.Value.Content, ClientInstance.Guilds.ToList()[0].Id, conf.LogChannel);
                }
                else if ((Reaction.Emote.Name.ToLowerInvariant() == conf.RequestDownvoteEmote.ToLowerInvariant() &&
                    Reaction.Message.Value.Reactions[Reaction.Emote].ReactionCount - 1 >= conf.DownvotesForDenial) ||
                    ((HasRole(conf.StaffRole, Reaction.UserId, ClientInstance.Guilds.ToList()[0].Id) ||
                    HasRole(conf.SupporterRole, Reaction.UserId, ClientInstance.Guilds.ToList()[0].Id)) &&
                    Reaction.Emote.Name.ToLowerInvariant() == conf.AdminDenyEmote.ToLowerInvariant()))
                {
                    await DeleteMessage(Reaction.Message.Value);
                    Requests--;

                    await SendMessage("A successful vote (or admin vote) was passed to deny help request from " + Reaction.Message.Value.Author.Mention + ".\nThe help topic is: \n" + Reaction.Message.Value.Content, ClientInstance.Guilds.ToList()[0].Id, conf.LogChannel);
                }
                else if ((Reaction.Emote.Name.ToLowerInvariant() == conf.RequestBanEmote.ToLowerInvariant() &&
                    Reaction.Message.Value.Reactions[Reaction.Emote].ReactionCount - 1 >= conf.BanvotesForRemoval) ||
                    ((HasRole(conf.StaffRole, Reaction.UserId, ClientInstance.Guilds.ToList()[0].Id) ||
                    HasRole(conf.SupporterRole, Reaction.UserId, ClientInstance.Guilds.ToList()[0].Id)) &&
                    Reaction.Emote.Name.ToLowerInvariant() == conf.AdminBanEmote.ToLowerInvariant()))
                {
                    await DeleteMessage(Reaction.Message.Value);

                    Requests--;

                    var guild = ClientInstance.Guilds.ToList()[0];
                    await guild.GetUser(Reaction.Message.Value.Author.Id).AddRoleAsync(guild.GetRole(conf.RequestBannedRole));

                    await SendMessage("A successful vote (or admin vote) was passed to ban " + Reaction.Message.Value.Author.Mention + " from support requests.\nThe help topic is: \n" + Reaction.Message.Value.Content, ClientInstance.Guilds.ToList()[0].Id, conf.LogChannel);
                }
            }
            else if (Reaction.Channel.Id == conf.GeneralChannel)
            {
                if (Reaction.Emote.Name.ToLowerInvariant() == conf.RequestDownvoteEmote.ToLowerInvariant() &&
                       (HasRole(conf.StaffRole, Reaction.UserId, ClientInstance.Guilds.ToList()[0].Id) ||
                       HasRole(conf.SupporterRole, Reaction.UserId, ClientInstance.Guilds.ToList()[0].Id)))
                {
                    var learning = Learning.Load();

                    if (learning.NoHelp.Exists(k => CalculateSimilarity(Reaction.Message.Value.Content, k) > 0.80))
                        return;

                    learning.NoHelp.Add(Reaction.Message.Value.Content);
                    learning.SaveJson();

                    await SendMessage(Reaction.Message.Value.Author.Mention + ", please don't ask for help in this channel.\nWe do not provide help/support in this channel since it is meant for talking about general stuff.\n If you want help, go in <#" + conf.SupportChannel + "> and ask there for help after tagging the support role.\n\nThank you in advance.", ClientInstance.Guilds.ToList()[0].Id, Reaction.Message.Value.Channel.Id);
                    await SendMessage("New message added to anti-general-help list: \n" + Reaction.Message.Value.Content, ClientInstance.Guilds.ToList()[0].Id, conf.LogChannel);
                }
            }
        }
        private async Task HandleCommand(SocketCommandContext Context)
        {
            var executed = Context.Message.Content.Split(' ').ToList();
            var comm = executed[0];
            executed.Remove(comm);
            var arguments = CheckDoubleQuotes(executed);
            var config = Configuration.Load();

            if (Context.Message.Author.Id == config.OwnerID && comm.StartsWith("$"))
            {
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "shutdown":
                        await DeleteMessage(Context.Message);
                        await ClientInstance.LogoutAsync();
                        Environment.Exit(0);
                        break;
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
                        break;
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
                        break;
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
                        break;
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
                        break;
                }
            }
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
        private async Task<IUserMessage> SendDirectMessage(string Message, IUser User)
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
                case "!premium":
                    return true;
                default:
                    return false;
            }
        }
        private bool HasRole(ulong RoleID, ulong UserID, ulong GuildID) =>
            UserID == Configuration.Load().OwnerID ? true : (ClientInstance?.GetGuild(GuildID)?.GetUser(UserID)?.Roles?.ToList()?.Exists(k => k.Id == RoleID) ?? false);
        private bool IsSpam(SocketUserMessage msg, out string Reason)
        {
            var usr = msg?.MentionedUsers ?? new List<SocketUser>();
            var rol = msg?.MentionedRoles ?? new List<SocketRole>();
            var cha = msg?.MentionedChannels ?? new List<SocketGuildChannel>();
            
            foreach (var u in usr)
            {
                if (usr.Count(k => k.Id == u.Id) > 1)
                {
                    Reason = "Multiple user mention";
                    return true;
                }
            }
            foreach (var r in rol)
            {
                if (rol.Count(k => k.Id == r.Id) > 1)
                {
                    Reason = "Multiple role mention";
                    return true;
                }
            }
            foreach (var c in cha)
            {
                if (cha.Count(k => k.Id == c.Id) > 1)
                {
                    Reason = "Multiple channel mention";
                    return true;
                }
            }

            var s = Spam?.Find(k => k.ID == msg.Author.Id) ?? null;
            if (s != null)
            {
                s?.Messages?.RemoveAll(k => (ulong)DateTime.Now.Subtract(k.Added).TotalHours > 1);

                var m = s?.Messages?.Find(k => CalculateSimilarity(msg.Content.ToLowerInvariant(), k.Message.ToLowerInvariant()) == 1) ?? null;
                if (m != null)
                {
                    var sim = CalculateSimilarity(msg.Content.ToLowerInvariant(), m.Message.ToLowerInvariant());
                    Reason = $"Similar message with {m.Message} \nSimilarity: {sim} \nPosted at: {m.Added}";
                    return true;
                }

                var msgs = s?.Messages?.ToList() ?? new List<AntiSpamMsg>();
                msgs.RemoveAll(k => (ulong)DateTime.Now.Subtract(k.Added).TotalSeconds > 5);

                m = s?.Messages?.Find(k => CalculateSimilarity(msg.Content.ToLowerInvariant(), k.Message.ToLowerInvariant()) > 0.80) ?? null;
                if (m != null)
                {
                    var sim = CalculateSimilarity(msg.Content.ToLowerInvariant(), m.Message.ToLowerInvariant());
                    Reason = $"Similar message with {m.Message} \nSimilarity: {sim} \nPosted at: {m.Added}";
                    return true;
                }
            }
            else
                Spam.Add(new AntiSpam() {
                    ID = msg?.Author?.Id ?? 0, Messages = new List<AntiSpamMsg>() {
                        new AntiSpamMsg() {
                            Added = DateTime.Now, Message = msg?.Content?.ToLowerInvariant() ?? "" }
                    }
                });

            Reason = null;
            return false;
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
                    else if (s.EndsWith("\\\""))
                        result.Add(s.Remove(s.Length - 2));

                    if (Combined == "")
                        result.Add(s);
                }
            }
            catch { }
            return result;
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