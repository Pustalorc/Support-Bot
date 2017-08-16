using Discord.Commands;
using Discord.WebSocket;
using Persiafighter.Applications.Support_Bot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Persiafighter.Applications.Support_Bot
{
    public class CommandHandler
    {
        private List<Users> Users = new List<Users>();
        private DiscordSocketClient _client;
        private CommandService _cmds;
        private AdminCommands _admincmds = new AdminCommands();

        public async Task InstallAsync(DiscordSocketClient c)
        {
            try
            {
                _client = c;
                _cmds = new CommandService();

                await _cmds.AddModulesAsync(Assembly.GetEntryAssembly());

                _client.MessageReceived += HandleCommandAsync;
            }
            catch { }
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            try
            {
                var msg = s as SocketUserMessage;
                if (msg == null || msg.Author.IsBot)
                    return;
                
                var context = new SocketCommandContext(_client, msg);

                var author = msg.Author;
                var user = Users.Find(k => k.ID == author.Id);
                var content = msg.Content;

                if (_admincmds.IsAdmin(author.Id))
                {
                    var command = content.Split(' ').ToList();
                    var comm = command[0];
                    command.Remove(command[0]);
                    var arguments = command;
                    if (comm.StartsWith(Configuration.Load().Prefix))
                    {
                        switch (comm.ToLower().TrimStart(Configuration.Load().Prefix[0]))
                        {
                            case "adds":
                                _admincmds.CommandAddServer(context);
                                return;
                            case "addm":
                                var message = arguments.ToList();
                                message.RemoveRange(0, 1);
                                _admincmds.CommandAddMessage(context, arguments[0], String.Join(" ", message));
                                return;
                            case "addk":
                                _admincmds.CommandAddKeyword(context, arguments[0], arguments[1]);
                                return;
                            case "details":
                                _admincmds.CommandDetails(context);
                                return;
                            case "status":
                                _admincmds.CommandStatus(context);
                                return;
                            case "rems":
                                _admincmds.CommandRemoveServer(context);
                                return;
                            case "remk":
                            case "remm":
                                _admincmds.CommandRemoveMessage(context, arguments[0]);
                                return;
                            case "shutdown":
                                _admincmds.CommandShutdown(context);
                                return;
                            case "google":
                                _admincmds.CommandGoogle(context, String.Join(" ", arguments));
                                return;
                            case "rage":
                                _admincmds.CommandRage(context);
                                return;
                            case "mute":
                                _admincmds.CommandMute(context, arguments[0], Users);
                                return;
                            case "unmute":
                                _admincmds.CommandUnmute(context, arguments[0], Users);
                                return;
                            case "purge":
                                if (arguments.Count == 1)
                                    _admincmds.CommandPurge(context, arguments[0]);
                                else if (arguments.Count == 2)
                                    _admincmds.CommandPurge(context, arguments[0], arguments[1]);
                                else
                                    _admincmds.CommandPurge(context);
                                return;
                            case "udetails":
                                _admincmds.CommandUserDetails(context, arguments[0]);
                                return;
                            case "admin":
                                _admincmds.CommandAdmin(context, arguments[0]);
                                return;
                            case "unadmin":
                                _admincmds.CommandUnadmin(context, arguments[0]);
                                return;
                            case "ptoggle":
                                _admincmds.CommandTogglePastebinAnalyzing(context);
                                return;
                        }
                    }
                }

                if (content.Split().Length > 10 || (user != null && user.Spammer))
                    return;

                var server = Configuration.Load().Servers.Find(k => k.ServerID == context.Guild.Id);
                if (server != null)
                {
                    if (server.AnalyzePastebins)
                        if (content.ToLowerInvariant().Contains("https://pastebin.com/"))
                        {
                            var helper = new PastebinAnalyzerHelper(content.Split().First(k => k.Contains("https://pastebin.com/")));
                            if (helper.IsLogs())
                                helper.WriteAndExplainErrors(context);
                        }

                    foreach (var mess in server.Messages)
                        foreach (string keyword in mess.ValidKeyWords)
                            if (content.ToLowerInvariant().Contains(keyword))
                            {
                                if (!IsRepeating(author, mess.ValidKeyWords.ToArray()))
                                    await context.Channel.SendMessageAsync(mess.Message);

                                if (user == null)
                                    Users.Add(new Users(author.Id, new List<UserMessage>() { new UserMessage(msg.Content) }));
                                else
                                {
                                    user.Messages.Add(new UserMessage(msg.Content));
                                    if (user.Spammer)
                                        await context.Channel.SendMessageAsync(context.User.Mention + ", you have spammed too much and now are marked as a spammer. All your messages will be ignored by me.");
                                    else if (user.Repeated == 5)
                                        await context.Channel.SendMessageAsync(context.User.Mention + ", please stop repeating the same thing. This is your first and last warning.");
                                }
                                return;
                            }
                }
            }
            catch { }
        }

        private bool IsRepeating(SocketUser Author, string[] Keyword)
        {
            try
            {
                var user = Users.Find(k => k.ID == Author.Id);
                if (user != null)
                {
                    if (user.Spammer)
                        return true;
                    foreach (string s in Keyword)
                        foreach (var mess in user.Messages)
                            if (mess.Message.Contains(s) && DateTime.Now.Subtract(mess.Sent).TotalSeconds <= 30)
                            {
                                user.Repeated++;
                                if (user.Repeated >= 10)
                                    user.Spammer = true;
                                return true;
                            }
                }
            }
            catch { }
            return false;
        }
    }
}

namespace Persiafighter.Applications.Support_Bot.Classes
{
    public sealed class Users
    {
        public ulong ID = 0;
        public List<UserMessage> Messages = new List<UserMessage>();
        public byte Repeated = 0;
        public bool Spammer = false;

        public Users(ulong ID, List<UserMessage> messages = null)
        {
            this.ID = ID;
            Messages = messages ?? new List<UserMessage>();
        }
    }

    public sealed class UserMessage
    {
        public string Message;
        public DateTime Sent;

        public UserMessage(string Message)
        {
            this.Message = Message;
            Sent = DateTime.Now;
        }
    }
}
