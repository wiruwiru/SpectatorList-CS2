using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace SpectatorList.Configs
{
    public class SpectatorConfig : BasePluginConfig
    {
        [JsonPropertyName("Commands")]
        public List<string> Commands { get; set; } = new List<string> { "css_speclist", "css_specs", "css_spectators" };

        [JsonPropertyName("CommandPermissions")]
        public string CommandPermissions { get; set; } = "@css/root";

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

        [JsonPropertyName("MaxNamesInMessage")]
        public int MaxNamesInMessage { get; set; } = 5;
    }
}