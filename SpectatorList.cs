using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;

using SpectatorList.Configs;

namespace SpectatorList;

[MinimumApiVersion(318)]
public class SpectatorList : BasePlugin, IPluginConfig<SpectatorConfig>
{
    public override string ModuleName => "SpectatorList";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "luca.uy";
    public override string ModuleDescription => "Toggle spectator list display via chat messages";

    public SpectatorConfig Config { get; set; } = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _updateTimer;
    private Dictionary<int, List<string>> _lastSpectatorLists = new();

    public void OnConfigParsed(SpectatorConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        foreach (var command in Config.Commands)
        {
            AddCommand(command, "Toggle spectator list display", OnSpectatorListCommand);
        }

        StartUpdateTimer();
    }

    public override void Unload(bool hotReload)
    {
        _updateTimer?.Kill();
        _updateTimer = null;
    }

    private void StartUpdateTimer()
    {
        _updateTimer?.Kill();
        _updateTimer = AddTimer(Config.Update.CheckInterval, () =>
        {
            CheckAndUpdateSpectatorLists();
        }, TimerFlags.REPEAT);
    }

    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    private void OnSpectatorListCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid)
            return;

        if (!string.IsNullOrEmpty(Config.CommandPermissions) && !AdminManager.PlayerHasPermissions(player, Config.CommandPermissions))
        {
            commandInfo.ReplyToCommand($"{Localizer["prefix"]} {Localizer["no_permissions"]}");
            return;
        }

        var spectators = GetPlayersSpectating(player);
        DisplaySpectatorList(player, spectators);
    }

    private void CheckAndUpdateSpectatorLists()
    {
        var alivePlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive).ToList();

        foreach (var player in alivePlayers)
        {
            var currentSpectators = GetPlayersSpectating(player);
            var currentSpectatorNames = currentSpectators.Select(s => s.PlayerName).ToList();

            bool hasChanged = false;

            if (_lastSpectatorLists.ContainsKey(player.Slot))
            {
                var lastList = _lastSpectatorLists[player.Slot];
                hasChanged = !currentSpectatorNames.SequenceEqual(lastList);
            }
            else
            {
                hasChanged = currentSpectatorNames.Count > 0;
            }

            _lastSpectatorLists[player.Slot] = currentSpectatorNames;

            if (hasChanged && Config.Update.ShowOnChange && currentSpectators.Count > 0)
            {
                DisplaySpectatorList(player, currentSpectators);
            }
        }

        if (Config.Update.ShowPeriodic)
        {
            ShowPeriodicSpectatorLists();
        }
    }

    private void ShowPeriodicSpectatorLists()
    {
        bool ShouldShowPeriodic()
        {
            return Server.CurrentTime % Config.Update.PeriodicInterval < Config.Update.CheckInterval;
        }

        if (!ShouldShowPeriodic())
            return;

        var alivePlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive).ToList();

        foreach (var player in alivePlayers)
        {
            var spectators = GetPlayersSpectating(player);
            if (spectators.Count > 0)
            {
                DisplaySpectatorList(player, spectators);
            }
        }
    }

    private List<CCSPlayerController> GetPlayersSpectating(CCSPlayerController targetPlayer)
    {
        var spectators = new List<CCSPlayerController>();

        if (targetPlayer?.PlayerPawn?.Value == null)
            return spectators;

        var allPlayers = Utilities.GetPlayers();

        foreach (var player in allPlayers)
        {
            if (!player.IsValid || player.Slot == targetPlayer.Slot)
                continue;

            if (player.PlayerPawn?.Value != null)
            {
                var observerServices = player.PlayerPawn.Value.ObserverServices;
                if (observerServices != null)
                {
                    var observerTarget = observerServices.ObserverTarget;
                    if (observerTarget?.Value?.Handle == targetPlayer.PlayerPawn.Value.Handle)
                    {
                        spectators.Add(player);
                        continue;
                    }
                }
            }

            if (player.ObserverPawn?.Value != null)
            {
                var observerServices = player.ObserverPawn.Value.ObserverServices;
                if (observerServices != null)
                {
                    var observerTarget = observerServices.ObserverTarget;
                    if (observerTarget?.Value?.Handle == targetPlayer.PlayerPawn.Value.Handle)
                    {
                        spectators.Add(player);
                        continue;
                    }
                }
            }
        }

        return spectators;
    }

    private void DisplaySpectatorList(CCSPlayerController player, List<CCSPlayerController> spectators)
    {
        var spectatorNames = spectators.Select(s => s.PlayerName).ToList();
        var spectatorCount = spectators.Count;

        if (spectatorNames.Count > Config.Display.MaxNamesInMessage)
        {
            var remainingCount = spectatorNames.Count - Config.Display.MaxNamesInMessage;
            spectatorNames = spectatorNames.Take(Config.Display.MaxNamesInMessage).ToList();
            spectatorNames.Add(Localizer["and_more", remainingCount]);
        }

        var spectatorList = string.Join(", ", spectatorNames);
        var chatMessage = $"{Localizer["prefix"]} {Localizer["spectators_watching", spectatorCount, spectatorList]}";
        player.PrintToChat(chatMessage);
    }
}