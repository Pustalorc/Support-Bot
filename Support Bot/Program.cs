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
using System.Management;
using System.Diagnostics;
using System.Net;

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

            await Task.Delay(-1);
        }
    }

    public class CommandHandler
    {
        private List<ulong> muted = new List<ulong>();
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
            if (msg == null || msg.Author.IsBot || muted.Contains(msg.Author.Id))
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
                        await msg.DeleteAsync();
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
                        await msg.DeleteAsync();
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
                        await msg.DeleteAsync();
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
                        await msg.DeleteAsync();
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
                    case "status":
                        var ping = DateTime.Now;
                        await msg.DeleteAsync();
                        await context.Channel.SendMessageAsync("----------- Support bot V1.2 status report -----------\nPing: " + (long)DateTime.Now.Subtract(ping).TotalMilliseconds + "ms.\nRunning on " + Environment.OSVersion + ".\nHave " + server.Messages.Count + " messages for this server.\n----------- Support bot V1.2 status report -----------");
                        return;
                    case "rems":
                        await msg.DeleteAsync();
                        if (server == null)
                            await context.Channel.SendMessageAsync("Wow m8, well done. Trying to remove something that is not even added :rolling_eyes:");
                        else
                        {
                            config.Servers.Remove(server);
                            await context.Channel.SendMessageAsync("Removed m8. I am finally free from the server.");
                        }
                        config.SaveJson();
                        return;
                    case "remk":
                    case "remm":
                        await msg.DeleteAsync();
                        if (server == null)
                            await context.Channel.SendMessageAsync("m8, this server is not even added. What are you trying to do? (>_>)");
                        else
                        {
                            var mess = server.Messages.Find(k => k.ValidKeyWords.Contains(command[1]));
                            if (mess == null)
                                await context.Channel.SendMessageAsync("I feel ashamed of you m8. You keep doing the same mistake!");
                            else
                            {
                                server.Messages.Remove(mess);
                                await context.Channel.SendMessageAsync("Removed the message... Hope it was what you wanted, because you cant go back now.");
                            }
                        }
                        config.SaveJson();
                        return;
                    case "shutdown":
                        await msg.DeleteAsync();
                        await context.Channel.SendMessageAsync("Shutting down. Bye.");
                        Environment.Exit(0);
                        return;
                    case "google":
                        await msg.DeleteAsync();
                        var search = command.ToList();
                        search.Remove(command[0]);
                        var url = string.Format("https://www.google.es/search?q={0}", WebUtility.UrlEncode(string.Join(" ", search)));
                        HttpWebRequest req = WebRequest.CreateHttp(url);
                        await context.Channel.SendMessageAsync("Result: " + req.GetResponse().ResponseUri);
                        return;
                    case "rage":
                        await msg.DeleteAsync();
                        await context.Channel.SendFileAsync(@"C:\Users\vpastor\Downloads\REEEE.gif", "REEEEEEEEEEE");
                        return;
                    case "mute":
                        await msg.DeleteAsync();
                        var user = context.Guild.Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(command[1].ToLowerInvariant()));
                        if (user == null && ulong.TryParse(command[1], out ulong ID))
                            user = context.Guild.GetUser(ID);
                        if (user != null)
                        {
                            muted.Add(user.Id);
                            await context.Channel.SendMessageAsync("All of " + user.Mention + "'s messages will now be ignored.");
                        }
                        else
                            await context.Channel.SendMessageAsync("User not found, you sure that's a user in this server? :thinking:");
                        return;
                    case "unmute":
                        await msg.DeleteAsync();
                        user = context.Guild.Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(command[1].ToLowerInvariant()));
                        if (user == null && ulong.TryParse(command[1], out ID))
                            user = context.Guild.GetUser(ID);
                        if (user != null)
                        {
                            if (!muted.Contains(user.Id))
                                await context.Channel.SendMessageAsync("User is not muted, what are you doing?");
                            else
                            {
                                muted.Remove(user.Id);
                                await context.Channel.SendMessageAsync(user.Mention + "'s messages will not be ignored anymore.");
                            }
                        }
                        else
                            await context.Channel.SendMessageAsync("User not found, you sure that's a user in this server? :thinking:");
                        return;
                    case "purge":
                        await msg.DeleteAsync();
                        int deleted = 0;
                        if (command.Length > 1)
                        {
                            user = context.Guild.Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(command[1].ToLowerInvariant()));
                            if (user == null && ulong.TryParse(command[1], out ID))
                                user = context.Guild.GetUser(ID);
                            if (user != null)
                            {
                                var msgs = await context.Channel.GetMessagesAsync().Flatten();
                                var item = msgs.Where(k => k.Author.Id == user.Id);
                                await context.Channel.DeleteMessagesAsync(item);
                                deleted = item.Count();
                            }
                        }
                        else
                        {
                            var msgs = await context.Channel.GetMessagesAsync().Flatten();
                            await context.Channel.DeleteMessagesAsync(msgs);
                            deleted = msgs.Count();
                        }
                        var botms = await context.Channel.SendMessageAsync("Deleted " + deleted + " messages.");
                        await Task.Delay(5000);
                        await botms.DeleteAsync();
                        return;
                    case "udetails":
                        await msg.DeleteAsync();
                        user = context.Guild.Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(command[1].ToLowerInvariant()));
                        if (user == null && ulong.TryParse(command[1], out ID))
                            user = context.Guild.GetUser(ID);
                        if (user != null)
                            await context.Channel.SendMessageAsync(user.Mention + "'s details:\nUsername - " + user.Username + "\nNickname - " + ((user.Nickname == string.Empty || user.Nickname == null) ? "N/A" : user.Nickname) + "\nID - " + user.Id + "\nStatus - " + user.Status + "\nCustom Status/Playing - " + (user.Game.HasValue ? user.Game.Value.Name : "N/A") + "\nCreated - " + user.CreatedAt + "\nJoined - " + user.JoinedAt);
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
