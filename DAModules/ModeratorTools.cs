using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

/// <summary>
/// Author: @RainOrigami
/// Version: 0.4.9
/// </summary>

[RequireModule(typeof(CommandHandler))]
public class ModeratorTools : BattleBitModule
{
    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }

    public override void OnModulesLoaded()
    {
        this.CommandHandler.Register(this);
    }

    public override Task OnConnected()
    {
        Task.Run(playerInspection);

        return Task.CompletedTask;
    }

    private async void playerInspection()
    {
        while (this.IsLoaded && this.Server.IsConnected)
        {
            foreach (KeyValuePair<RunnerPlayer, RunnerPlayer> inspection in this.inspectPlayers)
            {
                RunnerPlayer target = inspection.Value;

                StringBuilder playerInfo = new();
                playerInfo.AppendLine($"{target.Name} ({target.SteamID} - {target.Role}");
                playerInfo.AppendLine($"Net: {target.IP} - {target.PingMs}ms");
                playerInfo.AppendLine($"Game: {target.Team} - {target.SquadName} - {(target.IsConnected ? "Connected" : "Disconnected")}");
                playerInfo.AppendLine($"Health: {target.HP} - {(target.IsAlive ? "Alive" : "Dead")} - {(target.IsDown ? "Down" : "Up")} - {(target.IsBleeding ? "Bleeding" : "Not bleeding")}");
                playerInfo.AppendLine($"State: {target.StandingState} - {target.LeaningState} - {(target.InVehicle ? "In vehicle" : "Not in vehicle")}");
                playerInfo.AppendLine($"Position: {target.Position}");
                playerInfo.AppendLine($"Loadout: {target.CurrentLoadout.PrimaryWeapon.ToolName} - {target.CurrentLoadout.SecondaryWeapon.ToolName} - {target.CurrentLoadout.ThrowableName}");
                playerInfo.AppendLine($"Loadout: {target.CurrentLoadout.HeavyGadgetName} - {target.CurrentLoadout.LightGadgetName}");

                inspection.Key.Message(playerInfo.ToString());
            }

            await Task.Delay(250);
        }
    }

    [CommandCallback("Say", Description = "Prints a message to all players", AllowedRoles = Roles.Moderator)]
    public void Say(RunnerPlayer commandSource, string message)
    {
        this.Server.SayToAllChat(message);
    }

    [CommandCallback("SayToPlayer", Description = "Prints a message to all players", AllowedRoles = Roles.Moderator)]
    public void SayToPlayer(RunnerPlayer commandSource, RunnerPlayer target, string message)
    {
        this.Server.SayToChat(message, target.SteamID);
    }

    [CommandCallback("AnnounceShort", Description = "Prints a short announce to all players", AllowedRoles = Roles.Moderator)]
    public void AnnounceShort(RunnerPlayer commandSource, string message)
    {
        this.Server.AnnounceShort(message);
    }

    [CommandCallback("AnnounceLong", Description = "Prints a long announce to all players", AllowedRoles = Roles.Moderator)]
    public void AnnounceLong(RunnerPlayer commandSource, string message)
    {
        this.Server.AnnounceLong(message);
    }

    [CommandCallback("Message", Description = "Messages a specific player", AllowedRoles = Roles.Moderator)]
    public void Message(RunnerPlayer commandSource, RunnerPlayer target, string message, float? timeout = null)
    {
        if (timeout.HasValue)
        {
            target.Message(message, timeout.Value);
        }
        else
        {
            target.Message(message);
        }

        commandSource.Message($"Message sent to {target.Name}", 10);
    }

    [CommandCallback("Clear", Description = "Clears the chat", AllowedRoles = Roles.Moderator)]
    public void Clear(RunnerPlayer commandSource)
    {
        this.Server.SayToAllChat("".PadLeft(30, '\n') + "<size=0%>Chat cleared");
    }

    [CommandCallback("Kick", Description = "Kicks a player", AllowedRoles = Roles.Moderator)]
    public void Kick(RunnerPlayer commandSource, RunnerPlayer target, string? reason = null)
    {
        target.Kick(reason ?? string.Empty);

        commandSource.Message($"Player {target.Name} kicked", 10);
    }

    [CommandCallback("Ban", Description = "Bans a player", AllowedRoles = Roles.Moderator)]
    public void Ban(RunnerPlayer commandSource, RunnerPlayer target)
    {
        this.Server.ExecuteCommand($"ban {target.SteamID}");
        target.Kick();

        commandSource.Message($"Player {target.Name} banned", 10);
    }

    [CommandCallback("Kill", Description = "Kills a player", AllowedRoles = Roles.Moderator)]
    public void Kill(RunnerPlayer commandSource, RunnerPlayer target, string? message = null)
    {
        target.Kill();

        commandSource.Message($"Player {target.Name} killed", 10);

        if (!string.IsNullOrEmpty(message))
        {
            target.Message(message);
        }
    }

    [CommandCallback("Gag", Description = "Gags a player", AllowedRoles = Roles.Moderator)]
    public void Gag(RunnerPlayer commandSource, RunnerPlayer target, string? message = null)
    {
        if (this.gaggedPlayers.Contains(target.SteamID))
        {
            commandSource.Message($"Player {target.Name} is already gagged");
            return;
        }

        this.gaggedPlayers.Add(target.SteamID);

        commandSource.Message($"Player {target.Name} gagged", 10);

        if (!string.IsNullOrEmpty(message))
        {
            target.Message(message);
        }
    }

    [CommandCallback("Ungag", Description = "Ungags a player", AllowedRoles = Roles.Moderator)]
    public void Ungag(RunnerPlayer commandSource, RunnerPlayer target, string? message = null)
    {
        if (!this.gaggedPlayers.Contains(target.SteamID))
        {
            commandSource.Message($"Player {target.Name} is not gagged");
            return;
        }

        this.gaggedPlayers.Remove(target.SteamID);

        commandSource.Message($"Player {target.Name} ungagged", 10);

        if (!string.IsNullOrEmpty(message))
        {
            target.Message(message);
        }
    }

    [CommandCallback("Mute", Description = "Mutes a player", AllowedRoles = Roles.Moderator)]
    public void Mute(RunnerPlayer commandSource, RunnerPlayer target, string? message = null)
    {
        if (target.Modifications.IsVoiceChatMuted)
        {
            commandSource.Message($"Player {target.Name} is already muted");
            return;
        }

        target.Modifications.IsVoiceChatMuted = true;

        commandSource.Message($"Player {target.Name} muted", 10);

        if (!string.IsNullOrEmpty(message))
        {
            target.Message(message);
        }
    }

    [CommandCallback("Unmute", Description = "Unmutes a player", AllowedRoles = Roles.Moderator)]
    public void Unmute(RunnerPlayer commandSource, RunnerPlayer target, string? message = null)
    {
        if (!target.Modifications.IsVoiceChatMuted)
        {
            commandSource.Message($"Player {target.Name} is not muted");
            return;
        }

        target.Modifications.IsVoiceChatMuted = false;

        commandSource.Message($"Player {target.Name} unmuted", 10);

        if (!string.IsNullOrEmpty(message))
        {
            target.Message(message);
        }
    }

    [CommandCallback("Silence", Description = "Mutes and gags a player", AllowedRoles = Roles.Moderator)]
    public void Silence(RunnerPlayer commandSource, RunnerPlayer target, string? message = null)
    {
        Mute(commandSource, target);
        Gag(commandSource, target);
        commandSource.Message($"Player {target.Name} silenced", 10);

        if (!string.IsNullOrEmpty(message))
        {
            target.Message(message);
        }
    }

    [CommandCallback("Unsilence", Description = "Unmutes and ungags a player", AllowedRoles = Roles.Moderator)]
    public void Unsilence(RunnerPlayer commandSource, RunnerPlayer target, string? message = null)
    {
        Unmute(commandSource, target);
        Ungag(commandSource, target);
        commandSource.Message($"Player {target.Name} unsilenced", 10);

        if (!string.IsNullOrEmpty(message))
        {
            target.Message(message);
        }
    }

    [CommandCallback("LockSpawn", Description = "Prevents a player or all players from spawning", AllowedRoles = Roles.Moderator)]
    public void LockSpawn(RunnerPlayer commandSource, RunnerPlayer? target = null, string? message = null)
    {
        if (target == null)
        {
            this.globalSpawnLock = true;
            foreach (RunnerPlayer player in this.Server.AllPlayers)
            {
                player.Modifications.CanDeploy = false;
            }
            commandSource.Message("Spawn globally locked", 10);
        }
        else
        {
            if (this.lockedSpawns.Contains(target.SteamID))
            {
                commandSource.Message($"Spawn already locked for {target.Name}", 10);
                return;
            }

            target.Modifications.CanDeploy = false;
            this.lockedSpawns.Add(target.SteamID);
            commandSource.Message($"Spawn locked for {target.Name}", 10);

            if (!string.IsNullOrEmpty(message))
            {
                target.Message(message);
            }
        }
    }

    [CommandCallback("UnlockSpawn", Description = "Allows a player or all players to spawn", AllowedRoles = Roles.Moderator)]
    public void UnlockSpawn(RunnerPlayer commandSource, RunnerPlayer? target = null, string? message = null)
    {
        if (target == null)
        {
            this.globalSpawnLock = false;
            foreach (RunnerPlayer player in this.Server.AllPlayers)
            {
                player.Modifications.CanDeploy = true;
            }
            commandSource.Message("Spawn globally unlocked", 10);
        }
        else
        {
            if (!this.lockedSpawns.Contains(target.SteamID))
            {
                commandSource.Message($"Spawn already unlocked for {target.Name}", 10);
                return;
            }
            this.lockedSpawns.Remove(target.SteamID);
            commandSource.Message($"Spawn unlocked for {target.Name}", 10);

            if (!string.IsNullOrEmpty(message))
            {
                target.Message(message);
            }
        }
    }

    [CommandCallback("tp2me", Description = "Teleports a player to you", AllowedRoles = Roles.Moderator)]
    public void TeleportPlayerToMe(RunnerPlayer commandSource, RunnerPlayer target)
    {
        target.Teleport(new Vector3((int)commandSource.Position.X, (int)commandSource.Position.Y, (int)commandSource.Position.Z));
    }

    [CommandCallback("tpme2", Description = "Teleports you to a player", AllowedRoles = Roles.Moderator)]
    public void TeleportMeToPlayer(RunnerPlayer commandSource, RunnerPlayer target)
    {
        commandSource.Teleport(new Vector3((int)target.Position.X, (int)target.Position.Y, (int)target.Position.Z));
    }

    [CommandCallback("tp", Description = "Teleports a player to another player", AllowedRoles = Roles.Moderator)]
    public void TeleportPlayerToPlayer(RunnerPlayer commandSource, RunnerPlayer target, RunnerPlayer destination)
    {
        target.Teleport(new Vector3((int)destination.Position.X, (int)destination.Position.Y, (int)destination.Position.Z));
    }

    [CommandCallback("tp2pos", Description = "Teleports a player to a position", AllowedRoles = Roles.Moderator)]
    public void TeleportPlayerToPos(RunnerPlayer commandSource, RunnerPlayer target, int x, int y, int z)
    {
        target.Teleport(new Vector3(x, y, z));
    }

    [CommandCallback("tpme2pos", Description = "Teleports you to a position", AllowedRoles = Roles.Moderator)]
    public void TeleportMeToPos(RunnerPlayer commandSource, int x, int y, int z)
    {
        commandSource.Teleport(new Vector3(x, y, z));
    }

    private Dictionary<RunnerPlayer, RunnerPlayer> inspectPlayers = new();

    [CommandCallback("Inspect", Description = "Inspects a player or stops inspection", AllowedRoles = Roles.Moderator)]
    public void Inspect(RunnerPlayer commandSource, RunnerPlayer? target = null)
    {
        if (target is null)
        {
            this.inspectPlayers.Remove(commandSource);
            commandSource.Message("Inspection stopped", 2);
            return;
        }

        if (this.inspectPlayers.ContainsKey(commandSource))
        {
            this.inspectPlayers[commandSource] = target;
        }
        else
        {
            this.inspectPlayers.Add(commandSource, target);
        }
    }

    private List<ulong> gaggedPlayers = new();
    private List<ulong> lockedSpawns = new();
    private bool globalSpawnLock = false;

    public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
    {
        if (this.gaggedPlayers.Contains(player.SteamID))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public override Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
    {
        if (this.globalSpawnLock || this.lockedSpawns.Contains(player.SteamID))
        {
            return Task.FromResult<OnPlayerSpawnArguments?>(null);
        }

        return Task.FromResult(request as OnPlayerSpawnArguments?);
    }
}
