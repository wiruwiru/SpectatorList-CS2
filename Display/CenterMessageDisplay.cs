using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Text;

using SpectatorList.Configs;

namespace SpectatorList.Display
{
    public class CenterMessageDisplay : IDisposable
    {
        private readonly CCSPlayerController _player;
        private readonly SpectatorConfig _config;
        private readonly BasePlugin _plugin;
        private bool _isDisplaying = false;
        private CounterStrikeSharp.API.Modules.Timers.Timer? _hideTimer;

        public CenterMessageDisplay(CCSPlayerController player, SpectatorConfig config, BasePlugin plugin)
        {
            _player = player;
            _config = config;
            _plugin = plugin;
        }

        public void ShowSpectatorList(List<CCSPlayerController> spectators)
        {
            if (!_config.Display.UseCenterMessage || !_player.IsValid || spectators.Count == 0)
                return;

            try
            {
                _hideTimer?.Kill();
                _hideTimer = null;

                string message = BuildMessage(spectators);
                _player.PrintToCenter(message);
                _isDisplaying = true;

                if (_config.Display.CenterMessageDuration > 0)
                {
                    _hideTimer = _plugin.AddTimer(_config.Display.CenterMessageDuration, () =>
                    {
                        HideDisplay();
                    });
                }
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error showing center message: {ex.Message}");
            }
        }

        private string BuildMessage(List<CCSPlayerController> spectators)
        {
            var sb = new StringBuilder();

            string titleText = _plugin.Localizer["spectators_title", spectators.Count];
            titleText = titleText.Replace("[SpectatorList]", "").Trim();

            sb.Append($"{titleText}\n");

            int maxToShow = Math.Min(spectators.Count, _config.Display.MaxNamesInMessage);
            var spectatorNames = new List<string>();

            for (int i = 0; i < maxToShow; i++)
            {
                var spectator = spectators[i];
                if (spectator.IsValid && !string.IsNullOrEmpty(spectator.PlayerName))
                {
                    spectatorNames.Add(spectator.PlayerName);
                }
            }

            if (spectatorNames.Count > 0)
            {
                sb.Append(string.Join(", ", spectatorNames));
            }

            if (spectators.Count > maxToShow)
            {
                int remaining = spectators.Count - maxToShow;
                string andMoreText = _plugin.Localizer["and_more", remaining];
                andMoreText = andMoreText.Replace("[SpectatorList]", "").Trim();
                sb.Append($", {andMoreText}");
            }

            return sb.ToString().TrimEnd();
        }

        public void HideDisplay()
        {
            if (!_isDisplaying)
                return;

            try
            {
                _hideTimer?.Kill();
                _hideTimer = null;

                if (_player.IsValid)
                {
                    _player.PrintToCenter("");
                }
                _isDisplaying = false;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error hiding center message: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _hideTimer?.Kill();
            _hideTimer = null;
            HideDisplay();
        }
    }
}