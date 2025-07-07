using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using PlayerSettings;

namespace SpectatorList.Services
{
    public class PlayerSettingsStorage : IStorageService
    {
        private readonly ISettingsApi? _settingsApi;
        private readonly HashSet<int> _fallbackDisabledPlayers;

        public PlayerSettingsStorage(ISettingsApi? settingsApi)
        {
            _settingsApi = settingsApi;
            _fallbackDisabledPlayers = new HashSet<int>();
        }

        public async Task<bool> InitializeAsync()
        {
            await Task.CompletedTask;
            if (_settingsApi != null)
            {
                Server.PrintToConsole("[SpectatorList] Using PlayerSettings for storage");
                return true;
            }
            else
            {
                Server.PrintToConsole("[SpectatorList] PlayerSettings not available, using memory fallback");
                return false;
            }
        }

        public bool IsPlayerDisplayEnabled(CCSPlayerController player)
        {
            if (_settingsApi != null)
            {
                var displayEnabled = _settingsApi.GetPlayerSettingsValue(player, "spectator_display_enabled", "true");
                return displayEnabled.ToLower() == "true";
            }
            return !_fallbackDisabledPlayers.Contains(player.Slot);
        }

        public async Task<bool> IsPlayerDisplayEnabledAsync(CCSPlayerController player)
        {
            return await Task.FromResult(IsPlayerDisplayEnabled(player));
        }

        public void TogglePlayerDisplay(CCSPlayerController player)
        {
            if (_settingsApi != null)
            {
                var currentValue = _settingsApi.GetPlayerSettingsValue(player, "spectator_display_enabled", "true");
                var newValue = currentValue.ToLower() == "true" ? "false" : "true";
                _settingsApi.SetPlayerSettingsValue(player, "spectator_display_enabled", newValue);
            }
            else
            {
                if (_fallbackDisabledPlayers.Contains(player.Slot))
                {
                    _fallbackDisabledPlayers.Remove(player.Slot);
                }
                else
                {
                    _fallbackDisabledPlayers.Add(player.Slot);
                }
            }
        }

        public async Task TogglePlayerDisplayAsync(CCSPlayerController player)
        {
            await Task.Run(() => TogglePlayerDisplay(player));
        }

        public void OnPlayerDisconnect(CCSPlayerController player)
        {
            _fallbackDisabledPlayers.Remove(player.Slot);
        }

        public void ClearCache()
        {
            _fallbackDisabledPlayers.Clear();
        }

        public string GetStorageType()
        {
            return _settingsApi != null ? "PlayerSettings" : "Memory (Fallback)";
        }
    }
}