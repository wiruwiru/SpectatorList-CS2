using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Text;

using SpectatorList.Configs;

namespace SpectatorList.Display
{
    public enum CenterMessageType
    {
        Center,
        CenterAlert,
        CenterHtml
    }

    public class CenterMessageDisplay : IDisposable
    {
        private readonly CCSPlayerController _player;
        private readonly SpectatorConfig _config;
        private readonly BasePlugin _plugin;
        private bool _isDisplaying = false;
        private CounterStrikeSharp.API.Modules.Timers.Timer? _hideTimer;
        private CenterMessageType _messageType;
        private string? _currentMessage;
        private Listeners.OnTick? _onTickHandler;

        public CenterMessageDisplay(CCSPlayerController player, SpectatorConfig config, BasePlugin plugin)
        {
            _player = player;
            _config = config;
            _plugin = plugin;
            _messageType = ParseMessageType(config.Display.CenterMessageType);
        }

        private CenterMessageType ParseMessageType(string type)
        {
            return type.ToLower() switch
            {
                "center" => CenterMessageType.Center,
                "centeralert" => CenterMessageType.CenterAlert,
                "centerhtml" => CenterMessageType.CenterHtml,
                _ => CenterMessageType.Center
            };
        }

        public void ShowSpectatorList(List<CCSPlayerController> spectators)
        {
            if (!_config.Display.UseCenterMessage || !_player.IsValid || spectators.Count == 0)
                return;

            try
            {
                _hideTimer?.Kill();
                _hideTimer = null;

                string message = _messageType == CenterMessageType.CenterHtml
                    ? BuildHtmlMessage(spectators)
                    : BuildMessage(spectators);

                _currentMessage = message;

                if (_messageType == CenterMessageType.CenterHtml)
                {
                    if (!_isDisplaying)
                    {
                        _onTickHandler = OnTickUpdate;
                        _plugin.RegisterListener(_onTickHandler);
                    }
                }
                else
                {
                    SendMessage(message);
                }

                _isDisplaying = true;

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
            {
                return;
            }

            if (_player.IsValid)
            {
                _player.PrintToCenterHtml(_currentMessage);
            }
        }

        private void SendMessage(string message)
        {
            switch (_messageType)
            {
                case CenterMessageType.Center:
                    _player.PrintToCenter(message);
                    break;
                case CenterMessageType.CenterAlert:
                    _player.PrintToCenterAlert(message);
                    break;
                case CenterMessageType.CenterHtml:
                    _player.PrintToCenterHtml(message);
                    break;
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

        private string BuildHtmlMessage(List<CCSPlayerController> spectators)
        {
            if (_config.Display.CenterMessage.UseCustomHtml)
            {
                return BuildCustomHtmlMessage(spectators);
            }

            return BuildDefaultHtmlMessage(spectators);
        }

        private string BuildCustomHtmlMessage(List<CCSPlayerController> spectators)
        {
            string titleText = _plugin.Localizer["spectators_title", spectators.Count];
            titleText = titleText.Replace("[SpectatorList]", "").Trim();

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
                spectatorsList += $", {andMoreText}";
            }

            string html = _config.Display.CenterMessage.CustomHtmlTemplate;
            html = html.Replace("{TITLE}", titleText);
            html = html.Replace("{SPECTATORS}", spectatorsList);
            html = html.Replace("{COUNT}", spectators.Count.ToString());

            return html;
        }

        private string BuildDefaultHtmlMessage(List<CCSPlayerController> spectators)
        {
            var sb = new StringBuilder();

            string titleText = _plugin.Localizer["spectators_title", spectators.Count];
            titleText = titleText.Replace("[SpectatorList]", "").Trim();
            string escapedTitle = System.Net.WebUtility.HtmlEncode(titleText);

            // Título
            sb.Append($"<font class='fontSize-m' color='#FFD700'>{escapedTitle}</font>");
            sb.Append("<br>");

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

            if (spectatorNames.Count > 0)
            {
                string namesList = string.Join(", ", spectatorNames);

                if (spectators.Count > maxToShow)
                {
                    int remaining = spectators.Count - maxToShow;
                    string andMoreText = _plugin.Localizer["and_more", remaining];
                    andMoreText = andMoreText.Replace("[SpectatorList]", "").Trim();
                    string escapedMore = System.Net.WebUtility.HtmlEncode(andMoreText);
                    namesList += $", {escapedMore}";
                }

                sb.Append($"<font class='fontSize-s' color='#87CEEB'>{namesList}</font>");
            }

            return sb.ToString();
        }

        public void HideDisplay()
        {
            if (!_isDisplaying)
                return;

            try
            {
                _hideTimer?.Kill();
                _hideTimer = null;

                if (_messageType == CenterMessageType.CenterHtml && _onTickHandler != null)
                {
                    _plugin.RemoveListener(_onTickHandler);
                    _onTickHandler = null;
                }

                if (_player.IsValid)
                {
                    _player.PrintToCenter("");
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
                try
                {
                    _plugin.RemoveListener(_onTickHandler);
                }
                catch
                {
                }
                _onTickHandler = null;
            }

            HideDisplay();
        }
    }
}