using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
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
        private CPointOrient? _pointOrient;
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

                _pointOrient = CreateOrGetPointOrient();
                if (_pointOrient == null)
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

        private CPointOrient? CreateOrGetPointOrient()
        {
            if (_pointOrient != null && _pointOrient.IsValid)
                return _pointOrient;

            var pawn = GetPlayerPawn();
            if (pawn == null)
                return null;

            CPointOrient? entOrient = Utilities.CreateEntityByName<CPointOrient>("point_orient");
            if (entOrient == null || !entOrient.IsValid)
                return null;

            entOrient.Active = true;
            entOrient.GoalDirection = PointOrientGoalDirectionType_t.eEyesForward;
            entOrient.DispatchSpawn();

            Vector vecPos = new Vector(
                pawn.AbsOrigin!.X,
                pawn.AbsOrigin!.Y,
                pawn.AbsOrigin!.Z + pawn.ViewOffset.Z
            );
            entOrient.Teleport(vecPos, null, null);
            entOrient.AcceptInput("SetParent", pawn, null, "!activator");
            entOrient.AcceptInput("SetTarget", pawn, null, "!activator");

            return entOrient;
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
            if (_pointOrient == null || !_pointOrient.IsValid)
                return null;

            var angle = _pointOrient.AbsRotation;
            if (angle == null)
                return null;

            AngleVectors(angle, out Vector forward, out Vector right, out Vector up);

            Vector offset = forward * 7 + right * _config.Display.ScreenView.PositionX + up * _config.Display.ScreenView.PositionY;

            QAngle displayAngle = new()
            {
                Y = angle.Y + 270,
                Z = 90 - angle.X,
                X = 0
            };

            Vector position = _pointOrient.AbsOrigin! + offset;
            return (position, displayAngle);
        }

        private static void AngleVectors(QAngle angles, out Vector forward, out Vector right, out Vector up)
        {
            float angle = angles.Y * (MathF.PI * 2 / 360);
            float sy = MathF.Sin(angle);
            float cy = MathF.Cos(angle);

            angle = angles.X * (MathF.PI * 2 / 360);
            float sp = MathF.Sin(angle);
            float cp = MathF.Cos(angle);

            angle = angles.Z * (MathF.PI * 2 / 360);
            float sr = MathF.Sin(angle);
            float cr = MathF.Cos(angle);

            forward = new Vector(cp * cy, cp * sy, -sp);
            right = new Vector((-1 * sr * sp * cy) + (-1 * cr * -sy), (-1 * sr * sp * sy) + (-1 * cr * cy), -1 * sr * cp);
            up = new Vector((cr * sp * cy) + (-sr * -sy), (cr * sp * sy) + (-sr * cy), cr * cp);
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
            entity.AcceptInput("SetParent", _pointOrient, null, "!activator");

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

                if (_pointOrient?.IsValid == true)
                {
                    _pointOrient.Remove();
                }
                _pointOrient = null;

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