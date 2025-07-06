using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

using SpectatorList.Configs;
using SpectatorList.Display;

namespace SpectatorList.Managers
{
    public class DisplayManager : IDisposable
    {
        private readonly Dictionary<int, ScreenViewDisplay> _screenDisplays;
        private readonly HashSet<int> _disabledPlayers;
        private readonly SpectatorConfig _config;
        private readonly BasePlugin _plugin;

        public DisplayManager(SpectatorConfig config, BasePlugin plugin)
        {
            _config = config;
            _plugin = plugin;
            _screenDisplays = new Dictionary<int, ScreenViewDisplay>();
            _disabledPlayers = new HashSet<int>();
        }

        public bool IsPlayerDisplayEnabled(CCSPlayerController player)
        {
            return !_disabledPlayers.Contains(player.Slot);
        }

        public bool CanPlayerViewList(CCSPlayerController player)
        {
            if (string.IsNullOrEmpty(_config.CanViewList))
                return true;

            return AdminManager.PlayerHasPermissions(player, _config.CanViewList);
        }

        public void TogglePlayerDisplay(CCSPlayerController player)
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

            return spectators.Where(spectator =>
                !AdminManager.PlayerHasPermissions(spectator, _config.Display.ExclusionFlag))
                .ToList();
        }

        public void DisplaySpectatorList(CCSPlayerController player, List<CCSPlayerController> spectators)
        {
            if (!player.IsValid || spectators == null || !IsPlayerDisplayEnabled(player))
                return;

            if (!CanPlayerViewList(player))
                return;

            var filteredSpectators = FilterSpectators(spectators);
            if (filteredSpectators.Count == 0)
            {
                CleanupPlayerDisplay(player);
                return;
            }

            if (_config.Display.SendToChat)
            {
                DisplayInChat(player, filteredSpectators);
            }

            if (_config.Display.UseScreenView)
            {
                DisplayOnScreen(player, filteredSpectators);
            }
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
        }

        public void Dispose()
        {
            CleanupAllDisplays();
            _disabledPlayers.Clear();
        }
    }
}