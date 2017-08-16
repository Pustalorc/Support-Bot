using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Persiafighter.Applications.Support_Bot
{
    public class Program
    {
        static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandHandler _commands;

        public async Task StartAsync()
        {
            try
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
            catch { }
        }
    }
}
