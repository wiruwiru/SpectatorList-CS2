namespace SpectatorList.Models
{
    public class PlayerPreferences
    {
        public string SteamId { get; set; } = string.Empty;
        public bool DisplayEnabled { get; set; } = true;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}