using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

using SpectatorList.Configs;
using SpectatorList.Display;
using SpectatorList.Services;

namespace SpectatorList.Managers
{
    public class DisplayManager : IDisposable
    {
        private readonly Dictionary<int, ScreenViewDisplay> _screenDisplays;
        private readonly HashSet<int> _disabledPlayers;
        private readonly SpectatorConfig _config;
        private readonly BasePlugin _plugin;
        private readonly DatabaseService _databaseService;

        public DisplayManager(SpectatorConfig config, BasePlugin plugin)
        {
            _config = config;
            _plugin = plugin;
            _screenDisplays = new Dictionary<int, ScreenViewDisplay>();
            _disabledPlayers = new HashSet<int>();
            _databaseService = new DatabaseService(config);

            if (_databaseService.IsEnabled)
            {
                _ = InitializeDatabaseAsync();
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _databaseService.InitializeDatabase();
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error initializing database: {ex.Message}");
            }
        }

        public async Task<bool> IsPlayerDisplayEnabledAsync(CCSPlayerController player)
        {
            if (_databaseService.IsEnabled)
            {
                try
                {
                    var steamId = player.SteamID.ToString();
                    var preferences = await _databaseService.LoadOrCreatePlayerPreferences(steamId);
                    return preferences.DisplayEnabled;
                }
                catch (Exception ex)
                {
                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole($"[SpectatorList] Error loading preferences for {player.PlayerName}: {ex.Message}");
                    });
                    return !_disabledPlayers.Contains(player.Slot);
                }
            }

