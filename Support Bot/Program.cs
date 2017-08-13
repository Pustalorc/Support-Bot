using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Discord.Commands;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;

namespace Support_Bot
{
    public class Program
    {
        static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandHandler _commands;

        public async Task StartAsync()
        {
            Configuration.EnsureExists();
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000
            });

            _client.Log += (l)
                => Console.Out.WriteLineAsync(l.ToString());

            await _client.LoginAsync(TokenType.Bot, Configuration.Load().Token);
            await _client.StartAsync();

            _commands = new CommandHandler();
            await _commands.InstallAsync(_client);

            await _client.SetGameAsync("by helping people. (^_^)");
            _client.UserBanned += _client_UserBanned;

            await Task.Delay(-1);
        }

        private async Task _client_UserBanned(SocketUser user, SocketGuild server)
        {
            var chan = server.TextChannels.First();
            await chan.SendMessageAsync(user.Mention + " has been banned from the server!");
            
        }
    }

    public class CommandHandler
    {
        private Dictionary<ulong, string> pplMSG = new Dictionary<ulong, string>();
        private DiscordSocketClient _client;
        private CommandService _cmds;

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
            if (msg == null || msg.Author.IsBot /*|| msg.Author.Id == 181418012400812032*/)
                return;
            
            var context = new SocketCommandContext(_client, msg);
            
            var author = msg.Author;
            var content = msg.Content;
            var config = Configuration.Load();
            var server = config.Servers.Find(k => k.ServerID == context.Guild.Id);

            if (author.Id == 181418012400812032 || author.Id == 236451517257875456)
            {
                var command = content.Split(' ');
                switch (command[0].ToLower())
                {
                    case "adds":
                        if (server == null)
                        {
                            config.Servers.Add(new ServerSampleMessages() { ServerID = context.Guild.Id });
                            await context.Channel.SendMessageAsync("Added m8. Be happy I dont slap you.");
                        }
                        else
                            await context.Channel.SendMessageAsync("Wot u doing m8? Already added ye fatty.");
                        config.SaveJson();
                        return;
                    case "addm":
                        if (server != null)
                        {
                            var messig = server.Messages.Find(k => k.ValidKeyWords.Contains(command[1]));
                            if (messig == null)
                            {
                                var meg = command.ToList();
                                meg.Remove(command[0]);
                                meg.Remove(command[1]);
                                server.Messages.Add(new SampleMessages() { ValidKeyWords = new List<string>() { command[1] }, Message = string.Join(" ", meg) });
                                await context.Channel.SendMessageAsync("Added m8. You better watch yourself.");
                            }
                            else
                                await context.Channel.SendMessageAsync("m8, for the love of god, IT ALREADY EXISTS.");
                        }
                        else
                            await context.Channel.SendMessageAsync("m8, did u even add the server first? ADD IT!");
                        config.SaveJson();
                        return;
                    case "addk":
                        if (server != null)
                        {
                            var messig = server.Messages.Find(k => k.ValidKeyWords.Contains(command[1]));
                            if (messig == null)
                                await context.Channel.SendMessageAsync("-.- why do you do this m8? You didn't even add a message first!");
                            else
                            {
                                if (messig.ValidKeyWords.Contains(command[2]))
                                    await context.Channel.SendFileAsync(@"C:\Users\vpastor\Downloads\REEEE.gif", "REEEEEEEEEEE");
                                else
                                {
                                    messig.ValidKeyWords.Add(command[2]);
                                    await context.Channel.SendMessageAsync("Added, now please step off before the mine blows up.");
                                }
                            }
                        }
                        else
                            await context.Channel.SendMessageAsync("m8, did u even add the server first? ADD IT!");
                        config.SaveJson();
                        return;
                    case "details":
                        if (server != null)
                        {
                            if (server.Messages.Count == 0)
                            {
                                await context.Channel.SendMessageAsync(author.Mention + ", I can't send you details of a server that doesn't have any details. (>_>)");
                            }
                            else
                            {
                                await context.Channel.SendMessageAsync(author.Mention + ", sent you in dms the details.");
                                await author.SendMessageAsync(server.ServerID + "'s messages: ");
                                foreach (var ms in server.Messages)
                                {
                                    string keywords = "";
                                    foreach (string key in ms.ValidKeyWords)
                                    {
                                        if (key == ms.ValidKeyWords.Last())
                                        {
                                            keywords += key + ".";
                                            break;
                                        }
                                        keywords += key + ", ";
                                    }
                                    await author.SendMessageAsync("Keywords: " + keywords + " Message: " + ms.Message);
                                }
                            }
                        }
                        else
                            await author.SendMessageAsync("The server is not registered and has no messages :thinking:");
                        return;
                }
            }
            
            if (content.Split().Length > 50)
                return;

            if (server != null)
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
            /*if (msg.Content.ToLowerInvariant().Contains("portforward"))
            {
                if (!IsRepeating(msg.Author, context.Channel, new string[] { "portforward" }))
                    await context.Channel.SendMessageAsync("Portforwarding differs between routers. Please google how to portforward for your specific router. Please also remember unturned uses **ONLY** the 2 ports above the one you chose. So if you chose port 27015, forward ports 27016 and 27017. (UDP Protocol)");
            }
            else if (msg.Content.ToLowerInvariant().Contains("upnp"))
            {
                if (!IsRepeating(msg.Author, context.Channel, new string[] { "upnp" }))
                    await context.Channel.SendMessageAsync("UPnP is unstable in USO. If it does not work for you please restart USO or make sure UPnP is enabled in your router. If it still doesn't work, please keep trying or manually portforward.");
            }
            else if (msg.Content.ToLowerInvariant().Contains("help") || msg.Content.ToLowerInvariant().Contains("not working"))
            {
                if (!IsRepeating(msg.Author, context.Channel, new string[] { "help", "not working" }))
                    await context.Channel.SendMessageAsync("Please be more specific. We cannot help you if you are not specific on what your issue is.");
            }*/
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

    public enum AccessLevel
    {
        Blocked,
        User,
        ServerMod,
        ServerAdmin,
        ServerOwner,
        BotOwner
    }

    public class Configuration
    {
        [JsonIgnore]
        public static string FileName { get; private set; } = "config/configuration.json";
        public string Prefix { get; set; } = "!";
        public string Token { get; set; } = "";
        public List<ServerSampleMessages> Servers { get; set; } = new List<ServerSampleMessages>();

        public static void EnsureExists()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(file))
            {
                string path = Path.GetDirectoryName(file);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var config = new Configuration();

                Console.WriteLine("Please enter your token: ");
                string token = Console.ReadLine();

                config.Token = token;
                config.SaveJson();
            }
            Console.WriteLine("Configuration Loaded");
        }

        /// <summary> Save the configuration to the path specified in FileName. </summary>
        public void SaveJson()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            File.WriteAllText(file, ToJson());
        }

        /// <summary> Load the configuration from the path specified in FileName. </summary>
        public static Configuration Load()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(file));
        }

        /// <summary> Convert the configuration to a json string. </summary>
        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class MinPermissionsAttribute : PreconditionAttribute
    {
        private AccessLevel Level;

        public MinPermissionsAttribute(AccessLevel level)
        {
            Level = level;
        }

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var access = GetPermission(context);

            if (access >= Level)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError("Insufficient permissions."));
        }

        public AccessLevel GetPermission(ICommandContext c)
        {
            if (c.User.IsBot)
                return AccessLevel.Blocked;

            if (c.User.Id == 181418012400812032)
                return AccessLevel.BotOwner;

            var user = (SocketGuildUser)c.User;
            if (user != null)
            {
                if (c.Guild.OwnerId == user.Id)
                    return AccessLevel.ServerOwner;

                if (user.GuildPermissions.Administrator)
                    return AccessLevel.ServerAdmin;

                if (user.GuildPermissions.ManageMessages ||
                    user.GuildPermissions.BanMembers ||
                    user.GuildPermissions.KickMembers)
                    return AccessLevel.ServerMod;
            }

            return AccessLevel.User;
        }
    }

    public class ServerSampleMessages
    {
        public ulong ServerID { get; set; } = 0;
        public List<SampleMessages> Messages { get; set; } = new List<SampleMessages>();
    }

    public class SampleMessages
    {
        public List<string> ValidKeyWords { get; set; } = new List<string>();
        public string Message { get; set; } = "";
    }
}
