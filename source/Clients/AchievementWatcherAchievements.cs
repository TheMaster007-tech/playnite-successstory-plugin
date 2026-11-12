using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Models;
using SuccessStory.Models;
using SuccessStory.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace SuccessStory.Clients
{
    public class AchievementWatcherProvider : GenericAchievements
    {
        private static readonly Guid SteamLibraryId = Guid.Parse("CB91DFC9-B977-43BF-8E70-55F46E410FAB");
        private static int nextRequestId;
        private const int MaxMessageBytes = 8 * 1024 * 1024;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        public AchievementWatcherProvider() : base("Achievement Watcher Next")
        {
        }

        public override GameAchievements GetAchievements(Game game)
        {
            if (game == null)
            {
                return null;
            }

            try
            {
                var requestId = Interlocked.Increment(ref nextRequestId);
                var question = CreateQuestion(game, requestId);
                return RequestAchievements(question, game, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Achievement Watcher Next request failed for {game.Name}", true, PluginDatabase.PluginName);
                return null;
            }
        }

        public override bool ValidateConfiguration()
        {
            var port = PluginDatabase.PluginSettings.Settings.AchievementWatcherPort;
            return ValidatePort(port);
        }

        public override bool EnabledInSettings()
        {
            return PluginDatabase.PluginSettings.Settings.EnableAchievementWatcher;
        }

        private static bool ValidatePort(int port)
        {
            return port >= 1 && port <= 65535;
        }

        private static GameAchievements RequestAchievements(IDictionary<string, object> question, Game game, CancellationToken cancellationToken)
        {
            var settings = PluginDatabase.PluginSettings.Settings;
            if (!ValidatePort(settings.AchievementWatcherPort))
            {
                Logger.Warn($"Achievement Watcher Next port is invalid: {settings.AchievementWatcherPort}");
                return null;
            }

            var uri = new Uri($"ws://127.0.0.1:{settings.AchievementWatcherPort}/?client=successstory");

            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var socket = new ClientWebSocket())
            {
                timeout.CancelAfter(RequestTimeout);
                if (!string.IsNullOrWhiteSpace(settings.AchievementWatcherLogin))
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.AchievementWatcherLogin));
                    socket.Options.SetRequestHeader("Authorization", "Basic " + credentials);
                }

                socket.ConnectAsync(uri, timeout.Token).GetAwaiter().GetResult();
                var json = new JavaScriptSerializer { MaxJsonLength = MaxMessageBytes }.Serialize(question);
                var bytes = Encoding.UTF8.GetBytes(json);
                socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, timeout.Token).GetAwaiter().GetResult();

                while (socket.State == WebSocketState.Open && !timeout.IsCancellationRequested)
                {
                    var message = ReceiveMessage(socket, timeout.Token);
                    if (message == null)
                    {
                        return null;
                    }

                    GameAchievements result;
                    if (TryReadReply(message, (int)question["id"], game, out result))
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private static string ReceiveMessage(ClientWebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new byte[64 * 1024];
            using (var message = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).GetAwaiter().GetResult();
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return null;
                    }

                    message.Write(buffer, 0, result.Count);
                    if (message.Length > MaxMessageBytes)
                    {
                        throw new InvalidDataException("Achievement Watcher Next sent an oversized bridge message.");
                    }
                }
                while (!result.EndOfMessage);

                return result.MessageType == WebSocketMessageType.Text
                    ? Encoding.UTF8.GetString(message.ToArray())
                    : string.Empty;
            }
        }

        private static bool TryReadReply(string json, int requestId, Game game, out GameAchievements result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            IDictionary<string, object> reply;
            try
            {
                reply = new JavaScriptSerializer { MaxJsonLength = MaxMessageBytes }
                    .DeserializeObject(json) as IDictionary<string, object>;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            if (reply == null || !string.Equals(GetText(reply, "cmd"), "bridge-game", StringComparison.Ordinal))
            {
                return false;
            }

            int responseId;
            if (!int.TryParse(GetText(reply, "id"), NumberStyles.Integer, CultureInfo.InvariantCulture, out responseId) ||
                responseId != requestId)
            {
                return false;
            }

            if (!GetFlag(reply, "found"))
            {
                return true;
            }

            var awGame = GetMap(reply, "game");
            if (awGame == null)
            {
                return true;
            }

            var items = new List<Achievement>();
            var rawAchievements = GetValue(awGame, "achievements") as IEnumerable;
            if (rawAchievements != null)
            {
                foreach (var rawAchievement in rawAchievements)
                {
                    var achievement = rawAchievement as IDictionary<string, object>;
                    if (achievement == null)
                    {
                        continue;
                    }

                    var id = GetText(achievement, "id");
                    var name = GetText(achievement, "name");
                    if (id.Length == 0 && name.Length == 0)
                    {
                        continue;
                    }

                    var unlocked = GetFlag(achievement, "unlocked");
                    var icon = GetText(achievement, "icon");
                    var lockedIcon = GetText(achievement, "iconLocked");
                    var fallbackIcon = icon.Length == 0 ? lockedIcon : icon;
                    var achievementItem = new Achievement
                    {
                        ApiName = id,
                        Name = name.Length == 0 ? id : name,
                        Description = GetText(achievement, "description"),
                        UrlUnlocked = fallbackIcon,
                        UrlLocked = lockedIcon.Length == 0 ? fallbackIcon : lockedIcon,
                        IsHidden = GetFlag(achievement, "hidden"),
                        Percent = GetRarity(achievement),
                        DateUnlocked = unlocked
                            ? GetUnlockDate(achievement) ?? new DateTime(1982, 1, 1)
                            : (DateTime?)null
                    };

                    var progress = GetMap(achievement, "progress");
                    double current;
                    double max;
                    if (!unlocked && progress != null &&
                        TryGetNumber(progress, "current", out current) &&
                        TryGetNumber(progress, "max", out max) && max > 0)
                    {
                        achievementItem.Progression = new AchProgression
                        {
                            Min = 0,
                            Max = max,
                            Value = Math.Max(0, Math.Min(max, current))
                        };
                    }

                    items.Add(achievementItem);
                }
            }

            if (items.Count == 0)
            {
                return true;
            }

            result = PluginDatabase.GetDefault(game);
            result.Items = items;
            var matchedName = GetText(awGame, "name");
            result.SourcesLink = new CommonPluginsShared.Models.SourceLink
            {
                Name = "Achievement Watcher Next",
                GameName = matchedName.Length == 0 ? game.Name : matchedName
            };
            return true;
        }

        private static IDictionary<string, object> CreateQuestion(Game game, int requestId)
        {
            var exe = string.Empty;
            var launchOptions = string.Empty;
            var action = game.GameActions?.FirstOrDefault(x => x != null && x.IsPlayAction)
                ?? game.GameActions?.FirstOrDefault(x => x != null);

            if (action != null && action.Type == GameActionType.File)
            {
                launchOptions = Expand(game, action.Arguments);
                var path = (Expand(game, action.Path) ?? string.Empty).Trim().Trim('"');
                if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (!Path.IsPathRooted(path))
                        {
                            if (string.IsNullOrWhiteSpace(game.InstallDirectory))
                            {
                                path = string.Empty;
                            }
                            else
                            {
                                path = Path.Combine(Expand(game, game.InstallDirectory), path);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            exe = Path.GetFullPath(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Could not resolve the executable for {game.Name}", true, PluginDatabase.PluginName);
                    }
                }
            }
            else if (action != null && action.Type == GameActionType.Emulator &&
                     game.Roms != null && game.Roms.Count > 0 && game.Roms[0] != null)
            {
                launchOptions = Expand(game, game.Roms[0].Path);
            }

            var steamAppId = string.Empty;

            if (game.PluginId == SteamLibraryId && !string.IsNullOrWhiteSpace(game.GameId))
            {
                int parsedAppId;
                if (int.TryParse(game.GameId, NumberStyles.None, CultureInfo.InvariantCulture, out parsedAppId) && parsedAppId > 0)
                {
                    steamAppId = parsedAppId.ToString(CultureInfo.InvariantCulture);
                }
            }

            return new Dictionary<string, object>
            {
                { "cmd", "bridge-game" },
                { "id", requestId },
                { "exe", exe },
                { "launchOptions", launchOptions },
                { "steamAppId", steamAppId },
                { "name", game.Name ?? string.Empty }
            };
        }

        private static string Expand(Game game, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            try
            {
                return API.Instance.ExpandGameVariables(game, value) ?? value;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Could not expand a launch value for {game.Name}", true, PluginDatabase.PluginName);
                return value;
            }
        }

        private static DateTime? GetUnlockDate(IDictionary<string, object> achievement)
        {
            double timestamp;
            if (!TryGetNumber(achievement, "unlockedAt", out timestamp) ||
                timestamp <= 0 ||
                timestamp > 253402300799d)
            {
                return null;
            }

            try
            {
                return DateTimeOffset.FromUnixTimeSeconds((long)timestamp).LocalDateTime;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static float GetRarity(IDictionary<string, object> achievement)
        {
            double rarity;
            return TryGetNumber(achievement, "rarity", out rarity) &&
                !double.IsNaN(rarity) && !double.IsInfinity(rarity) &&
                rarity >= 0 && rarity <= 100
                    ? (float)rarity
                    : 100;
        }

        private static bool TryGetNumber(IDictionary<string, object> map, string key, out double value)
        {
            var raw = GetValue(map, key);
            return double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static IDictionary<string, object> GetMap(IDictionary<string, object> map, string key)
        {
            return GetValue(map, key) as IDictionary<string, object>;
        }

        private static object GetValue(IDictionary<string, object> map, string key)
        {
            object value;
            return map != null && map.TryGetValue(key, out value) ? value : null;
        }

        private static string GetText(IDictionary<string, object> map, string key)
        {
            var value = GetValue(map, key);
            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static bool GetFlag(IDictionary<string, object> map, string key)
        {
            var value = GetValue(map, key);
            if (value is bool)
            {
                return (bool)value;
            }

            bool parsed;
            return bool.TryParse(GetText(map, key), out parsed) && parsed;
        }
    }
}
