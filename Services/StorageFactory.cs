using CounterStrikeSharp.API;
using PlayerSettings;

using SpectatorList.Configs;

namespace SpectatorList.Services
{
    public static class StorageFactory
    {
        public static IStorageService CreateStorageService(SpectatorConfig config, ISettingsApi? settingsApi)
        {
            var storageType = config.Storage.StorageType.ToLower();

            switch (storageType)
            {
                case "playersettings":
                    if (settingsApi != null)
                    {
                        return new PlayerSettingsStorage(settingsApi);
                    }
                    else
                    {
                        Server.PrintToConsole("[SpectatorList] PlayerSettings requested but not available, falling back to Memory storage");
                        return new MemoryStorage();
                    }

                case "mysql":
                    if (IsValidDatabaseConfig(config.Storage.Database))
                    {
                        return new DatabaseService(config);
                    }
                    else
                    {
                        Server.PrintToConsole("[SpectatorList] MySQL requested but database configuration is invalid, falling back to Memory storage");
                        return new MemoryStorage();
                    }

                case "memory":
                    return new MemoryStorage();

                default:
                    Server.PrintToConsole($"[SpectatorList] Unknown storage type '{config.Storage.StorageType}', falling back to Memory storage");
                    return new MemoryStorage();
            }
        }

        private static bool IsValidDatabaseConfig(DatabaseConfig dbConfig)
        {
            return !string.IsNullOrEmpty(dbConfig.Host) && !string.IsNullOrEmpty(dbConfig.DatabaseName) && !string.IsNullOrEmpty(dbConfig.User);
        }
    }
}