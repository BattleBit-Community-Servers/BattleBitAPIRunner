using BattleBitAPI.Common;
using BBRAPIModules;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;


namespace BattleBitBaseModules;

/// <summary>
/// Author: @RainOrigami
/// Version: 0.4.10
/// </summary>
/// 
public class BasicServerSettings : BattleBitModule
{
    public BasicServerSettingsConfiguration Configuration { get; set; }

    public override Task OnConnected()
    {
        this.applyServerSettings();

        foreach (RunnerPlayer player in this.Server.AllPlayers)
        {
            this.applyPlayerSettings(player);
        }

        return Task.CompletedTask;
    }

    public override Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        if (oldState == newState)
        {
            return Task.CompletedTask;
        }

        this.applyRoundSettings(newState);

        return Task.CompletedTask;
    }

    public override Task OnPlayerConnected(RunnerPlayer player)
    {
        this.applyPlayerSettings(player);

        return Task.CompletedTask;
    }

    private void applyServerSettings()
    {
        this.Server.ServerSettings.APCSpawnDelayMultipler = this.Configuration.APCSpawnDelayMultipler ?? this.Server.ServerSettings.APCSpawnDelayMultipler;
        this.Server.ServerSettings.CanVoteDay = this.Configuration.CanVoteDay ?? this.Server.ServerSettings.CanVoteDay;
        this.Server.ServerSettings.CanVoteNight = this.Configuration.CanVoteNight ?? this.Server.ServerSettings.CanVoteNight;
        this.Server.ServerSettings.DamageMultiplier = this.Configuration.DamageMultiplier ?? this.Server.ServerSettings.DamageMultiplier;
        this.Server.ServerSettings.EngineerLimitPerSquad = this.Configuration.EngineerLimitPerSquad ?? this.Server.ServerSettings.EngineerLimitPerSquad;
        this.Server.ServerSettings.FriendlyFireEnabled = this.Configuration.FriendlyFireEnabled ?? this.Server.ServerSettings.FriendlyFireEnabled;
        this.Server.ServerSettings.HelicopterSpawnDelayMultipler = this.Configuration.HelicopterSpawnDelayMultipler ?? this.Server.ServerSettings.HelicopterSpawnDelayMultipler;
        this.Server.ServerSettings.MedicLimitPerSquad = this.Configuration.MedicLimitPerSquad ?? this.Server.ServerSettings.MedicLimitPerSquad;
        this.Server.ServerSettings.OnlyWinnerTeamCanVote = this.Configuration.OnlyWinnerTeamCanVote ?? this.Server.ServerSettings.OnlyWinnerTeamCanVote;
        this.Server.ServerSettings.PlayerCollision = this.Configuration.PlayerCollision ?? this.Server.ServerSettings.PlayerCollision;
        this.Server.ServerSettings.ReconLimitPerSquad = this.Configuration.ReconLimitPerSquad ?? this.Server.ServerSettings.ReconLimitPerSquad;
        this.Server.ServerSettings.SeaVehicleSpawnDelayMultipler = this.Configuration.SeaVehicleSpawnDelayMultipler ?? this.Server.ServerSettings.SeaVehicleSpawnDelayMultipler;
        this.Server.ServerSettings.SupportLimitPerSquad = this.Configuration.SupportLimitPerSquad ?? this.Server.ServerSettings.SupportLimitPerSquad;
        this.Server.ServerSettings.TankSpawnDelayMultipler = this.Configuration.TankSpawnDelayMultipler ?? this.Server.ServerSettings.TankSpawnDelayMultipler;
        this.Server.ServerSettings.TransportSpawnDelayMultipler = this.Configuration.TransportSpawnDelayMultipler ?? this.Server.ServerSettings.TransportSpawnDelayMultipler;
        this.Server.ServerSettings.UnlockAllAttachments = this.Configuration.UnlockAllAttachments ?? this.Server.ServerSettings.UnlockAllAttachments;
        this.Server.ServerSettings.TeamlessMode = this.Configuration.TeamlessMode ?? this.Server.ServerSettings.TeamlessMode;
    }

    private void applyRoundSettings(GameState gameState)
    {
        if (!this.Configuration.RoundSettings.ContainsKey(gameState))
        {
            return;
        }

        var roundSettings = this.Configuration.RoundSettings[gameState];

        this.Server.RoundSettings.MaxTickets = roundSettings.MaxTickets ?? this.Server.RoundSettings.MaxTickets;
        this.Server.RoundSettings.PlayersToStart = roundSettings.PlayersToStart ?? this.Server.RoundSettings.PlayersToStart;
        this.Server.RoundSettings.SecondsLeft = roundSettings.SecondsLeft ?? this.Server.RoundSettings.SecondsLeft;
        this.Server.RoundSettings.TeamATickets = roundSettings.TeamATickets ?? this.Server.RoundSettings.TeamATickets;
        this.Server.RoundSettings.TeamBTickets = roundSettings.TeamBTickets ?? this.Server.RoundSettings.TeamBTickets;
    }

    private void applyPlayerSettings(RunnerPlayer player)
    {
        player.Modifications.AirStrafe = this.Configuration.AirStrafe ?? player.Modifications.AirStrafe;
        player.Modifications.AllowedVehicles = this.Configuration.AllowedVehicles ?? player.Modifications.AllowedVehicles;
        player.Modifications.CanDeploy = this.Configuration.CanDeploy ?? player.Modifications.CanDeploy;
        player.Modifications.CanSpectate = this.Configuration.CanSpectate ?? player.Modifications.CanSpectate;
        player.Modifications.CanSuicide = this.Configuration.CanSuicide ?? player.Modifications.CanSuicide;
        player.Modifications.CanUseNightVision = this.Configuration.CanUseNightVision ?? player.Modifications.CanUseNightVision;
        player.Modifications.CaptureFlagSpeedMultiplier = this.Configuration.CaptureFlagSpeedMultiplier ?? player.Modifications.CaptureFlagSpeedMultiplier;
        player.Modifications.DownTimeGiveUpTime = this.Configuration.DownTimeGiveUpTime ?? player.Modifications.DownTimeGiveUpTime;
        player.Modifications.FallDamageMultiplier = this.Configuration.FallDamageMultiplier ?? player.Modifications.FallDamageMultiplier;
        player.Modifications.FriendlyHUDEnabled = this.Configuration.FriendlyHUDEnabled ?? player.Modifications.FriendlyHUDEnabled;
        player.Modifications.GiveDamageMultiplier = this.Configuration.GiveDamageMultiplier ?? player.Modifications.GiveDamageMultiplier;
        player.Modifications.HitMarkersEnabled = this.Configuration.HitMarkersEnabled ?? player.Modifications.HitMarkersEnabled;
        player.Modifications.HpPerBandage = this.Configuration.HpPerBandage ?? player.Modifications.HpPerBandage;
        player.Modifications.IsExposedOnMap = this.Configuration.IsExposedOnMap ?? player.Modifications.IsExposedOnMap;
        player.Modifications.IsTextChatMuted = this.Configuration.IsTextChatMuted ?? player.Modifications.IsTextChatMuted;
        player.Modifications.IsVoiceChatMuted = this.Configuration.IsVoiceChatMuted ?? player.Modifications.IsVoiceChatMuted;
        player.Modifications.JumpHeightMultiplier = this.Configuration.JumpHeightMultiplier ?? player.Modifications.JumpHeightMultiplier;
        player.Modifications.KillFeed = this.Configuration.KillFeed ?? player.Modifications.KillFeed;
        player.Modifications.MinimumDamageToStartBleeding = this.Configuration.MinimumDamageToStartBleeding ?? player.Modifications.MinimumDamageToStartBleeding;
        player.Modifications.MinimumHpToStartBleeding = this.Configuration.MinimumHpToStartBleeding ?? player.Modifications.MinimumHpToStartBleeding;
        player.Modifications.PointLogHudEnabled = this.Configuration.PointLogHudEnabled ?? player.Modifications.PointLogHudEnabled;
        player.Modifications.ReceiveDamageMultiplier = this.Configuration.ReceiveDamageMultiplier ?? player.Modifications.ReceiveDamageMultiplier;
        player.Modifications.ReloadSpeedMultiplier = this.Configuration.ReloadSpeedMultiplier ?? player.Modifications.ReloadSpeedMultiplier;
        player.Modifications.RespawnTime = this.Configuration.RespawnTime ?? player.Modifications.RespawnTime;
        player.Modifications.RunningSpeedMultiplier = this.Configuration.RunningSpeedMultiplier ?? player.Modifications.RunningSpeedMultiplier;
        player.Modifications.SpawningRule = this.Configuration.SpawningRule ?? player.Modifications.SpawningRule;
        player.Modifications.StaminaEnabled = this.Configuration.StaminaEnabled ?? player.Modifications.StaminaEnabled;
        player.Modifications.HideOnMap = this.Configuration.HideOnMap ?? player.Modifications.HideOnMap;
        player.Modifications.Freeze = this.Configuration.Freeze ?? player.Modifications.Freeze;
        player.Modifications.ReviveHP = this.Configuration.ReviveHP ?? player.Modifications.ReviveHP;
    }
}
public class BasicServerSettingsConfiguration : ModuleConfiguration
{
    // Server
    public float? APCSpawnDelayMultipler { get; set; } = null;
    public float? HelicopterSpawnDelayMultipler { get; set; } = null;
    public float? SeaVehicleSpawnDelayMultipler { get; set; } = null;
    public float? TankSpawnDelayMultipler { get; set; } = null;
    public float? TransportSpawnDelayMultipler { get; set; } = null;
    public bool? CanVoteDay { get; set; } = null;
    public bool? CanVoteNight { get; set; } = null;
    public float? DamageMultiplier { get; set; } = null;
    public byte? EngineerLimitPerSquad { get; set; } = null;
    public byte? MedicLimitPerSquad { get; set; } = null;
    public byte? ReconLimitPerSquad { get; set; } = null;
    public byte? SupportLimitPerSquad { get; set; } = null;
    public bool? FriendlyFireEnabled { get; set; } = null;
    public bool? OnlyWinnerTeamCanVote { get; set; } = null;
    public bool? PlayerCollision { get; set; } = null;
    public bool? UnlockAllAttachments { get; set; } = null;
    public bool? TeamlessMode { get; set; } = null;

