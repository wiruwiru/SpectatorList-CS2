using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using MySqlConnector;

using SpectatorList.Configs;
using SpectatorList.Models;

namespace SpectatorList.Services
{
    public class DatabaseService : IStorageService
    {
        private readonly SpectatorConfig _config;
        private readonly Dictionary<string, PlayerPreferences> _preferencesCache = new();
        private readonly HashSet<int> _fallbackDisabledPlayers = new();
        private bool _isInitialized = false;
        private bool _databaseAvailable = false;

        public DatabaseService(SpectatorConfig config)
        {
            _config = config;
        }

        public bool IsEnabled => !string.IsNullOrEmpty(_config.Storage.Database.Host) &&
                                !string.IsNullOrEmpty(_config.Storage.Database.DatabaseName);

        public async Task<bool> InitializeAsync()
        {
            if (!IsEnabled)
            {
                Server.PrintToConsole("[SpectatorList] Database configuration is incomplete");
                return false;
            }

            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();
                await CreateTable(connection);
                _isInitialized = true;
                _databaseAvailable = true;

                Server.PrintToConsole("[SpectatorList] Database connection established and table created");
                return true;
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                _databaseAvailable = false;
                Server.PrintToConsole($"[SpectatorList] Failed to initialize database: {ex.Message}");
                Server.PrintToConsole("[SpectatorList] Falling back to memory storage for this session");
                return false;
            }
        }

        private async Task CreateTable(MySqlConnection connection)
        {
            var createTableQuery = @"
                CREATE TABLE IF NOT EXISTS spectatorlist_preferences (
                    steamid VARCHAR(32) PRIMARY KEY UNIQUE NOT NULL,
                    display_enabled BOOLEAN DEFAULT TRUE,
                    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            using var cmd = new MySqlCommand(createTableQuery, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        public bool IsPlayerDisplayEnabled(CCSPlayerController player)
        {
            if (!_databaseAvailable)
            {
                return !_fallbackDisabledPlayers.Contains(player.Slot);
            }

            var cachedPrefs = GetCachedPlayerPreferences(player.SteamID.ToString());
            if (cachedPrefs != null)
            {
                return cachedPrefs.DisplayEnabled;
            }

            _ = LoadPlayerPreferencesAsync(player);
            return true;
        }

        public async Task<bool> IsPlayerDisplayEnabledAsync(CCSPlayerController player)
        {
            if (!_databaseAvailable)
            {
                return !_fallbackDisabledPlayers.Contains(player.Slot);
            }

            try
            {
                var steamId = player.SteamID.ToString();
                var preferences = await LoadOrCreatePlayerPreferences(steamId);
                return preferences.DisplayEnabled;
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                {
                    Server.PrintToConsole($"[SpectatorList] Error loading preferences for {player.PlayerName}: {ex.Message}");
                });
                return !_fallbackDisabledPlayers.Contains(player.Slot);
            }
        }

        public void TogglePlayerDisplay(CCSPlayerController player)
        {
            if (!_databaseAvailable)
            {
                if (_fallbackDisabledPlayers.Contains(player.Slot))
                {
                    _fallbackDisabledPlayers.Remove(player.Slot);
                }
                else
                {
                    _fallbackDisabledPlayers.Add(player.Slot);
                }
                return;
            }

            _ = TogglePlayerDisplayAsync(player);
        }

        public async Task TogglePlayerDisplayAsync(CCSPlayerController player)
        {
            if (!_databaseAvailable)
            {
                Server.NextFrame(() => TogglePlayerDisplay(player));
                return;
            }

            try
            {
                var steamId = player.SteamID.ToString();
                var preferences = await LoadOrCreatePlayerPreferences(steamId);

                preferences.DisplayEnabled = !preferences.DisplayEnabled;
                preferences.LastUpdated = DateTime.Now;

                await SavePlayerPreferences(preferences);
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                {
                    Server.PrintToConsole($"[SpectatorList] Error toggling display for {player.PlayerName}: {ex.Message}");
                    TogglePlayerDisplay(player);
                });
            }
        }

        private async Task<PlayerPreferences?> LoadPlayerPreferences(string steamId)
        {
            if (!IsEnabled || !_isInitialized || !_databaseAvailable)
                return null;

            try
            {
                if (_preferencesCache.TryGetValue(steamId, out var cachedPrefs))
                {
                    return cachedPrefs;
                }

                using var connection = GetConnection();
                await connection.OpenAsync();

                var selectQuery = @"
                    SELECT steamid, display_enabled, last_updated 
                    FROM spectatorlist_preferences 
                    WHERE steamid = @steamid";

                using var cmd = new MySqlCommand(selectQuery, connection);
                cmd.Parameters.AddWithValue("@steamid", steamId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var preferences = new PlayerPreferences
                    {
                        SteamId = reader.GetString("steamid"),
                        DisplayEnabled = reader.GetBoolean("display_enabled"),
                        LastUpdated = reader.GetDateTime("last_updated")
                    };

                    _preferencesCache[steamId] = preferences;
                    return preferences;
                }

                return null;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error loading player preferences for SteamID {steamId}: {ex.Message}");
                _databaseAvailable = false;
                return null;
            }
        }

        private async Task<bool> SavePlayerPreferences(PlayerPreferences preferences)
        {
            if (!IsEnabled || !_isInitialized || !_databaseAvailable)
                return false;

            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();

                var upsertQuery = @"
                    INSERT INTO spectatorlist_preferences (steamid, display_enabled, last_updated)
                    VALUES (@steamid, @display_enabled, @last_updated)
                    ON DUPLICATE KEY UPDATE
                        display_enabled = @display_enabled,
                        last_updated = @last_updated";

                using var cmd = new MySqlCommand(upsertQuery, connection);
                cmd.Parameters.AddWithValue("@steamid", preferences.SteamId);
                cmd.Parameters.AddWithValue("@display_enabled", preferences.DisplayEnabled);
                cmd.Parameters.AddWithValue("@last_updated", preferences.LastUpdated);

                await cmd.ExecuteNonQueryAsync();

                _preferencesCache[preferences.SteamId] = preferences;
                return true;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error saving player preferences for SteamID {preferences.SteamId}: {ex.Message}");
                _databaseAvailable = false;
                return false;
            }
        }

