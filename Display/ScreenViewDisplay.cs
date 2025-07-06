using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Text;

using SpectatorList.Configs;

namespace SpectatorList.Display
{
    public class ScreenViewDisplay : IDisposable
    {
        private readonly CCSPlayerController _player;
        private readonly SpectatorConfig _config;
        private readonly BasePlugin _plugin;
        private CPointWorldText? _screenText;
        private CCSGOViewModel? _viewModel;
        private bool _isDisplaying = false;

        public ScreenViewDisplay(CCSPlayerController player, SpectatorConfig config, BasePlugin plugin)
        {
            _player = player;
            _config = config;
            _plugin = plugin;
        }

        public void ShowSpectatorList(List<CCSPlayerController> spectators)
        {
            if (!_config.Display.UseScreenView || !_player.IsValid || spectators.Count == 0)
                return;

            try
            {
                CleanupDisplay();

                _viewModel = EnsureCustomView();
                if (_viewModel == null)
                    return;

                var vectorData = GetPlayerVectorData();
                if (!vectorData.HasValue)
                    return;

                string displayText = BuildDisplayText(spectators);

                _screenText = CreateWorldTextEntity(displayText, vectorData.Value);
                if (_screenText == null)
                    return;

                _isDisplaying = true;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error showing screen view: {ex.Message}");
                CleanupDisplay();
            }
        }

        private string BuildDisplayText(List<CCSPlayerController> spectators)
        {
            var sb = new StringBuilder();

            string title = _plugin.Localizer["spectators_title", spectators.Count];
            sb.AppendLine(title);
            sb.AppendLine();

            foreach (var spectator in spectators)
            {
                if (spectator.IsValid && !string.IsNullOrEmpty(spectator.PlayerName))
                {
                    sb.AppendLine($"• {spectator.PlayerName}");
                }
            }

            return sb.ToString();
        }

        private CCSGOViewModel? EnsureCustomView()
        {
            var pawn = GetPlayerPawn();
            if (pawn?.ViewModelServices == null)
                return null;

            int offset = Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
            IntPtr viewModelHandleAddress = pawn.ViewModelServices.Handle + offset + 4;

            CHandle<CCSGOViewModel> handle = new(viewModelHandleAddress);
            if (!handle.IsValid)
            {
                CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
                if (viewmodel == null) return null;

                viewmodel.DispatchSpawn();
                handle.Raw = viewmodel.EntityHandle.Raw;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_pViewModelServices");
            }

            return handle.Value;
        }

        private CCSPlayerPawn? GetPlayerPawn()
        {
            if (_player.Pawn.Value is not CBasePlayerPawn pawn)
                return null;

            if (pawn.LifeState == (byte)LifeState_t.LIFE_DEAD)
            {
                if (pawn.ObserverServices?.ObserverTarget.Value?.As<CBasePlayerPawn>() is not CBasePlayerPawn observer)
                    return null;
                pawn = observer;
            }

            return pawn.As<CCSPlayerPawn>();
        }

        private (Vector Position, QAngle Angle)? GetPlayerVectorData()
        {
            var playerPawn = GetPlayerPawn();
            if (playerPawn == null)
                return null;

            QAngle eyeAngles = playerPawn.EyeAngles;
            Vector forward = new(), right = new(), up = new();
            NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

            Vector offset = forward * 7 + right * _config.Display.ScreenView.PositionX + up * _config.Display.ScreenView.PositionY;
            QAngle angle = new()
            {
                Y = eyeAngles.Y + 270,
                Z = 90 - eyeAngles.X,
                X = 0
            };

            Vector position = playerPawn.AbsOrigin! + offset + new Vector(0, 0, playerPawn.ViewOffset.Z);
            return (position, angle);
        }

        private CPointWorldText? CreateWorldTextEntity(string text, (Vector Position, QAngle Angle) vectorData)
        {
            CPointWorldText entity = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext")!;
            if (entity == null || !entity.IsValid)
                return null;

            Color titleColor = ParseHexColor(_config.Display.ScreenView.TitleColor);

            entity.MessageText = text;
            entity.Enabled = true;
            entity.FontSize = 30;
            entity.FontName = "Tahoma Bold";
            entity.Fullbright = true;
            entity.Color = titleColor;
            entity.WorldUnitsPerPx = 0.0085f;
            entity.BackgroundWorldToUV = 0.01f;
            entity.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
            entity.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
            entity.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;
            entity.RenderMode = RenderMode_t.kRenderNormal;
            entity.DepthOffset = 0.1f;

            entity.DrawBackground = true;
            entity.BackgroundBorderHeight = 0.1f;
            entity.BackgroundBorderWidth = 0.1f;

            entity.DispatchSpawn();
            entity.Teleport(vectorData.Position, vectorData.Angle, null);
            entity.AcceptInput("SetParent", _viewModel, null, "!activator");

            return entity;
        }

        private Color ParseHexColor(string hexColor)
        {
            try
            {
                if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#"))
                    return Color.White;

                string hex = hexColor.Substring(1);
                if (hex.Length == 6)
                {
                    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
            }
            catch
            {

            }
            return Color.White;
        }

        public void HideDisplay()
        {
            if (!_isDisplaying)
                return;

            CleanupDisplay();
        }

        private void CleanupDisplay()
        {
            try
            {
                if (_screenText?.IsValid == true)
                {
                    _screenText.Remove();
                }
                _screenText = null;
                _isDisplaying = false;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[SpectatorList] Error cleaning up display: {ex.Message}");
            }
        }

        public void Dispose()
        {
            CleanupDisplay();
        }
    }
}