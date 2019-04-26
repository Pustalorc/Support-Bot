using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Persiafighter.Applications.Support_Bot
{
    public sealed class AntiSpamModule
    {
        private readonly DiscordSocketClient _client;
        private readonly List<AntiSpam> _spam = new List<AntiSpam>();
        private readonly List<ulong> _warnedSpammers = new List<ulong>();

        public AntiSpamModule(DiscordSocketClient client)
        {
            _client = client;
        }

        public bool IsSpam(SocketCommandContext contextMessage, out string reason)
        {
            reason = "None";
            var config = Configuration.Load();
            /*if (Utilities.HasRole(_client, config.SupporterRole, contextMessage.Message.Author.Id,
                    contextMessage.Guild.Id) || Utilities.HasRole(_client, config.StaffRole,
                    contextMessage.Message.Author.Id, contextMessage.Guild.Id))
                return false;*/

            var usr = contextMessage.Message.MentionedUsers ?? new List<SocketUser>();
            var rol = contextMessage.Message.MentionedRoles ?? new List<SocketRole>();
            var cha = contextMessage.Message.MentionedChannels ?? new List<SocketGuildChannel>();

            if (usr.Any(u => usr.Count(k => k.Id == u.Id) > 1))
            {
                reason = "Multiple user mention";
                return true;
            }

            if (rol.Any(r => rol.Count(k => k.Id == r.Id) > 1))
            {
                reason = "Multiple role mention";
                return true;
            }

            if (cha.Any(c => cha.Count(k => k.Id == c.Id) > 1))
            {
                reason = "Multiple channel mention";
                return true;
            }

            var s = _spam?.Find(k => k.Id == contextMessage.Message.Author.Id);
            if (s != null)
            {
                s.Messages?.RemoveAll(k => (ulong) DateTime.Now.Subtract(k.Added).TotalHours > 1);

                var m = s.Messages?.Find(k =>
                    Utilities.CalculateSimilarity(contextMessage.Message.Content.ToLowerInvariant(),
                        k.Message.ToLowerInvariant()) == 1);
                if (m != null)
                {
                    var sim = Utilities.CalculateSimilarity(contextMessage.Message.Content.ToLowerInvariant(),
                        m.Message.ToLowerInvariant());
                    reason = $"Similar message with {m.Message} \nSimilarity: {sim} \nPosted at: {m.Added}";
                    return true;
                }

                var messages = s.Messages?.ToList() ?? new List<AntiSpamMsg>();
                messages.RemoveAll(k => (ulong) DateTime.Now.Subtract(k.Added).TotalSeconds > 5);

                m = s.Messages?.Find(k =>
                    Utilities.CalculateSimilarity(contextMessage.Message.Content.ToLowerInvariant(),
                        k.Message.ToLowerInvariant()) > 0.80);
                if (m != null)
                {
                    var sim = Utilities.CalculateSimilarity(contextMessage.Message.Content.ToLowerInvariant(),
                        m.Message.ToLowerInvariant());
                    reason = $"Similar message with {m.Message} \nSimilarity: {sim} \nPosted at: {m.Added}";
                    return true;
                }
            }
            else
            {
                _spam?.Add(new AntiSpam
                {
                    Id = contextMessage.Message.Author.Id, Messages = new List<AntiSpamMsg>
                    {
                        new AntiSpamMsg
                        {
                            Added = DateTime.Now, Message = contextMessage.Message.Content.ToLowerInvariant()
                        }
                    }
                });
            }

            reason = null;
            return false;
        }

        public async void HandleSpam(SocketCommandContext contextMessage, string reason)
        {
            var config = Configuration.Load();

            if (_warnedSpammers.Exists(k => k == contextMessage.Message.Author.Id))
            {
                await contextMessage.Guild.AddBanAsync(contextMessage.Message.Author, 7, "Spamming. Anti-Spam ban.",
                    new RequestOptions {AuditLogReason = "Anti-spam ban"});
                await contextMessage.Channel.SendMessageAsync(
                    contextMessage.Message.Author.Mention + " banned for spamming.");
                await contextMessage.Guild.GetTextChannel(config.LogChannel).SendMessageAsync(
                    "Anti-spam triggered on " + contextMessage.Message.Author.Id + ".\nReason: " + (reason ?? "N/A") +
                    ".\nMessage sent: " + contextMessage.Message.Content +
                    ".\nMessage ID (For discord reports if needed):" + contextMessage.Message.Id +
                    ".\nAction: Banned.");

                _warnedSpammers.RemoveAll(k => k == contextMessage.Message.Author.Id);
            }
            else
            {
                await contextMessage.Channel.SendMessageAsync(
                    contextMessage.Message.Author.Mention +
                    " cease your spam immediately. Failure to do so will result in a ban." +
                    " This is your first **AND FINAL** warning.");
                await contextMessage.Guild.GetTextChannel(config.LogChannel).SendMessageAsync(
                    "Anti-spam triggered on " + contextMessage.Message.Author.Id + ".\nReason: " + (reason ?? "N/A") +
                    ".\nMessage sent: " + contextMessage.Message.Content +
                    ".\nMessage ID (For discord reports if needed):" + contextMessage.Message.Id +
                    ".\nAction: Warned.");

                _warnedSpammers.Add(contextMessage.Message.Author.Id);
            }
        }
    }
}