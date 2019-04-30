using Discord;
using Discord.WebSocket;

namespace Persiafighter.Applications.Support_Bot
{
    public sealed class IssueChannel
    {
        public IMessageChannel Channel;
        public SocketGuildUser Owner;
        public string Issue;
    }
}