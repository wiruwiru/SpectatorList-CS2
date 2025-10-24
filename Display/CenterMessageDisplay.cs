using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

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
        private string? _currentMessage;
        private Listeners.OnTick? _onTickHandler;

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

                _currentMessage = BuildHtmlMessage(spectators);

                if (!_isDisplaying)
                {
                    _onTickHandler = OnTickUpdate;
                    _plugin.RegisterListener(_onTickHandler);
                    _isDisplaying = true;
                }

                if (_config.Display.CenterMessageDuration > 0)
                {
                    _hideTimer = _plugin.AddTimer(_config.Display.CenterMessageDuration, HideDisplay);
                }
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error showing center message: {ex.Message}");
            }
        }

        private void OnTickUpdate()
        {
            if (!_isDisplaying || !_player.IsValid || string.IsNullOrEmpty(_currentMessage))
                return;

            _player.PrintToCenterHtml(_currentMessage);
        }

        private string BuildHtmlMessage(List<CCSPlayerController> spectators)
        {
            string titleText = _plugin.Localizer["spectators_title", spectators.Count];
            titleText = titleText.Replace("[SpectatorList]", "").Trim();
            string escapedTitle = System.Net.WebUtility.HtmlEncode(titleText);

            int maxToShow = Math.Min(spectators.Count, _config.Display.MaxNamesInMessage);
            var spectatorNames = new List<string>();

            for (int i = 0; i < maxToShow; i++)
            {
                var spectator = spectators[i];
                if (spectator.IsValid && !string.IsNullOrEmpty(spectator.PlayerName))
                {
                    string escapedName = System.Net.WebUtility.HtmlEncode(spectator.PlayerName);
                    spectatorNames.Add(escapedName);
                }
            }

            string spectatorsList = string.Join(", ", spectatorNames);

            if (spectators.Count > maxToShow)
            {
                int remaining = spectators.Count - maxToShow;
                string andMoreText = _plugin.Localizer["and_more", remaining];
                andMoreText = andMoreText.Replace("[SpectatorList]", "").Trim();
                string escapedMore = System.Net.WebUtility.HtmlEncode(andMoreText);
                spectatorsList += $", {escapedMore}";
            }

            string html = _config.Display.CenterMessageHtml;
            html = html.Replace("{TITLE}", escapedTitle);
            html = html.Replace("{SPECTATORS}", spectatorsList);
            html = html.Replace("{COUNT}", spectators.Count.ToString());

            return html;
        }

        public void HideDisplay()
        {
            if (!_isDisplaying)
                return;

            try
            {
                _hideTimer?.Kill();
                _hideTimer = null;

                if (_onTickHandler != null)
                {
                    _plugin.RemoveListener(_onTickHandler);
                    _onTickHandler = null;
                }

                _currentMessage = null;
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

            if (_onTickHandler != null)
            {
                _onTickHandler = null;
            }

            HideDisplay();
        }
    }
}