    // Player
    public bool? AirStrafe { get; set; } = null;
    public VehicleType? AllowedVehicles { get; set; } = null;
    public bool? CanDeploy { get; set; } = null;
    public bool? CanSpectate { get; set; } = null;
    public bool? CanSuicide { get; set; } = null;
    public bool? CanUseNightVision { get; set; } = null;
    public float? CaptureFlagSpeedMultiplier { get; set; } = null;
    public float? DownTimeGiveUpTime { get; set; } = null;
    public float? FallDamageMultiplier { get; set; } = null;
    public bool? FriendlyHUDEnabled { get; set; } = null;
    public float? GiveDamageMultiplier { get; set; } = null;
    public bool? HitMarkersEnabled { get; set; } = null;
    public float? HpPerBandage { get; set; } = null;
    public bool? IsExposedOnMap { get; set; } = null;
    public bool? IsTextChatMuted { get; set; } = null;
    public bool? IsVoiceChatMuted { get; set; } = null;
    public float? JumpHeightMultiplier { get; set; } = null;
    public bool? KillFeed { get; set; } = null;
    public float? MinimumDamageToStartBleeding { get; set; } = null;
    public float? MinimumHpToStartBleeding { get; set; } = null;
    public bool? PointLogHudEnabled { get; set; } = null;
    public float? ReceiveDamageMultiplier { get; set; } = null;
    public float? ReloadSpeedMultiplier { get; set; } = null;
    public float? RespawnTime { get; set; } = null;
    public float? RunningSpeedMultiplier { get; set; } = null;
    public SpawningRule? SpawningRule { get; set; } = null;
    public bool? StaminaEnabled { get; set; } = null;
    public bool? HideOnMap { get; set; } = null;
    public bool? Freeze { get; set; } = null;
    public float? ReviveHP { get; set; } = null;

    public ReadOnlyDictionary<GameState, RoundSettingsConfiguration> RoundSettings = new(new Dictionary<GameState, RoundSettingsConfiguration>()
    {
        { GameState.WaitingForPlayers, new() },
        { GameState.CountingDown, new() },
        { GameState.Playing, new() },
        { GameState.EndingGame, new() }
    });
}

public class RoundSettingsConfiguration
{
    public double? MaxTickets { get; set; } = null;
    public int? PlayersToStart { get; set; } = null;
    public int? SecondsLeft { get; set; } = null;
    public double? TeamATickets { get; set; } = null;
    public double? TeamBTickets { get; set; } = null;
}