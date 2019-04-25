using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

#pragma warning disable 4014

namespace Persiafighter.Applications.Support_Bot
{
    public sealed class SupportBot
    {
        private AntiSpamModule _antiSpam;
        private DiscordSocketClient _clientInstance;
        private SupportModule _support;

        private static void Main()
        {
            new SupportBot().StartAsync().GetAwaiter().GetResult();
        }

        private async Task StartAsync()
        {
            try
            {
                Configuration.EnsureExists();
                Learning.EnsureExists();

                _clientInstance = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose
                });

                _antiSpam = new AntiSpamModule(_clientInstance);
                _support = new SupportModule(_clientInstance);

                _clientInstance.Log += l => Console.Out.WriteLineAsync(l.ToString());
                _clientInstance.MessageReceived += async o =>
                    await HandleMessage(new SocketCommandContext(_clientInstance, o as SocketUserMessage));
                _clientInstance.ChannelDestroyed += _support.HandleChannelDelete;

                await _clientInstance.LoginAsync(TokenType.Bot, Configuration.Load().Token);
                await _clientInstance.StartAsync();
                await _clientInstance.SetGameAsync("Dealing with people who don't read.");

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        private async Task HandleMessage(SocketCommandContext context)
        {
            if (context?.Message.Author.IsBot != false || context.IsPrivate)
                return;

            if (Utilities.IsCommand(context.Message.Content))
            {
                await context.Message.DeleteAsync();
                await HandleCommand(context);
                return;
            }

            var config = Configuration.Load();

            if (_antiSpam.IsSpam(context, out var reason))
            {
                _antiSpam.HandleSpam(context, reason);
                return;
            }

            _support.AnalyzeMessage(context);
        }

        private async Task HandleCommand(SocketCommandContext context)
        {
            var executed = context.Message.Content.Split(' ').ToList();
            var comm = executed[0];
            executed.Remove(comm);
            var arguments = Utilities.CheckDoubleQuotes(executed);
            var config = Configuration.Load();

            if (context.Message.Author.Id == config.OwnerId && comm.StartsWith("$", StringComparison.Ordinal))
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "shutdown":
                        await _clientInstance.LogoutAsync();
                        Environment.Exit(0);
                        break;
                    case "game":
                        await _clientInstance.SetGameAsync(string.Join(" ", arguments));
                        break;
                }

            if (Utilities.HasRole(_clientInstance, config.StaffRole, context.Message.Author.Id, context.Guild.Id) &&
                comm.StartsWith("/", StringComparison.Ordinal))
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "status":
                        var d = DateTime.Now;
                        await _clientInstance.GetConnectionsAsync();
                        await context.Channel.SendMessageAsync(
                            "----------- Support bot V5.0 status report -----------\nPing: " +
                            (ulong) DateTime.Now.Subtract(d).TotalMilliseconds +
                            "ms.\nRunning on " + Environment.OSVersion +
                            ".\n----------- Support bot V5.0 status report -----------");
                        break;
                }

            if ((Utilities.HasRole(_clientInstance, config.StaffRole, context.Message.Author.Id, context.Guild.Id) ||
                 Utilities.HasRole(_clientInstance, config.SupporterRole, context.Message.Author.Id, context.Guild.Id)
                ) && comm.StartsWith("!", StringComparison.Ordinal))
                switch (comm.Substring(1).ToLowerInvariant())
                {
                    case "resolve":
                        _support.ResolveIssue(context);
                        break;
                    case "close":
                        _support.CloseIssue(context);
                        break;
                }
        }
    }
}