using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;

using SpectatorList.Configs;
using SpectatorList.Managers;

namespace SpectatorList;

[MinimumApiVersion(318)]
public class SpectatorList : BasePlugin, IPluginConfig<SpectatorConfig>
{
    public override string ModuleName => "SpectatorList";
    public override string ModuleVersion => "1.0.2";
    public override string ModuleAuthor => "luca.uy";
    public override string ModuleDescription => "Toggle spectator list display via chat messages with ScreenView support and exclusion flags";

    public SpectatorConfig Config { get; set; } = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _updateTimer;
    private Dictionary<int, List<string>> _lastSpectatorLists = new();
    private DisplayManager? _displayManager;

    public void OnConfigParsed(SpectatorConfig config)
    {
        Config = config;

        _displayManager?.Dispose();
        _displayManager = new DisplayManager(Config, this);
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        _displayManager?.CleanupAllDisplays();
        _lastSpectatorLists.Clear();
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _displayManager?.CleanupAllDisplays();
        _lastSpectatorLists.Clear();
        return HookResult.Continue;
    }

    public override void Load(bool hotReload)
    {
        _displayManager = new DisplayManager(Config, this);

        foreach (var command in Config.Commands)
        {
            AddCommand(command, "Toggle spectator list display", OnSpectatorListCommand);
        }

        StartUpdateTimer();

        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
    }

    public override void Unload(bool hotReload)
    {
        _updateTimer?.Kill();
        _updateTimer = null;

        _displayManager?.Dispose();
        _displayManager = null;
    }

    private void StartUpdateTimer()
    {
        _updateTimer?.Kill();
        _updateTimer = AddTimer(Config.Update.CheckInterval, CheckAndUpdateSpectatorLists, TimerFlags.REPEAT);
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

        if (!string.IsNullOrEmpty(Config.CanViewList) && !AdminManager.PlayerHasPermissions(player, Config.CanViewList))
        {
            commandInfo.ReplyToCommand($"{Localizer["prefix"]} {Localizer["no_permissions"]}");
            return;
        }

        if (_displayManager == null)
        {
            commandInfo.ReplyToCommand($"{Localizer["prefix"]} Display manager not initialized");
            return;
        }

        _ = HandleToggleCommand(player, commandInfo);
    }

