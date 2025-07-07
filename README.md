# SpectatorList CS2
Shows real-time spectators both in chat messages and on-screen display with customizable permissions and exclusion flags.

![Counter-strike 2 Screenshot 2025 07 06 - 19 34 39 42](https://github.com/user-attachments/assets/d8a908ea-7baa-4609-bdee-29545edd693e)

---

### Installation
1. Install [CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp) and [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)
2. Download [SpectatorList.zip](https://github.com/wiruwiru/SpectatorList-CS2/releases/latest) from releases
3. Extract and upload to your game server
4. Start server and configure the generated config file
5. (Optional) Configure MySQL database for persistent user preferences

---

### Main Configuration Parameters

| Parameter            | Description                                                                                       | Required |
|----------------------|---------------------------------------------------------------------------------------------------|----------|
| `Commands`           | List of chat commands players can use to toggle spectator list display. (**Default**: `["css_speclist", "css_specs", "css_spectators"]`) | **YES**  |
| `CommandPermissions` | Permission flag required to use the toggle commands. Leave empty for all players. (**Default**: `"@css/root"`) | **YES**  |
| `CanViewList`        | Permission flag required to view spectator lists (both chat and screen). Leave empty for all players. (**Default**: `""`) | **YES**  |
| `UpdateSettings`     | Configuration for automatic updates and periodic displays. | **YES**  |
| `DisplaySettings`    | Configuration for how spectator lists are displayed. | **YES**  |
| `Database`           | MySQL database configuration for persistent user preferences. | **NO**   |

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
| `MaxNamesInMessage` | Maximum number of spectator names to show in chat before showing "and X more...". (**Default**: `5`) | **YES**  |
| `SendToChat`      | Enable/disable chat messages for spectator lists. (**Default**: `false`) | **YES**  |
| `UseScreenView`   | Enable/disable on-screen floating text display. (**Default**: `true`) | **YES**  |
| `ScreenViewSettings` | Configuration for on-screen display positioning and appearance. | **YES**  |

### Screen View Settings Parameters

| Parameter         | Description                                                                                         | Required |
|-------------------|-----------------------------------------------------------------------------------------------------|----------|
| `PositionX`       | Horizontal position offset for on-screen display. (**Default**: `-8.0`) | **YES**  |
| `PositionY`       | Vertical position offset for on-screen display. (**Default**: `2.0`) | **YES**  |
| `TitleColor`      | Hex color code for the spectator list title. (**Default**: `"#FFD700"`) | **YES**  |
| `PlayerNameColor` | Hex color code for spectator names. (**Default**: `"#FFFFFF"`) | **YES**  |
| `CountColor`      | Hex color code for spectator count. (**Default**: `"#87CEEB"`) | **YES**  |

### Database Settings Parameters

| Parameter         | Description                                                                                         | Required |
|-------------------|-----------------------------------------------------------------------------------------------------|----------|
| `Host`            | MySQL server hostname or IP address. Leave empty to disable database. (**Default**: `""`) | **NO**   |
| `Port`            | MySQL server port. (**Default**: `3306`) | **NO**   |
| `User`            | MySQL username for database connection. (**Default**: `""`) | **NO**   |
| `Password`        | MySQL password for database connection. (**Default**: `""`) | **NO**   |
| `DatabaseName`    | Name of the MySQL database to use. Leave empty to disable database. (**Default**: `""`) | **NO**   |

---

## Support

For issues, questions, or feature requests, please visit our [GitHub Issues](https://github.com/wiruwiru/SpectatorList-CS2/issues) page.