        private async Task<PlayerPreferences> LoadOrCreatePlayerPreferences(string steamId)
        {
            var preferences = await LoadPlayerPreferences(steamId);
            if (preferences == null)
            {
                preferences = new PlayerPreferences
                {
                    SteamId = steamId,
                    DisplayEnabled = true,
                    LastUpdated = DateTime.Now
                };

                if (IsEnabled && _isInitialized && _databaseAvailable)
                {
                    await SavePlayerPreferences(preferences);
                }
            }

            return preferences;
        }

        private async Task LoadPlayerPreferencesAsync(CCSPlayerController player)
        {
            try
            {
                var steamId = player.SteamID.ToString();
                await LoadOrCreatePlayerPreferences(steamId);
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error loading preferences for {player.PlayerName}: {ex.Message}");
            }
        }

        public void CachePlayerPreferences(string steamId, PlayerPreferences preferences)
        {
            _preferencesCache[steamId] = preferences;
        }

        public PlayerPreferences? GetCachedPlayerPreferences(string steamId)
        {
            return _preferencesCache.TryGetValue(steamId, out var preferences) ? preferences : null;
        }

        public void OnPlayerDisconnect(CCSPlayerController player)
        {
            _preferencesCache.Remove(player.SteamID.ToString());
            _fallbackDisabledPlayers.Remove(player.Slot);
        }

        public void ClearCache()
        {
            _preferencesCache.Clear();
            _fallbackDisabledPlayers.Clear();
        }

        private MySqlConnection GetConnection()
        {
            if (_config.Storage.Database == null)
            {
                throw new InvalidOperationException("Database configuration is null");
            }

            var builder = new MySqlConnectionStringBuilder
            {
                Server = _config.Storage.Database.Host,
                Port = _config.Storage.Database.Port,
                UserID = _config.Storage.Database.User,
                Database = _config.Storage.Database.DatabaseName,
                Password = _config.Storage.Database.Password,
                Pooling = true,
                SslMode = MySqlSslMode.Preferred
            };

            return new MySqlConnection(builder.ConnectionString);
        }

        public async Task<bool> TestConnection()
        {
            if (!IsEnabled)
                return false;

            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Database connection test failed: {ex.Message}");
                return false;
            }
        }

        public string GetStorageType()
        {
            if (_databaseAvailable)
                return "MySQL Database";
            else
                return "Memory (Database Unavailable)";
        }
    }
}