    private async Task HandleToggleCommand(CCSPlayerController player, CommandInfo commandInfo)
    {
        try
        {
            if (_displayManager == null)
            {
                Server.NextFrame(() =>
                {
                    if (player.IsValid)
                    {
                        player.PrintToChat($"{Localizer["prefix"]} Display manager not initialized");
                    }
                });
                return;
            }

            await _displayManager.TogglePlayerDisplayAsync(player);

            bool isEnabled = await _displayManager.IsPlayerDisplayEnabledAsync(player);
            string message = isEnabled ? Localizer["spectator_display_enabled"] : Localizer["spectator_display_disabled"];

            Server.NextFrame(() =>
            {
                try
                {
                    if (player.IsValid)
                    {
                        player.PrintToChat($"{Localizer["prefix"]} {message}");
                    }
                }
                catch (Exception ex)
                {
                    Server.PrintToConsole($"[SpectatorList] Error sending response: {ex.Message}");
                }
            });

            if (isEnabled)
            {
                Server.NextFrame(() =>
                {
                    try
                    {
                        if (player.IsValid && _displayManager != null)
                        {
                            var spectators = GetPlayersSpectating(player);
                            if (spectators.Count > 0)
                            {
                                _ = _displayManager.DisplaySpectatorListAsync(player, spectators);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Server.PrintToConsole($"[SpectatorList] Error displaying spectators: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Server.NextFrame(() =>
            {
                Server.PrintToConsole($"[SpectatorList] Error in HandleToggleCommand: {ex.Message}");
                if (player.IsValid)
                {
                    player.PrintToChat($"{Localizer["prefix"]} {Localizer["database_error"]}");
                }
            });
        }
    }

    private void CheckAndUpdateSpectatorLists()
    {
        if (_displayManager == null) return;

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

            if (hasChanged && Config.Update.ShowOnChange)
            {
                if (currentSpectators.Count > 0)
                {
                    _ = _displayManager.DisplaySpectatorListAsync(player, currentSpectators);
                }
                else
                {
                    _displayManager.CleanupPlayerDisplay(player);
                }
            }
        }

        var alivePlayerSlots = alivePlayers.Select(p => p.Slot).ToHashSet();
        var slotsToRemove = _lastSpectatorLists.Keys.Where(slot => !alivePlayerSlots.Contains(slot)).ToList();
        foreach (var slot in slotsToRemove)
        {
            _lastSpectatorLists.Remove(slot);
        }

        if (Config.Update.ShowPeriodic)
        {
            _ = ShowPeriodicSpectatorLists();
        }
    }

    private async Task ShowPeriodicSpectatorLists()
    {
        if (_displayManager == null) return;

        bool ShouldShowPeriodic()
        {
            return Server.CurrentTime % Config.Update.PeriodicInterval < Config.Update.CheckInterval;
        }

        if (!ShouldShowPeriodic())
            return;

        var alivePlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive).ToList();
        var tasks = new List<Task>();

        foreach (var player in alivePlayers)
        {
            var spectators = GetPlayersSpectating(player);
            if (spectators.Count > 0)
            {
                tasks.Add(_displayManager.DisplaySpectatorListAsync(player, spectators));
            }
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Server.NextFrame(() =>
            {
                Server.PrintToConsole($"[SpectatorList] Error in ShowPeriodicSpectatorLists: {ex.Message}");
            });
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

            if (player.PawnIsAlive)
                continue;

            bool isSpectating = false;
            if (player.PlayerPawn?.Value != null)
            {
                var observerServices = player.PlayerPawn.Value.ObserverServices;
                if (observerServices != null)
                {
                    var observerTarget = observerServices.ObserverTarget;
                    if (observerTarget?.Value?.Handle == targetPlayer.PlayerPawn.Value.Handle)
                    {
                        isSpectating = true;
                    }
                }
            }

            if (!isSpectating && player.ObserverPawn?.Value != null)
            {
                var observerServices = player.ObserverPawn.Value.ObserverServices;
                if (observerServices != null)
                {
                    var observerTarget = observerServices.ObserverTarget;
                    if (observerTarget?.Value?.Handle == targetPlayer.PlayerPawn.Value.Handle)
                    {
                        isSpectating = true;
                    }
                }
            }

            if (isSpectating)
            {
                spectators.Add(player);
            }
        }

        return spectators;
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null && player.IsValid)
        {
            _displayManager?.OnPlayerDisconnect(player);
            _lastSpectatorLists.Remove(player.Slot);
            Server.NextFrame(UpdateAllSpectatorLists);
        }
        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null && player.IsValid)
        {
            _displayManager?.CleanupPlayerDisplay(player);
            Server.NextFrame(UpdateAllSpectatorLists);
        }
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null && player.IsValid)
        {
            _displayManager?.CleanupPlayerDisplay(player);
            Server.NextFrame(UpdateAllSpectatorLists);
        }
        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null && player.IsValid)
        {
            Server.NextFrame(UpdateAllSpectatorLists);
        }
        return HookResult.Continue;
    }

    private void UpdateAllSpectatorLists()
    {
        if (_displayManager == null) return;

        var alivePlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive).ToList();
        var allPlayers = Utilities.GetPlayers().Where(p => p.IsValid).ToList();

        foreach (var player in allPlayers)
        {
            if (!player.PawnIsAlive)
            {
                _displayManager.CleanupPlayerDisplay(player);
            }
        }

        var tasks = new List<Task>();

        foreach (var player in alivePlayers)
        {
            var currentSpectators = GetPlayersSpectating(player);
            if (currentSpectators.Count > 0)
            {
                tasks.Add(_displayManager.DisplaySpectatorListAsync(player, currentSpectators));
            }
            else
            {
                _displayManager.CleanupPlayerDisplay(player);
            }

            var currentSpectatorNames = currentSpectators.Select(s => s.PlayerName).ToList();
            _lastSpectatorLists[player.Slot] = currentSpectatorNames;
        }

        var alivePlayerSlots = alivePlayers.Select(p => p.Slot).ToHashSet();
        var slotsToRemove = _lastSpectatorLists.Keys.Where(slot => !alivePlayerSlots.Contains(slot)).ToList();
        foreach (var slot in slotsToRemove)
        {
            _lastSpectatorLists.Remove(slot);
        }

        if (tasks.Count > 0)
        {
            _ = Task.WhenAll(tasks).ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    Server.NextFrame(() =>
                    {
                        Server.PrintToConsole($"[SpectatorList] Error in UpdateAllSpectatorLists: {t.Exception.Message}");
                    });
                }
            });
        }
    }
}