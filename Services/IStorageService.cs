using CounterStrikeSharp.API.Core;

namespace SpectatorList.Services
{
    public interface IStorageService
    {
        Task<bool> InitializeAsync();
        bool IsPlayerDisplayEnabled(CCSPlayerController player);
        Task<bool> IsPlayerDisplayEnabledAsync(CCSPlayerController player);
        void TogglePlayerDisplay(CCSPlayerController player);
        Task TogglePlayerDisplayAsync(CCSPlayerController player);
        void OnPlayerDisconnect(CCSPlayerController player);
        void ClearCache();
        string GetStorageType();
    }
}