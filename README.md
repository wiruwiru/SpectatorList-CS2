# SpectatorList CS2
Shows real-time spectators both in chat messages and on-screen display with customizable permissions and exclusion flags.

> [!CAUTION]
> The current solution for `"UseScreenView"` does not work as well as it did before the update on 07/28/2025. It is recommended not to use this feature for now, pending a more stable method.

![Counter-strike 2 Screenshot 2025 07 06 - 19 34 39 42](https://github.com/user-attachments/assets/d8a908ea-7baa-4609-bdee-29545edd693e)

---

## 🚀 Installation

### Basic Installation
1. Install [CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp) and [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)
2. Download [SpectatorList.zip](https://github.com/wiruwiru/SpectatorList-CS2/releases/latest) from releases
3. Extract and upload to your game server
4. Start server and configure the generated config file

### Optional Dependencies (for PlayerSettings storage)
If you want to use `PlayerSettings` storage type:
1. Install [AnyBaseLibCS2](https://github.com/NickFox007/AnyBaseLibCS2/releases/latest) (required for PlayerSettings)
2. Install [PlayerSettingsCS2](https://github.com/NickFox007/PlayerSettingsCS2/releases/latest) (required dependency)
3. Set `StorageType` to `"PlayerSettings"` in your config

### Optional Dependencies (for MySQL storage)
If you want to use `MySQL` storage type:
1. Configure your MySQL database
2. Set `StorageType` to `"MySQL"` in your config
3. Fill in the database connection details

---

## ⚙️ Storage Options

The plugin supports three different storage methods for user preferences:

| Storage Type | Description | Persistence | Performance | Dependencies |
|--------------|-------------|-------------|-------------|--------------|
| **PlayerSettings** | Uses PlayerSettings plugin | ✅ Persistent | ⚡ Fast | PlayerSettingsCS2 + AnyBaseLibCS2 |
| **MySQL** | Traditional database storage | ✅ Persistent | 🔄 Database queries | MySQL/MariaDB database |
| **Memory** | Temporary in-memory storage | ❌ Lost on restart | ⚡⚡ Fastest | None |

**Recommendation**: Use `PlayerSettings` for most servers, `MySQL` if you don't care about the number of active pool connections, and `Memory` for testing.

---

## 📋 Main Configuration Parameters
| Parameter            | Description                                                                                       | Required |
|----------------------|---------------------------------------------------------------------------------------------------|----------|
| `Commands`           | List of chat commands players can use to toggle spectator list display. (**Default**: `["css_speclist", "css_specs", "css_spectators"]`) | **YES**  |
| `CommandPermissions` | Permission flag required to use the toggle commands. Leave empty for all players. (**Default**: `"@css/vip"`) | **YES**  |
| `CanViewList`        | Permission flag required to view spectator lists (both chat and screen). Leave empty for all players. (**Default**: `"@css/vip"`) | **YES**  |
| `UpdateSettings`     | Configuration for automatic updates and periodic displays. | **YES**  |
| `DisplaySettings`    | Configuration for how spectator lists are displayed. | **YES**  |
| `StorageSettings`    | Configuration for user preference storage method. | **YES**  |

### Storage Settings Parameters
| Parameter         | Description                                                                                         | Required |
|-------------------|-----------------------------------------------------------------------------------------------------|----------|
| `StorageType`     | Storage method to use: `"PlayerSettings"`, `"MySQL"`, or `"Memory"`. (**Default**: `"PlayerSettings"`) | **YES**  |
| `Database`        | MySQL database configuration (only used when `StorageType` is `"MySQL"`). | **NO**   |

### Update Settings Parameters
| Parameter         | Description                                                                                         | Required |
|-------------------|-----------------------------------------------------------------------------------------------------|----------|
| `CheckInterval`   | How often (in seconds) to check for spectator changes. (**Default**: `2.0`) | **YES**  |
| `ShowOnChange`    | Show spectator list automatically when spectators change. (**Default**: `true`) | **YES**  |
| `ShowPeriodic`    | Show spectator list at regular intervals even without changes. (**Default**: `false`) | **YES**  |
| `PeriodicInterval` | Interval (in seconds) for periodic displays when `ShowPeriodic` is enabled. (**Default**: `5.0`) | **YES**  |

### Display Settings Parameters
| Parameter         | Description                                                                                         | Required |
|-------------------|-----------------------------------------------------------------------------------------------------|----------|
| `ExclusionFlag`   | Players with this flag will be hidden from spectator lists. (**Default**: `"@css/generic"`) | **YES**  |
| `MaxNamesInMessage` | Maximum number of spectator names to show before showing "and X more...". (**Default**: `5`) | **YES**  |
| `SendToChat`      | Enable/disable chat messages for spectator lists. (**Default**: `false`) | **YES**  |
| `UseCenterMessage` | Enable/disable center screen HTML message display for spectator lists. (**Default**: `false`) | **YES**  |
| `CenterMessageDuration` | Duration (in seconds) to show center message before auto-hiding. Set to 0 for permanent display. (**Default**: `5.0`) | **YES**  |
| `CenterMessageHtml` | Custom HTML template for center messages. Use placeholders: `{TITLE}`, `{SPECTATORS}`, `{COUNT}`. (**Default**: `"<font class='fontSize-m' color='#FFD700'>{TITLE}</font><br><font class='fontSize-m' color='#87CEEB'>{SPECTATORS}</font>`) | **YES**  |
| `UseScreenView`   | Enable/disable on-screen floating text display. (**Default**: `true`) | **YES**  |
| `ScreenViewSettings` | Configuration for on-screen display positioning and appearance. | **YES**  |

### Screen View Settings Parameters
| Parameter         | Description                                                                                         | Required |
|-------------------|-----------------------------------------------------------------------------------------------------|----------|
| `PositionX`       | Horizontal position offset for on-screen display. (**Default**: `-8.0`) | **YES**  |
| `PositionY`       | Vertical position offset for on-screen display. (**Default**: `1.0`) | **YES**  |
| `TitleColor`      | Hex color code for the spectator list title. (**Default**: `"#FFD700"`) | **YES**  |
| `PlayerNameColor` | Hex color code for spectator names. (**Default**: `"#FFFFFF"`) | **YES**  |
| `CountColor`      | Hex color code for spectator count. (**Default**: `"#87CEEB"`) | **YES**  |

### Database Settings Parameters
*(Only used when `StorageType` is set to `"MySQL"`)*
| Parameter         | Description                                                                                         | Required |
|-------------------|-----------------------------------------------------------------------------------------------------|----------|
| `Host`            | MySQL server hostname or IP address. (**Default**: `""`) | **NO**   |
| `Port`            | MySQL server port. (**Default**: `3306`) | **NO**   |
| `User`            | MySQL username for database connection. (**Default**: `""`) | **NO**   |
| `Password`        | MySQL password for database connection. (**Default**: `""`) | **NO**   |
| `DatabaseName`    | Name of the MySQL database to use. (**Default**: `""`) | **NO**   |

---

## 📊 Support

For issues, questions, or feature requests, please visit our [GitHub Issues](https://github.com/wiruwiru/SpectatorList-CS2/issues) page.
