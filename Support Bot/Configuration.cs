using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Pustalorc.Applications.SupportBot_
{
    public sealed class Configuration
    {
        [JsonIgnore]
        public static string FileName { get; private set; } = "config/configuration.json";
        public string Token { get; set; } = Guid.Empty.ToString();
        public string RequestAcceptEmote { get; set; } = "👍";
        public string RequestDownvoteEmote { get; set; } = "👎";
        public string RequestBanEmote { get; set; } = "⛔";
        public string AdminBanEmote { get; set; } = "💢";
        public string AdminDenyEmote { get; set; } = "💂";
        public byte BanvotesForRemoval { get; set; } = 10;
        public byte DownvotesForDenial { get; set; } = 5;
        public bool SupporterAntiTagBypass { get; set; } = true;
        public ulong SupportRequestClearMilliseconds { get; set; } = 14400000;
        public ulong OwnerID { get; set; } = 0;
        public ulong GeneralChannel { get; set; } = 0;
        public ulong SupportChannel { get; set; } = 0;
        public ulong SupportRequestsChannel { get; set; } = 0;
        public ulong LogChannel { get; set; } = 0;
        public ulong RequestBannedRole { get; set; } = 0;
        public ulong SupporterRole { get; set; } = 0;
        public ulong StaffRole { get; set; } = 0;

        public void SaveJson()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            File.WriteAllText(file, ToJson());
        }
        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
        public static bool EnsureExists()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(file))
            {
                string path = Path.GetDirectoryName(file);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var config = new Configuration();

                Console.WriteLine("Please enter your discord bot token: ");
                config.Token = Console.ReadLine();
                Console.WriteLine("Please enter the name of the emote (if custom, the unicode otherwise) to be used to accept support requests: ");

                var s = Console.ReadLine();
                try
                {
                    var hexVal = Convert.ToInt32(s, 16);
                    var strVal = Char.ConvertFromUtf32(hexVal);

                    config.RequestAcceptEmote = strVal;
                }
                catch
                {
                    config.RequestAcceptEmote = s;
                }

                Console.WriteLine("Please enter the name of the emote (if custom, the unicode otherwise) to be used to decline support requests: ");

                s = Console.ReadLine();
                try
                {
                    var hexVal = Convert.ToInt32(s, 16);
                    var strVal = Char.ConvertFromUtf32(hexVal);

                    config.RequestDownvoteEmote = strVal;
                }
                catch
                {
                    config.RequestDownvoteEmote = s;
                }

                Console.WriteLine("Write the number of downvotes required in order for a support request to be declined (Min 1, Max 255): ");
                if (byte.TryParse(Console.ReadLine(), out byte n) && n > 0)
                    config.DownvotesForDenial = n;
                else
                    Console.WriteLine("Not a valid number (Min 1, Max 255).");

                Console.WriteLine("Please enter the name of the emote (if custom, the unicode otherwise) to be used to request a ban of a user from support requests: ");

                s = Console.ReadLine();
                try
                {
                    var hexVal = Convert.ToInt32(s, 16);
                    var strVal = Char.ConvertFromUtf32(hexVal);

                    config.RequestBanEmote = strVal;
                }
                catch
                {
                    config.RequestBanEmote = s;
                }

                Console.WriteLine("Write the number of votes required in order for a user to be banned from support requests (Min 1, Max 255): ");
                if (byte.TryParse(Console.ReadLine(), out n) && n > 0)
                    config.BanvotesForRemoval = n;
                else
                    Console.WriteLine("Not a valid number (Min 1, Max 255).");

                Console.WriteLine("Please enter the name of the emote (if custom, the unicode otherwise) to be used to ban a user from support requests: ");

                s = Console.ReadLine();
                try
                {
                    var hexVal = Convert.ToInt32(s, 16);
                    var strVal = Char.ConvertFromUtf32(hexVal);

                    config.AdminBanEmote = strVal;
                }
                catch
                {
                    config.AdminBanEmote = s;
                }

                Console.WriteLine("Please enter the name of the emote (if custom, the unicode otherwise) to be used to deny a request: ");

                s = Console.ReadLine();
                try
                {
                    var hexVal = Convert.ToInt32(s, 16);
                    var strVal = Char.ConvertFromUtf32(hexVal);

                    config.AdminDenyEmote = strVal;
                }
                catch
                {
                    config.AdminDenyEmote = s;
                }

                Console.WriteLine("Please write if anti-tag should be bypassed by supporters (Y/N): ");
                if ("n" == Console.ReadLine().ToLowerInvariant())
                    config.SupporterAntiTagBypass = false;

                Console.WriteLine("Write the number (in Milliseconds) that the support-requests channel should be cleared (Min 1, Max 18446744073709551615): ");
                if (ulong.TryParse(Console.ReadLine(), out ulong m) && m > 1000)
                    config.SupportRequestClearMilliseconds = m;
                else
                    Console.WriteLine("Not a valid number (Min 1000, Max 18446744073709551615)");

                Console.WriteLine("Write the ID of the general channel: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.GeneralChannel = m;
                else
                    Console.WriteLine("Not a valid ID.");

                Console.WriteLine("Write the ID of the support channel: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.SupportChannel = m;
                else
                    Console.WriteLine("Not a valid ID.");

                Console.WriteLine("Write the ID of the support requests channel: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.SupportRequestsChannel = m;
                else
                    Console.WriteLine("Not a valid ID.");

                Console.WriteLine("Write the ID of the log channel: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.LogChannel = m;
                else
                    Console.WriteLine("Not a valid ID.");

                Console.WriteLine("Write your discord ID: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.OwnerID = m;
                else
                    Console.WriteLine("Not a valid ID.");

                Console.WriteLine("Write the ID of the supporter role: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.SupporterRole = m;
                else
                    Console.WriteLine("Not a valid ID.");

                Console.WriteLine("Write the ID of the staff role: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.StaffRole = m;
                else
                    Console.WriteLine("Not a valid ID.");
                
                Console.WriteLine("Write the ID of the banned from requests role: ");
                if (ulong.TryParse(Console.ReadLine(), out m))
                    config.RequestBannedRole = m;
                else
                    Console.WriteLine("Not a valid ID.");

                config.SaveJson();
                Console.WriteLine("First-Start Configuration Generated");
                return true;
            }
            Console.WriteLine("Configuration Verified");
            return false;
        }
        public static Configuration Load()
        {
            string file = Path.Combine(AppContext.BaseDirectory, FileName);
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(file));
        }
    }
}