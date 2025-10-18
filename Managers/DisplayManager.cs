using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

using SpectatorList.Configs;
using SpectatorList.Display;
using SpectatorList.Services;

namespace SpectatorList.Managers
{
    public class DisplayManager : IDisposable
    {
        private readonly Dictionary<int, CenterMessageDisplay> _centerDisplays;
        private readonly Dictionary<int, ScreenViewDisplay> _screenDisplays;
        private readonly SpectatorConfig _config;
        private readonly BasePlugin _plugin;
        private readonly IStorageService _storageService;

        public DisplayManager(SpectatorConfig config, BasePlugin plugin, IStorageService storageService)
        {
            _config = config;
            _plugin = plugin;
            _storageService = storageService;
            _centerDisplays = new Dictionary<int, CenterMessageDisplay>();
            _screenDisplays = new Dictionary<int, ScreenViewDisplay>();

            _ = InitializeStorageAsync();
        }

        private async Task InitializeStorageAsync()
        {
            try
            {
                var success = await _storageService.InitializeAsync();
                var storageType = _storageService.GetStorageType();

                Server.NextFrame(() =>
                {
                    if (success)
                    {
                        Server.PrintToConsole($"[SpectatorList] Storage initialized successfully: {storageType}");
                    }
                    else
                    {
                        Server.PrintToConsole($"[SpectatorList] Storage initialization failed, using: {storageType}");
                    }
                });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                Server.NextFrame(() =>
                {
                    Server.PrintToConsole($"[SpectatorList] Error initializing storage: {errorMessage}");
                });
            }
        }

        public async Task<bool> IsPlayerDisplayEnabledAsync(CCSPlayerController player)
        {
            try
            {
                return await _storageService.IsPlayerDisplayEnabledAsync(player);
            }
            catch (Exception ex)
            {
                var playerName = player.PlayerName;
                var errorMessage = ex.Message;

                Server.NextFrame(() =>
                {
                    Server.PrintToConsole($"[SpectatorList] Error checking display status for {playerName}: {errorMessage}");
                });
                return true;
            }
        }

        public bool IsPlayerDisplayEnabled(CCSPlayerController player)
        {
            try
            {
                return _storageService.IsPlayerDisplayEnabled(player);
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                {
                    Server.PrintToConsole($"[SpectatorList] Error checking display status for {player.PlayerName}: {ex.Message}");
                });
                return true;
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
            try
            {
                await _storageService.TogglePlayerDisplayAsync(player);

                var isEnabled = await _storageService.IsPlayerDisplayEnabledAsync(player);
                if (!isEnabled)
                {
                    Server.NextFrame(() => CleanupPlayerDisplay(player));
                }
            }
            catch (Exception ex)
            {
                var playerName = player.PlayerName;
                var errorMessage = ex.Message;

                Server.NextFrame(() =>
                {
                    Server.PrintToConsole($"[SpectatorList] Error toggling display for {playerName}: {errorMessage}");
                });
            }
        }

        public void TogglePlayerDisplay(CCSPlayerController player)
        {
            try
            {
                _storageService.TogglePlayerDisplay(player);

                var isEnabled = _storageService.IsPlayerDisplayEnabled(player);
                if (!isEnabled)
                {
                    CleanupPlayerDisplay(player);
                }
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error toggling display for {player.PlayerName}: {ex.Message}");
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

                    var errorMessage = ex.Message;
                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole($"[SpectatorList] Error checking permissions for spectator: {errorMessage}");
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
                    DisplayOnScreen(player, filteredSpectators);
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

        private void DisplayOnScreen(CCSPlayerController player, List<CCSPlayerController> spectators)
        {
            if (spectators.Count == 0)
                return;

            CleanupPlayerDisplay(player);

            if (_config.Display.UseCenterMessage)
            {
                var centerDisplay = new CenterMessageDisplay(player, _config, _plugin);
                _centerDisplays[player.Slot] = centerDisplay;
                centerDisplay.ShowSpectatorList(spectators);
            }

            if (_config.Display.UseScreenView)
            {
                var screenDisplay = new ScreenViewDisplay(player, _config, _plugin);
                _screenDisplays[player.Slot] = screenDisplay;
                screenDisplay.ShowSpectatorList(spectators);
            }

            if (_config.Display.SendToChat)
            {
                DisplayInChat(player, spectators);
            }
        }

        private void DisplayInChat(CCSPlayerController player, List<CCSPlayerController> spectators)
        {
            if (spectators.Count == 0)
                return;

            try
            {
                var spectatorCount = spectators.Count;
                var spectatorNames = new List<string>();

                foreach (var spectator in spectators.Take(_config.Display.MaxNamesInMessage))
                {
                    if (spectator.IsValid && !string.IsNullOrEmpty(spectator.PlayerName))
                    {
                        spectatorNames.Add(spectator.PlayerName);
                    }
                }

                if (spectators.Count > _config.Display.MaxNamesInMessage)
                {
                    var remainingCount = spectators.Count - _config.Display.MaxNamesInMessage;
                    string andMoreText = _plugin.Localizer["and_more", remainingCount];
                    spectatorNames.Add(andMoreText);
                }

                var spectatorList = string.Join(", ", spectatorNames);

                string prefix = _plugin.Localizer["prefix"];
                string message = _plugin.Localizer["spectators_watching", spectatorCount, spectatorList];
                player.PrintToChat($"{prefix} {message}");
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error in DisplayInChat: {ex.Message}");
                Server.PrintToConsole($"[SpectatorList] Exception type: {ex.GetType().Name}");
                try
                {
                    string prefix = _plugin.Localizer["prefix"];
                    string simpleMessage = _plugin.Localizer["spectators_simplewatching", spectators.Count];
                    player.PrintToChat($"{prefix} {simpleMessage}");
                }
                catch (Exception ex2)
                {
                    Server.PrintToConsole($"[SpectatorList] Fallback message also failed: {ex2.Message}");
                }
            }
        }

        public void CleanupPlayerDisplay(CCSPlayerController player)
        {
            if (_centerDisplays.TryGetValue(player.Slot, out var centerDisplay))
            {
                centerDisplay.Dispose();
                _centerDisplays.Remove(player.Slot);
            }

            if (_screenDisplays.TryGetValue(player.Slot, out var screenDisplay))
            {
                screenDisplay.Dispose();
                _screenDisplays.Remove(player.Slot);
            }
        }

        public void CleanupAllDisplays()
        {
            foreach (var display in _centerDisplays.Values)
            {
                display.Dispose();
            }
            _centerDisplays.Clear();

            foreach (var display in _screenDisplays.Values)
            {
                display.Dispose();
            }
            _screenDisplays.Clear();
        }

        public void HidePlayerDisplay(CCSPlayerController player)
        {
            if (_centerDisplays.TryGetValue(player.Slot, out var centerDisplay))
            {
                centerDisplay.HideDisplay();
            }

            if (_screenDisplays.TryGetValue(player.Slot, out var screenDisplay))
            {
                screenDisplay.HideDisplay();
            }
        }

        public void OnPlayerDisconnect(CCSPlayerController player)
        {
            CleanupPlayerDisplay(player);
            _storageService.OnPlayerDisconnect(player);
        }

        public void Dispose()
        {
            CleanupAllDisplays();
            _storageService.ClearCache();
        }
    }
}