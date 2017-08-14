using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Persiafighter.Applications.Support_Bot
{
    public class CommandHandler
    {
        private List<ulong> muted = new List<ulong>();
        private Dictionary<ulong, string> pplMSG = new Dictionary<ulong, string>();
        private DiscordSocketClient _client;
        private CommandService _cmds;
        private AdminCommands _admincmds = new AdminCommands();

        public async Task InstallAsync(DiscordSocketClient c)
        {
            _client = c;
            _cmds = new CommandService();

            await _cmds.AddModulesAsync(Assembly.GetEntryAssembly());

            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null || msg.Author.IsBot || muted.Contains(msg.Author.Id))
                return;

            var context = new SocketCommandContext(_client, msg);
            
            var author = msg.Author;
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
                            _admincmds.CommandMute(context, arguments[0], muted);
                            return;
                        case "unmute":
                            _admincmds.CommandUnmute(context, arguments[0], muted);
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

            if (content.Split().Length > 10)
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
                            if (!IsRepeating(author, context.Channel, mess.ValidKeyWords.ToArray()))
                                await context.Channel.SendMessageAsync(mess.Message);
                            if (!pplMSG.ContainsKey(msg.Author.Id))
                                pplMSG.Add(msg.Author.Id, msg.Content);
                            else
                                pplMSG[msg.Author.Id] = msg.Content;
                            return;
                        }
            }
        }

        private bool IsRepeating(SocketUser Author, ISocketMessageChannel Channel, string[] Keyword)
        {
            if (pplMSG.ContainsKey(Author.Id))
            {
                foreach (string s in Keyword)
                    if (pplMSG[Author.Id].Contains(s))
                    {
                        return true;
                    }
            }
            return false;
        }
    }
}
