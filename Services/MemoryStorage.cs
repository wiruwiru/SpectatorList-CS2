using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace SpectatorList.Services
{
    public class MemoryStorage : IStorageService
    {
        private readonly HashSet<int> _disabledPlayers;

        public MemoryStorage()
        {
            _disabledPlayers = new HashSet<int>();
        }

        public async Task<bool> InitializeAsync()
        {
            await Task.CompletedTask;
            Server.PrintToConsole("[SpectatorList] Using Memory storage (preferences will not persist between server restarts)");
            return true;
        }

        public bool IsPlayerDisplayEnabled(CCSPlayerController player)
        {
            return !_disabledPlayers.Contains(player.Slot);
        }

        public async Task<bool> IsPlayerDisplayEnabledAsync(CCSPlayerController player)
        {
            return await Task.FromResult(IsPlayerDisplayEnabled(player));
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
            }
        }

        public async Task TogglePlayerDisplayAsync(CCSPlayerController player)
        {
            await Task.Run(() => TogglePlayerDisplay(player));
        }

        public void OnPlayerDisconnect(CCSPlayerController player)
        {
            _disabledPlayers.Remove(player.Slot);
        }

        public void ClearCache()
        {
            _disabledPlayers.Clear();
        }

        public string GetStorageType()
        {
            return "Memory";
        }
    }
}