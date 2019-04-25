using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Persiafighter.Applications.Support_Bot
{
    public sealed class SupportModule
    {
        private readonly DiscordSocketClient _client;
        private readonly List<IssueChannel> _issues = new List<IssueChannel>();

        public SupportModule(DiscordSocketClient clientInstance)
        {
            _client = clientInstance;
        }

        public async void AnalyzeMessage(SocketCommandContext context)
        {
            var config = Configuration.Load();
            var guild = context.Guild;
            var readonlyRole = guild.GetRole(config.ReadOnlyRole);
            var author = Utilities.GetUser(_client, context.User.Id.ToString(), guild.Id);

            if (HasAnswer(context.Message))
            {
                await context.Message.DeleteAsync();
                await author.AddRoleAsync(readonlyRole);
                try
                {
                    await author.SendMessageAsync(
                        "You have been muted in the server for not reading previous existing information before asking." +
                        "\n If you want to regain the ability to talk, please contact a staff member and tell them you've learnt your lesson.");
                }
                catch
                {
                    // Ignore any exceptions
                }
            }
            else if (context.Channel.Id == config.SupportChannel)
            {
                if (_issues.Exists(k => k.Owner == author.Id))
                {
                    var channel2 = _issues.Find(k => k.Owner == author.Id);
                    await channel2.Channel.SendMessageAsync($"{author.Mention} says: {context.Message.Content}");
                    await context.Message.DeleteAsync();
                    return;
                }

                var channel = await guild.CreateTextChannelAsync(author.Nickname ?? author.Username);

                await channel.ModifyAsync(k =>
                {
                    var channel2 = guild.GetTextChannel(context.Channel.Id);
                    k.Topic = context.Message.Content;
                    k.CategoryId = channel2.CategoryId;
                    k.Position = channel2.Position + 1;
                });

                await channel.SendMessageAsync(
                    $"<@&{config.SupporterRole}>, {author.Mention} needs your help with: {context.Message.Content}");

                var a = context.Channel as SocketTextChannel;

                var modified = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow,
                    PermValue.Deny,
                    PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny,
                    PermValue.Deny, manageRoles: PermValue.Deny, manageWebhooks: PermValue.Deny);

                if (a != null) await a.AddPermissionOverwriteAsync(context.Message.Author, modified);

                _issues.Add(new IssueChannel {Owner = author.Id, Channel = channel});
            }
        }

        private static bool HasAnswer(IMessage contextMessage)
        {
            var learning = Learning.Load();
            foreach (var s in learning.PreviousHelp)
                if (Utilities.CalculateSimilarity(s, contextMessage.Content) > .80)
                    return true;

            return false;
        }

        public async void ResolveIssue(SocketCommandContext context)
        {
            if (!_issues.Exists(k => k.Channel.Id == context.Channel.Id))
                return;

            var theChannel = context.Guild.GetTextChannel(context.Channel.Id);

            var learning = Learning.Load();
            learning.PreviousHelp.Add(theChannel.Topic);
            learning.SaveJson();

            await theChannel.ModifyAsync(async k =>
            {
                var category = context.Guild.CategoryChannels.FirstOrDefault(l => l.Name == "RESOLVED") ??
                               (ICategoryChannel) await context.Guild.CreateCategoryChannelAsync("RESOLVED");

                var lastChannel = context.Guild.TextChannels.OrderBy(l => l.Position).LastOrDefault(l =>
                {
                    if (l.CategoryId.HasValue) return l.CategoryId.Value == category.Id;

                    return false;
                });

                k.Name = k.Topic;
                k.Topic = "";
                k.CategoryId = category.Id;
                k.Position = (lastChannel?.Position ?? category.Position) + 1;
            });

            var modified = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow,
                PermValue.Deny,
                PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny,
                PermValue.Deny, manageRoles: PermValue.Deny, manageWebhooks: PermValue.Deny);

            await theChannel.AddPermissionOverwriteAsync(context.Guild.EveryoneRole, modified);

            _issues.RemoveAll(k => k.Channel.Id == context.Channel.Id);
        }

        public async void CloseIssue(SocketCommandContext context)
        {
            if (!_issues.Exists(k => k.Channel.Id == context.Channel.Id))
                return;

            var theChannel = context.Guild.GetTextChannel(context.Channel.Id);
            await theChannel.DeleteAsync();

            _issues.RemoveAll(k => k.Channel.Id == context.Channel.Id);
        }

        public Task HandleChannelDelete(SocketChannel arg)
        {
            if (_issues.Exists(k => k.Channel.Id == arg.Id))
                _issues.RemoveAll(k => k.Channel.Id == arg.Id);

            return null;
        }
    }
}