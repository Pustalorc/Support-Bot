using System;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;

// ReSharper disable EmptyEmbeddedStatement

namespace Persiafighter.Applications.Support_Bot
{
    public static class Utilities
    {
        public static IEnumerable<string> CheckDoubleQuotes(IEnumerable<string> items)
        {
            var result = new List<string>();
            try
            {
                var combined = "";
                foreach (var s in items)
                {
                    if (combined != "")
                        combined += " " + s;

                    if (s.StartsWith("\"", StringComparison.Ordinal))
                        combined += s.TrimStart('"');

                    if (s.EndsWith("\"", StringComparison.Ordinal) && !s.EndsWith("\\\"", StringComparison.Ordinal))
                    {
                        var t = combined.TrimEnd('"');
                        result.Add(t);
                        combined = "";
                        continue;
                    }

                    if (s.EndsWith("\\\"", StringComparison.Ordinal)) result.Add(s.Remove(s.Length - 2));

                    if (combined == "")
                        result.Add(s);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        public static double CalculateSimilarity(string source, string target)
        {
            if (source == null || target == null) return 0.0;
            if (source.Length == 0 || target.Length == 0) return 0.0;
            if (source == target) return 1.0;

            var stepsToSame = ComputeLevenshteinDistance(source, target);
            return 1.0 - stepsToSame / (double) Math.Max(source.Length, target.Length);
        }

        private static int ComputeLevenshteinDistance(string source, string target)
        {
            if (source == null || target == null) return 0;
            if (source.Length == 0 || target.Length == 0) return 0;
            if (source == target) return source.Length;

            var sourceWordCount = source.Length;
            var targetWordCount = target.Length;
            if (sourceWordCount == 0)
                return targetWordCount;
            if (targetWordCount == 0)
                return sourceWordCount;

            var distance = new int[sourceWordCount + 1, targetWordCount + 1];
            for (var i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
            for (var j = 0; j <= targetWordCount; distance[0, j] = j++) ;
            for (var i = 1; i <= sourceWordCount; i++)
            for (var j = 1; j <= targetWordCount; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }

            return distance[sourceWordCount, targetWordCount];
        }

        public static bool HasRole(DiscordSocketClient clientInstance, ulong roleId, ulong userId, ulong guildId)
        {
            return userId == Configuration.Load().OwnerId ||
                   (clientInstance?.GetGuild(guildId)?.GetUser(userId)?.Roles?.ToList().Exists(k => k.Id == roleId) ??
                    false);
        }

        public static bool IsCommand(string message)
        {
            switch (message.Split()[0].ToLowerInvariant())
            {
                case "$shutdown":
                case "$game":
                case "/status":
                case "/learningfile":
                case "/dellearning":
                case "!premium":
                case "!close":
                case "!resolve":
                    return true;
                default:
                    return false;
            }
        }

        public static SocketGuildUser GetUser(DiscordSocketClient clientInstance, string nameOrId, ulong guildId)
        {
            try
            {
                return clientInstance.GetGuild(guildId).Users.ToList().Find(k =>
                {
                    if (ulong.TryParse(nameOrId, out var uid))
                    {
                        bool? id = k?.Id == uid;
                        return id == true;
                    }

                    var username = k?.Username?.ToLowerInvariant().Contains(nameOrId.ToLowerInvariant());
                    var nickname = k?.Nickname?.ToLowerInvariant().Contains(nameOrId.ToLowerInvariant());
                    return username == true || nickname == true;
                });
            }
            catch
            {
                try
                {
                    return clientInstance.GetGuild(guildId).Users.ToList().Find(k =>
                    {
                        if (ulong.TryParse(nameOrId, out var uid))
                        {
                            bool? id = k?.Id == uid;
                            return id == true;
                        }

                        var username = k?.Username?.ToLowerInvariant().Contains(nameOrId.ToLowerInvariant());
                        var nickname = k?.Nickname?.ToLowerInvariant().Contains(nameOrId.ToLowerInvariant());
                        return username == true || nickname == true;
                    });
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}