            return !_disabledPlayers.Contains(player.Slot);
        }

        public bool IsPlayerDisplayEnabled(CCSPlayerController player)
        {
            if (_databaseService.IsEnabled)
            {
                var cachedPrefs = _databaseService.GetCachedPlayerPreferences(player.SteamID.ToString());
                if (cachedPrefs != null)
                {
                    return cachedPrefs.DisplayEnabled;
                }

                _ = LoadPlayerPreferencesAsync(player);
                return true;
            }

            return !_disabledPlayers.Contains(player.Slot);
        }

        private async Task LoadPlayerPreferencesAsync(CCSPlayerController player)
        {
            try
            {
                var steamId = player.SteamID.ToString();
                await _databaseService.LoadOrCreatePlayerPreferences(steamId);
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error loading preferences for {player.PlayerName}: {ex.Message}");
            }
        }

        public bool CanPlayerViewList(CCSPlayerController player)
        {
            if (string.IsNullOrEmpty(_config.CanViewList))
                return true;

            return AdminManager.PlayerHasPermissions(player, _config.CanViewList);
        }

        public async Task TogglePlayerDisplayAsync(CCSPlayerController player)
        {
            if (_databaseService.IsEnabled)
            {
                try
                {
                    var steamId = player.SteamID.ToString();
                    var preferences = await _databaseService.LoadOrCreatePlayerPreferences(steamId);

                    preferences.DisplayEnabled = !preferences.DisplayEnabled;
                    preferences.LastUpdated = DateTime.Now;

                    await _databaseService.SavePlayerPreferences(preferences);

                    if (!preferences.DisplayEnabled)
                    {
                        Server.NextFrame(() => CleanupPlayerDisplay(player));
                    }
                }
                catch (Exception ex)
                {
                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole($"[SpectatorList] Error toggling display for {player.PlayerName}: {ex.Message}");
                    });
                    Server.NextFrame(() => TogglePlayerDisplayMemory(player));
                }
            }
            else
            {
                Server.NextFrame(() => TogglePlayerDisplayMemory(player));
            }
        }

        public void TogglePlayerDisplay(CCSPlayerController player)
        {
            if (_databaseService.IsEnabled)
            {
                _ = TogglePlayerDisplayAsync(player);
            }
            else
            {
                TogglePlayerDisplayMemory(player);
            }
        }

        private void TogglePlayerDisplayMemory(CCSPlayerController player)
        {
            if (_disabledPlayers.Contains(player.Slot))
            {
                _disabledPlayers.Remove(player.Slot);
            }
            else
            {
                _disabledPlayers.Add(player.Slot);
                CleanupPlayerDisplay(player);
            }
        }

        public List<CCSPlayerController> FilterSpectators(List<CCSPlayerController> spectators)
        {
            if (string.IsNullOrEmpty(_config.Display.ExclusionFlag))
                return spectators;

            var filteredList = new List<CCSPlayerController>();

            foreach (var spectator in spectators)
            {
                try
                {
                    if (spectator.IsValid && !AdminManager.PlayerHasPermissions(spectator, _config.Display.ExclusionFlag))
                    {
                        filteredList.Add(spectator);
                    }
                }
                catch (Exception ex)
                {
                    if (spectator.IsValid)
                    {
                        filteredList.Add(spectator);
                    }

                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole($"[SpectatorList] Error checking permissions for spectator: {ex.Message}");
                    });
                }
            }

            return filteredList;
        }

        public async Task DisplaySpectatorListAsync(CCSPlayerController player, List<CCSPlayerController> spectators)
        {
            if (!player.IsValid || spectators == null)
                return;

            bool canView = false;
            var canViewTask = new TaskCompletionSource<bool>();

            Server.NextFrame(() =>
            {
                try
                {
                    canView = CanPlayerViewList(player);
                    canViewTask.SetResult(canView);
                }
                catch (Exception ex)
                {
                    Server.PrintToConsole($"[SpectatorList] Error checking view permissions: {ex.Message}");
                    canViewTask.SetResult(false);
                }
            });

            canView = await canViewTask.Task;
            if (!canView)
                return;

            var isEnabled = await IsPlayerDisplayEnabledAsync(player);
            if (!isEnabled)
                return;

            var filteredSpectators = new List<CCSPlayerController>();
            var filterTask = new TaskCompletionSource<List<CCSPlayerController>>();

            Server.NextFrame(() =>
            {
                try
                {
                    filteredSpectators = FilterSpectators(spectators);
                    filterTask.SetResult(filteredSpectators);
                }
                catch (Exception ex)
                {
                    Server.PrintToConsole($"[SpectatorList] Error filtering spectators: {ex.Message}");
                    filterTask.SetResult(new List<CCSPlayerController>());
                }
            });

            filteredSpectators = await filterTask.Task;

            if (filteredSpectators.Count == 0)
            {
                Server.NextFrame(() => CleanupPlayerDisplay(player));
                return;
            }

            Server.NextFrame(() =>
            {
                try
                {
                    if (_config.Display.SendToChat)
                    {
                        DisplayInChat(player, filteredSpectators);
                    }

                    if (_config.Display.UseScreenView)
                    {
                        DisplayOnScreen(player, filteredSpectators);
                    }
                }
                catch (Exception ex)
                {
                    Server.PrintToConsole($"[SpectatorList] Error displaying spectator list: {ex.Message}");
                }
            });
        }

        public void DisplaySpectatorList(CCSPlayerController player, List<CCSPlayerController> spectators)
        {
            _ = DisplaySpectatorListAsync(player, spectators);
        }

        private void DisplayInChat(CCSPlayerController player, List<CCSPlayerController> spectators)
        {
            if (spectators.Count == 0)
                return;

            var spectatorNames = spectators.Select(s => s.PlayerName).ToList();
            var spectatorCount = spectators.Count;

            if (spectatorNames.Count > _config.Display.MaxNamesInMessage)
            {
                var remainingCount = spectatorNames.Count - _config.Display.MaxNamesInMessage;
                spectatorNames = spectatorNames.Take(_config.Display.MaxNamesInMessage).ToList();
                spectatorNames.Add(_plugin.Localizer["and_more", remainingCount]);
            }

            var spectatorList = string.Join(", ", spectatorNames);
            var chatMessage = $"{_plugin.Localizer["prefix"]} {_plugin.Localizer["spectators_watching", spectatorCount, spectatorList]}";
            player.PrintToChat(chatMessage);
        }

        private void DisplayOnScreen(CCSPlayerController player, List<CCSPlayerController> spectators)
        {
            if (spectators.Count == 0)
                return;

            CleanupPlayerDisplay(player);

            var screenDisplay = new ScreenViewDisplay(player, _config, _plugin);
            _screenDisplays[player.Slot] = screenDisplay;

            screenDisplay.ShowSpectatorList(spectators);
        }

        public void CleanupPlayerDisplay(CCSPlayerController player)
        {
            if (_screenDisplays.TryGetValue(player.Slot, out var display))
            {
                display.Dispose();
                _screenDisplays.Remove(player.Slot);
            }
        }

        public void CleanupAllDisplays()
        {
            foreach (var display in _screenDisplays.Values)
            {
                display.Dispose();
            }
            _screenDisplays.Clear();
        }

        public void HidePlayerDisplay(CCSPlayerController player)
        {
            if (_screenDisplays.TryGetValue(player.Slot, out var display))
            {
                display.HideDisplay();
            }
        }

        public void OnPlayerDisconnect(CCSPlayerController player)
        {
            CleanupPlayerDisplay(player);
            _disabledPlayers.Remove(player.Slot);

            if (_databaseService.IsEnabled)
            {
                _databaseService.RemoveFromCache(player.SteamID.ToString());
            }
        }

        public void Dispose()
        {
            CleanupAllDisplays();
            _disabledPlayers.Clear();
            _databaseService.ClearCache();
        }
    }
}