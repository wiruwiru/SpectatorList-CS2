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
        private readonly SpectatorConfig _config;
        private readonly BasePlugin _plugin;
        private readonly IStorageService _storageService;

        public DisplayManager(SpectatorConfig config, BasePlugin plugin, IStorageService storageService)
        {
            _config = config;
            _plugin = plugin;
            _storageService = storageService;
            _screenDisplays = new Dictionary<int, ScreenViewDisplay>();

            _ = InitializeStorageAsync();
        }

        private async Task InitializeStorageAsync()
        {
            try
            {
                var success = await _storageService.InitializeAsync();
                var storageType = _storageService.GetStorageType();

                if (success)
                {
                    Server.PrintToConsole($"[SpectatorList] Storage initialized successfully: {storageType}");
                }
                else
                {
                    Server.PrintToConsole($"[SpectatorList] Storage initialization failed, using: {storageType}");
                }
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error initializing storage: {ex.Message}");
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
                Server.PrintToConsole($"[SpectatorList] Error checking display status for {player.PlayerName}: {ex.Message}");
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
                Server.PrintToConsole($"[SpectatorList] Error checking display status for {player.PlayerName}: {ex.Message}");
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
                Server.PrintToConsole($"[SpectatorList] Error toggling display for {player.PlayerName}: {ex.Message}");
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
            _storageService.OnPlayerDisconnect(player);
        }

        public void Dispose()
        {
            CleanupAllDisplays();
            _storageService.ClearCache();
        }
    }
}