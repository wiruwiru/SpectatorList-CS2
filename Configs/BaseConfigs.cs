using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace SpectatorList.Configs
{
    public class SpectatorConfig : BasePluginConfig
    {
        [JsonPropertyName("Commands")]
        public List<string> Commands { get; set; } = new List<string> { "css_speclist", "css_specs", "css_spectators" };

        [JsonPropertyName("CommandPermissions")]
        public string CommandPermissions { get; set; } = "@css/vip";

        [JsonPropertyName("CanViewList")]
        public string CanViewList { get; set; } = "@css/vip";

        [JsonPropertyName("UpdateSettings")]
        public UpdateSettings Update { get; set; } = new();

        [JsonPropertyName("DisplaySettings")]
        public DisplaySettings Display { get; set; } = new();

        [JsonPropertyName("StorageSettings")]
        public StorageSettings Storage { get; set; } = new();
    }

    public class UpdateSettings
    {
        [JsonPropertyName("CheckInterval")]
        public float CheckInterval { get; set; } = 2.0f;

        [JsonPropertyName("ShowOnChange")]
        public bool ShowOnChange { get; set; } = true;

        [JsonPropertyName("ShowPeriodic")]
        public bool ShowPeriodic { get; set; } = false;

        [JsonPropertyName("PeriodicInterval")]
        public float PeriodicInterval { get; set; } = 5.0f;
    }

    public class DisplaySettings
    {
        [JsonPropertyName("ExclusionFlag")]
        public string ExclusionFlag { get; set; } = "@css/generic";

        [JsonPropertyName("MaxNamesInMessage")]
        public int MaxNamesInMessage { get; set; } = 5;

        [JsonPropertyName("SendToChat")]
        public bool SendToChat { get; set; } = false;

        [JsonPropertyName("UseCenterMessage")]
        public bool UseCenterMessage { get; set; } = false;

        [JsonPropertyName("CenterMessageType")]
        public string CenterMessageType { get; set; } = "PrintToCenter";

        [JsonPropertyName("CenterMessageDuration")]
        public float CenterMessageDuration { get; set; } = 5.0f;

        [JsonPropertyName("CenterMessageSettings")]
        public CenterMessageSettings CenterMessage { get; set; } = new();

        [JsonPropertyName("UseScreenView")]
        public bool UseScreenView { get; set; } = true;

        [JsonPropertyName("ScreenViewSettings")]
        public ScreenViewSettings ScreenView { get; set; } = new();
    }

    public class CenterMessageSettings
    {
        [JsonPropertyName("UseCustomHtml")]
        public bool UseCustomHtml { get; set; } = false;

        [JsonPropertyName("CustomHtmlTemplate")]
        public string CustomHtmlTemplate { get; set; } = @"<font class='fontSize-l' color='#FFD700'>{TITLE}</font><br><font class='fontSize-m' color='#87CEEB'>{SPECTATORS}</font>";

        [JsonPropertyName("TitleStyle")]
        public string TitleStyle { get; set; } = "fontSize-l stratum-bold";

        [JsonPropertyName("TitleColor")]
        public string TitleColor { get; set; } = "#FFD700";

        [JsonPropertyName("ContentStyle")]
        public string ContentStyle { get; set; } = "fontSize-m stratum-regular";

        [JsonPropertyName("ContentColor")]
        public string ContentColor { get; set; } = "#FFFFFF";

        [JsonPropertyName("SpectatorNameStyle")]
        public string SpectatorNameStyle { get; set; } = "fontSize-m stratum-regular";

        [JsonPropertyName("SpectatorNameColor")]
        public string SpectatorNameColor { get; set; } = "#87CEEB";

        [JsonPropertyName("SeparatorColor")]
        public string SeparatorColor { get; set; } = "#CCCCCC";
    }

    public class ScreenViewSettings
    {
        [JsonPropertyName("PositionX")]
        public float PositionX { get; set; } = -8.0f;

        [JsonPropertyName("PositionY")]
        public float PositionY { get; set; } = 1.0f;

        [JsonPropertyName("TitleColor")]
        public string TitleColor { get; set; } = "#FFD700";

        [JsonPropertyName("PlayerNameColor")]
        public string PlayerNameColor { get; set; } = "#FFFFFF";

        [JsonPropertyName("CountColor")]
        public string CountColor { get; set; } = "#87CEEB";
    }

    public class StorageSettings
    {
        [JsonPropertyName("StorageType")]
        public string StorageType { get; set; } = "PlayerSettings";

        [JsonPropertyName("Database")]
        public DatabaseConfig Database { get; set; } = new DatabaseConfig();
    }

    public class DatabaseConfig
    {
        [JsonPropertyName("Host")]
        public string Host { get; set; } = "";

        [JsonPropertyName("Port")]
        public uint Port { get; set; } = 3306;

        [JsonPropertyName("User")]
        public string User { get; set; } = "root";

        [JsonPropertyName("Password")]
        public string Password { get; set; } = "";

        [JsonPropertyName("DatabaseName")]
        public string DatabaseName { get; set; } = "";
    }
}