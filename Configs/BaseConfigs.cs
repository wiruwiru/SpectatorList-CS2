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

        [JsonPropertyName("UseScreenView")]
        public bool UseScreenView { get; set; } = true;

        [JsonPropertyName("ScreenViewSettings")]
        public ScreenViewSettings ScreenView { get; set; } = new();
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
}