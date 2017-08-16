using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Persiafighter.Applications.Support_Bot.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Persiafighter.Applications.Support_Bot
{
    public sealed class AdminCommands
    {
        private List<ulong> Owners = new List<ulong>() { 181418012400812032, 236451517257875456 };

        public async void CommandAddServer(SocketCommandContext context)
        {
            try
            {
                var config = Configuration.Load();
                var server = config.Servers.Find(k => k.ServerID == context.Guild.Id);
                await context.Message.DeleteAsync();
                if (server == null)
                {
                    config.Servers.Add(new ServerSampleMessages() { ServerID = context.Guild.Id });
                    await context.Channel.SendMessageAsync("Added m8. Be happy I dont slap you.");
                }
                else
                    await context.Channel.SendMessageAsync("Wot u doing m8? Already added ye fatty.");
                config.SaveJson();
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandAddMessage(SocketCommandContext context, string Keyword, string Message)
        {
            try
            {
                var config = Configuration.Load();
                var server = config.Servers.Find(k => k.ServerID == context.Guild.Id);
                await context.Message.DeleteAsync();
                if (server != null)
                {
                    var messig = server.Messages.Find(k => k.ValidKeyWords.Contains(Keyword));
                    if (messig == null)
                    {
                        server.Messages.Add(new SampleMessages() { ValidKeyWords = new List<string>() { Keyword }, Message = Message });
                        await context.Channel.SendMessageAsync("Added m8. You better watch yourself.");
                    }
                    else
                        await context.Channel.SendMessageAsync("m8, for the love of god, IT ALREADY EXISTS.");
                }
                else
                    await context.Channel.SendMessageAsync("m8, did u even add the server first? ADD IT!");
                config.SaveJson();
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandAddKeyword(SocketCommandContext context, string BaseKeyword, string NewKeyword)
        {
            try
            {
                var config = Configuration.Load();
                var server = config.Servers.Find(k => k.ServerID == context.Guild.Id);
                await context.Message.DeleteAsync();
                if (server != null)
                {
                    var messig = server.Messages.Find(k => k.ValidKeyWords.Contains(BaseKeyword));
                    if (messig == null)
                        await context.Channel.SendMessageAsync("-.- why do you do this m8? You didn't even add a message first!");
                    else
                    {
                        if (messig.ValidKeyWords.Contains(NewKeyword))
                            await context.Channel.SendFileAsync(@"C:\Users\vpastor\Downloads\REEEE.gif", "REEEEEEEEEEE");
                        else
                        {
                            messig.ValidKeyWords.Add(NewKeyword);
                            await context.Channel.SendMessageAsync("Added, now please step off before the mine blows up.");
                        }
                    }
                }
                else
                    await context.Channel.SendMessageAsync("m8, did u even add the server first? ADD IT!");
                config.SaveJson();
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandDetails(SocketCommandContext context)
        {
            try
            {
                var config = Configuration.Load();
                var server = config.Servers.Find(k => k.ServerID == context.Guild.Id);
                var author = context.Message.Author;
                await context.Message.DeleteAsync();
                if (server != null)
                {
                    if (server.Messages.Count == 0)
                    {
                        await context.Channel.SendMessageAsync(author.Mention + ", I can't send you details of a server that doesn't have any details. (>_>)");
                    }
                    else
                    {
                        await context.Channel.SendMessageAsync(author.Mention + ", sent you in dms the details.");
                        string DM = "";
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
                            DM += " Keywords: " + keywords + " Message: " + ms.Message;
                        }

                        string[] words = DM.Split(' ');
                        List<string> lines = new List<string>();
                        string currentLine = server.ServerID + "'s messages: ";
                        int maxLength = 2000;
                        foreach (var currentWord in words)
                        {
                            if ((currentLine.Length > maxLength) || ((currentLine.Length + currentWord.Length) > maxLength))
                            {
                                lines.Add(currentLine);
                                currentLine = "";
                            }

                            if (currentLine.Length > 0)
                            {
                                if (currentWord == "Keywords:")
                                    currentLine += "\n\n" + currentWord;
                                else if (currentWord == "Message:")
                                    currentLine += "\n" + currentWord;
                                else
                                    currentLine += " " + currentWord;
                            }
                            else
                                currentLine += currentWord;

                            if (currentWord == words.Last())
                                lines.Add(currentLine);
                        }
                        foreach (string line in lines)
                            await author.SendMessageAsync(line);
                    }
                }
                else
                    await context.Channel.SendMessageAsync("The server is not registered and has no messages :thinking:");
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandStatus(SocketCommandContext context)
        {
            try
            {
                var config = Configuration.Load();
                var server = config.Servers.Find(k => k.ServerID == context.Guild.Id);
                var ping = DateTime.Now;
                await context.Message.DeleteAsync();
                await context.Channel.SendMessageAsync("----------- Support bot V1.4 status report -----------\nPing: " + (long)DateTime.Now.Subtract(ping).TotalMilliseconds + "ms.\nRunning on " + Environment.OSVersion + ".\nHave " + server.Messages.Count + " messages for this server.\n----------- Support bot V1.4 status report -----------");
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandRemoveServer(SocketCommandContext context)
        {
            try
            {
                var config = Configuration.Load();
                var server = config.Servers.Find(k => k.ServerID == context.Guild.Id);
                await context.Message.DeleteAsync();
                if (server == null)
                    await context.Channel.SendMessageAsync("Wow m8, well done. Trying to remove something that is not even added :rolling_eyes:");
                else
                {
                    config.Servers.Remove(server);
                    await context.Channel.SendMessageAsync("Removed m8. I am finally free from the server.");
                }
                config.SaveJson();
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandRemoveMessage(SocketCommandContext context, string KeyWord)
        {
            try
            {
                var config = Configuration.Load();
                var server = config.Servers.Find(k => k.ServerID == context.Guild.Id);
                await context.Message.DeleteAsync();
                if (server == null)
                    await context.Channel.SendMessageAsync("m8, this server is not even added. What are you trying to do? (>_>)");
                else
                {
                    var mess = server.Messages.Find(k => k.ValidKeyWords.Contains(KeyWord));
                    if (mess == null)
                        await context.Channel.SendMessageAsync("I feel ashamed of you m8. You keep doing the same mistake!");
                    else
                    {
                        server.Messages.Remove(mess);
                        await context.Channel.SendMessageAsync("Removed the message... Hope it was what you wanted, because you cant go back now.");
                    }
                }
                config.SaveJson();
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandShutdown(SocketCommandContext context)
        {
            try
            {
                await context.Message.DeleteAsync();
                await context.Channel.SendMessageAsync("Shutting down. Bye.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandGoogle(SocketCommandContext context, string Search)
        {
            try
            {
                await context.Message.DeleteAsync();
                var url = string.Format("https://www.google.es/search?q={0}", WebUtility.UrlEncode(Search));
                HttpWebRequest req = WebRequest.CreateHttp(url);
                await context.Channel.SendMessageAsync("Result: " + req.GetResponse().ResponseUri);
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandRage(SocketCommandContext context)
        {
            try
            {
                await context.Message.DeleteAsync();
                if (!File.Exists("rage.gif"))
                    using (var net = new WebClient())
                        net.DownloadFile("http://i0.kym-cdn.com/photos/images/newsfeed/000/915/652/b49.gif", "rage.gif");
                await context.Channel.SendFileAsync("rage.gif", "REEEEEEEEEEE");
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }
        
        public async void CommandMute(SocketCommandContext context, string User, List<Users> All)
        {
            try
            {
                await context.Message.DeleteAsync();
                var user = context.Guild.Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(User.ToLowerInvariant()));
                if (user == null && ulong.TryParse(User, out ulong ID))
                    user = context.Guild.GetUser(ID);
                if (user != null)
                {
                    var muted = All.Find(k => k.ID == user.Id);
                    if (muted != null)
                        muted.Spammer = true;
                    else
                        All.Add(new Users(user.Id) { Spammer = true });
                    await context.Channel.SendMessageAsync("All of " + user.Mention + "'s messages will now be ignored.");
                }
                else
                    await context.Channel.SendMessageAsync("User not found, you sure that's a user in this server? :thinking:");
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandUnmute(SocketCommandContext context, string name, List<Users> All)
        {
            try
            {
                await context.Message.DeleteAsync();
                var user = context.Guild.Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(name.ToLowerInvariant()));
                if (user == null && ulong.TryParse(name, out ulong ID))
                    user = context.Guild.GetUser(ID);
                if (user != null)
                {
                    var muted = All.Find(k => k.ID == user.Id);
                    if (muted != null)
                    {
                        if (!muted.Spammer)
                            await context.Channel.SendMessageAsync("User is not muted, what are you doing?");
                        else
                        {
                            muted.Spammer = false;
                            await context.Channel.SendMessageAsync(user.Mention + "'s messages will not be ignored anymore.");
                        }
                    }
                    else
                        await context.Channel.SendMessageAsync(user.Username + " is not muted, what are you trying to do?");
                }
                else
                    await context.Channel.SendMessageAsync("User not found, you sure that's a user in this server? :thinking:");
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandPurge(SocketCommandContext context, string User = null, string Ammount = null)
        {
            try
            {
                await context.Message.DeleteAsync();
                int deleted = 0;
                if (User != null)
                {
                    bool isnumber = ulong.TryParse(User, out ulong ID);
                    if (isnumber)
                    {
                        var us = context.Guild.Users.ToList().Find(k => k.Id == ID);
                        if (us == null)
                        {
                            var msgs = await context.Channel.GetMessagesAsync((int)ID).Flatten();
                            await context.Channel.DeleteMessagesAsync(msgs);
                            deleted = msgs.Count();
                        }
                        else
                        {
                            var msgs = await context.Channel.GetMessagesAsync(Ammount != null ? int.Parse(Ammount) : 100).Flatten();
                            var item = msgs.Where(k => k.Author.Id == us.Id);
                            await context.Channel.DeleteMessagesAsync(item);
                            deleted = item.Count();
                        }
                    }
                    else if (!isnumber)
                    {
                        var us = context.Guild.Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(User.ToLowerInvariant()));
                        var msgs = await context.Channel.GetMessagesAsync(Ammount != null ? int.Parse(Ammount) : 100).Flatten();
                        var item = msgs.Where(k => k.Author.Id == us.Id);
                        await context.Channel.DeleteMessagesAsync(item);
                        deleted = item.Count();
                    }
                }
                else
                {
                    var msgs = await context.Channel.GetMessagesAsync(100).Flatten();
                    await context.Channel.DeleteMessagesAsync(msgs);
                    deleted = msgs.Count();
                }
                var botms = await context.Channel.SendMessageAsync("Deleted " + deleted + " messages.");
                await Task.Delay(5000);
                try
                {
                    await botms.DeleteAsync();
                }
                catch { }
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandUserDetails(SocketCommandContext context, string User)
        {
            try
            {
                await context.Message.DeleteAsync();
                var user = context.Guild.Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(User.ToLowerInvariant()));
                if (user == null && ulong.TryParse(User, out ulong ID))
                    user = context.Guild.GetUser(ID);
                if (user != null)
                    await context.Channel.SendMessageAsync(user.Mention + "'s details:\nUsername - " + user.Username + "\nNickname - " + ((user.Nickname == string.Empty || user.Nickname == null) ? "N/A" : user.Nickname) + "\nID - " + user.Id + "\nStatus - " + user.Status + "\nCustom Status/Playing - " + (user.Game.HasValue ? user.Game.Value.Name : "N/A") + "\nCreated - " + user.CreatedAt + "\nJoined - " + user.JoinedAt);
                else
                    await context.Channel.SendMessageAsync("User not found, did you even put a user? :thinking:");
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandAdmin(SocketCommandContext context, string User)
        {
            try
            {
                var config = Configuration.Load();
                await context.Message.DeleteAsync();
                var user = context.Guild.Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(User.ToLowerInvariant()));
                if (user == null && ulong.TryParse(User, out ulong ID))
                    user = context.Guild.GetUser(ID);
                if (user != null)
                {
                    if (config.Admins.Contains(user.Id))
                        await context.Channel.SendMessageAsync("m8, how many times do I have to tell you? -.- It already exists!");
                    else
                    {
                        config.Admins.Add(user.Id);
                        config.SaveJson();
                        await context.Channel.SendMessageAsync(user.Mention + " is now an admin and can use all the admin commands. Hope he doesn't screw with the bot.");
                    }
                }
                else
                    await context.Channel.SendMessageAsync("User not found, did you even put a user? :thinking:");
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandUnadmin(SocketCommandContext context, string User)
        {
            try
            {
                var config = Configuration.Load();
                await context.Message.DeleteAsync();
                var user = context.Guild.Users.ToList().Find(k => k.Username.ToLowerInvariant().Contains(User.ToLowerInvariant()));
                if (user == null && ulong.TryParse(User, out ulong ID))
                    user = context.Guild.GetUser(ID);
                if (user != null)
                {
                    if (!config.Admins.Contains(user.Id))
                        await context.Channel.SendMessageAsync("OMG. I am SOOO done with you giving me things that cannot be done.");
                    else
                    {
                        config.Admins.Remove(user.Id);
                        config.SaveJson();
                        await context.Channel.SendMessageAsync(user.Mention + " is no longer an admin. :thinking: you happy?");
                    }
                }
                else
                    await context.Channel.SendMessageAsync("m8, that's not a user in this server.");
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public async void CommandTogglePastebinAnalyzing(SocketCommandContext context)
        {
            try
            {
                var config = Configuration.Load();
                await context.Message.DeleteAsync();
                var server = config.Servers.Find(k => k.ServerID == context.Guild.Id);
                if (server != null)
                {
                    server.AnalyzePastebins = !server.AnalyzePastebins;
                    config.SaveJson();
                    await context.Channel.SendMessageAsync("Toggled analyzing pastebins to: " + (server.AnalyzePastebins ? "TRUE" : "FALSE"));
                }
                else
                    await context.Channel.SendMessageAsync("This server is not registered m8.");
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message); Console.WriteLine(ex);
            }
        }

        public bool IsAdmin(ulong ID)
        {
            try
            {
                bool owner = Owners.Contains(ID);
                if (owner)
                    return true;
                else
                    return Configuration.Load().Admins.Contains(ID);
            }
            catch { return false; }
        }
    }
}
