using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using MySqlConnector;

using SpectatorList.Configs;
using SpectatorList.Models;

namespace SpectatorList.Services
{
    public class DatabaseService
    {
        private readonly SpectatorConfig _config;
        private readonly Dictionary<string, PlayerPreferences> _preferencesCache = new();
        private bool _isInitialized = false;

        public DatabaseService(SpectatorConfig config)
        {
            _config = config;
        }

        public bool IsEnabled => !string.IsNullOrEmpty(_config.Database.Host) && !string.IsNullOrEmpty(_config.Database.DatabaseName);

        public async Task<bool> InitializeDatabase()
        {
            if (!IsEnabled)
            {
                await Task.Run(() =>
                {
                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole("[SpectatorList] Database is disabled in configuration");
                    });
                });
                return false;
            }

            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();
                await CreateTable(connection);
                _isInitialized = true;

                await Task.Run(() =>
                {
                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole("[SpectatorList] Database connection established and table created");
                    });
                });
                return true;
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                await Task.Run(() =>
                {
                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole($"[SpectatorList] Failed to initialize database: {ex.Message}");
                    });
                });
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

        public async Task<PlayerPreferences?> LoadPlayerPreferences(string steamId)
        {
            if (!IsEnabled || !_isInitialized)
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
                await Task.Run(() =>
                {
                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole($"[SpectatorList] Error loading player preferences for SteamID {steamId}: {ex.Message}");
                    });
                });
                return null;
            }
        }

        public async Task<bool> SavePlayerPreferences(PlayerPreferences preferences)
        {
            if (!IsEnabled || !_isInitialized)
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
                await Task.Run(() =>
                {
                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole($"[SpectatorList] Error saving player preferences for SteamID {preferences.SteamId}: {ex.Message}");
                    });
                });
                return false;
            }
        }

        public async Task<PlayerPreferences> LoadOrCreatePlayerPreferences(string steamId)
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

                if (IsEnabled && _isInitialized)
                {
                    await SavePlayerPreferences(preferences);
                }
            }

            return preferences;
        }

        public void CachePlayerPreferences(string steamId, PlayerPreferences preferences)
        {
            _preferencesCache[steamId] = preferences;
        }

        public PlayerPreferences? GetCachedPlayerPreferences(string steamId)
        {
            return _preferencesCache.TryGetValue(steamId, out var preferences) ? preferences : null;
        }

        public void RemoveFromCache(string steamId)
        {
            _preferencesCache.Remove(steamId);
        }

        public void ClearCache()
        {
            _preferencesCache.Clear();
        }

        private MySqlConnection GetConnection()
        {
            if (_config.Database == null)
            {
                throw new InvalidOperationException("Database configuration is null");
            }

            var builder = new MySqlConnectionStringBuilder
            {
                Server = _config.Database.Host,
                Port = _config.Database.Port,
                UserID = _config.Database.User,
                Database = _config.Database.DatabaseName,
                Password = _config.Database.Password,
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
                await Task.Run(() =>
                {
                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole($"[SpectatorList] Database connection test failed: {ex.Message}");
                    });
                });
                return false;
            }
        }
    }
}