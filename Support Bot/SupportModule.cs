using System;
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
        private readonly OverwritePermissions _readonly = new OverwritePermissions(PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Deny, PermValue.Allow, PermValue.Deny, PermValue.Deny, manageRoles: PermValue.Deny, manageWebhooks: PermValue.Deny);

        public SupportModule(DiscordSocketClient clientInstance)
        {
            _client = clientInstance;
        }

        public async void AnalyzeMessage(SocketCommandContext context)
        {
            var config = Configuration.Load();
            var guild = context.Guild;
            var author = Utilities.GetUser(_client, context.User.Id.ToString(), guild.Id);

            if (context.Channel.Id != config.SupportChannel)
                return;

            if (HasAnswer(context.Message))
            {
                await context.Message.DeleteAsync();
                await context.Channel.SendMessageAsync(
                    $"{author.Mention} that question has been previously asked! Please check the RESOLVED category for an answer. If you still need help with this, contact a supporter.");
                return;
            }

            if (_issues.Exists(k => k.Owner == author.Id))
            {
                var channel2 = _issues.Find(k => k.Owner == author.Id);
                await channel2.Channel.SendMessageAsync($"{author.Mention} says: {context.Message.Content}");
                await context.Message.DeleteAsync();
                return;
            }

            var cName = context.Message.Content.Length > 100
                ? context.Message.Content.Substring(0, 100)
                : context.Message.Content;
            var channel = await guild.CreateTextChannelAsync(cName);

            await channel.ModifyAsync(k =>
            {
                var channel2 = guild.GetTextChannel(context.Channel.Id);
                var msg = context.Message.Content;
                var cTopic = msg.Length > 1024 ? msg.Substring(0, 1024) : msg;
                k.Topic = cTopic;
                k.CategoryId = channel2.CategoryId;
                k.Position = channel2.Position + 1;
            });

            await channel.SendMessageAsync(
                $"<@&{config.SupporterRole}>, {author.Mention} needs your help with: {context.Message.Content}");

            if (context.Channel is SocketTextChannel a) await a.AddPermissionOverwriteAsync(context.Message.Author, _readonly);

            _issues.Add(new IssueChannel {Owner = author.Id, Channel = channel, Issue = context.Message.Content});

            await context.Message.DeleteAsync();
        }

        private static bool HasAnswer(IMessage contextMessage)
        {
            var learning = Learning.Load();
            return learning.PreviousHelp.Any(k => Utilities.CalculateSimilarity(k, contextMessage.Content) > .80);
        }

        public async void ResolveIssue(SocketCommandContext context)
        {
            if (!_issues.Exists(k => k.Channel.Id == context.Channel.Id))
                return;

            var issue = _issues.Find(k => k.Channel.Id == context.Channel.Id);

            var learning = Learning.Load();
            learning.PreviousHelp.Add(issue.Issue);
            learning.SaveJson();

            var theChannel = context.Guild.GetTextChannel(context.Channel.Id);
            await theChannel.AddPermissionOverwriteAsync(context.Guild.EveryoneRole, _readonly);

            var a = context.Guild.GetTextChannel(Configuration.Load().SupportChannel);
            if (a != null) await a.RemovePermissionOverwriteAsync(context.Message.Author);

            _issues.RemoveAll(k => k.Channel.Id == context.Channel.Id);

            await theChannel.ModifyAsync(k => k.Topic = "");

            var category = context.Guild.CategoryChannels.FirstOrDefault(l =>
                               string.Equals(l.Name, "RESOLVED", StringComparison.InvariantCultureIgnoreCase)) ??
                           (ICategoryChannel) await context.Guild.CreateCategoryChannelAsync("RESOLVED");

            var lastChannel = context.Guild.TextChannels.OrderBy(l => l.Position).LastOrDefault(l =>
            {
                if (l.CategoryId.HasValue) return l.CategoryId.Value == category.Id;

                return false;
            });

            await theChannel.ModifyAsync(k =>
            {
                k.CategoryId = category.Id;
                k.Position = (lastChannel?.Position ?? category.Position) + 1;
            });
        }

        public async void CloseIssue(SocketCommandContext context)
        {
            if (!_issues.Exists(k => k.Channel.Id == context.Channel.Id))
                return;

            var theChannel = context.Guild.GetTextChannel(context.Channel.Id);
            await theChannel.DeleteAsync();

            var a = context.Guild.GetTextChannel(Configuration.Load().SupportChannel);
            if (a != null) await a.RemovePermissionOverwriteAsync(context.Message.Author);

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