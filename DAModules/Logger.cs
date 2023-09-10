using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BBRAPIModules;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Logger;

/// <summary>
/// Authors: @jhawker09, @LostGardenGnome
/// Version: 0.1.0
/// </summary>
public class Logger : BattleBitModule
{
    
    public DateTime timestamp;
    public string timestampString;
    
    public Logger()
    {
        timestamp = DateTime.Now;
        timestampString = timestamp.ToString("HH:mm:ss");
    }
    public override async Task OnConnected()
    {
        Console.WriteLine($"{timestampString} | {"T-OC",8} | Connected.");
    }
    public override async Task OnTick()
    {
        
    }
    public override async Task OnSessionChanged(long oldSessionID, long newSessionID)
    {
        Console.WriteLine($"{timestampString} | {"T-OSC",8} | OnSessionChanged: New: {newSessionID} | Old: {oldSessionID}");
    }
    public override async Task OnDisconnected()
    {
        
    }
    public override async Task OnPlayerConnected(RunnerPlayer player)
    {
        Console.WriteLine($"{timestampString} | {"T-OPC",8} | Role: {player.Role}");
    }
    public override async Task OnPlayerDisconnected(RunnerPlayer player)
    {
        Console.WriteLine($"{timestampString} | {"T-OPD",8} | Role: {player.Role}");
    }
    public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
    {
        return true;
    }
    public override async Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args)
    {
        Console.WriteLine($"{timestampString} | {"T-OPJTS",8} | Player: {steamID}");
    }
    public override async Task OnSavePlayerStats(ulong steamID, PlayerStats stats)
    {
        
    }
    public override async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole)
    {
        Console.WriteLine($"{timestampString} | {"T-RCR",8} | Player: {player.ToString()} | Requested Role: {requestedRole}");
        return true;
    }
    public override async Task<bool> OnPlayerRequestingToChangeTeam(RunnerPlayer player, Team requestedTeam)
    {
        
        return true;
    }
    public override async Task OnPlayerChangedRole(RunnerPlayer player, GameRole role)
    {
        Console.WriteLine($"{timestampString} | {"T-OPCR",8} | Player: {player.ToString()} | Role: {role}");
    }
    public override async Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
    {
        Console.WriteLine($"{timestampString} | {"T-PJS",8} | Player: {player.ToString()} | Role: {player.Role}");
    }
    public override async Task OnPlayerLeftSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
    {
        
    }
    public override async Task OnPlayerChangeTeam(RunnerPlayer player, Team team)
    {
        Console.WriteLine($"{timestampString} | {"T-CT",8} | ChangeTeam: {player.Role.ToString()}");
    }
    public override async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
    {
        return request;
    }
    public override async Task OnPlayerSpawned(RunnerPlayer player)
    {
        
    }
    public override async Task OnPlayerDied(RunnerPlayer player)
    {
        
    }
    public override async Task OnPlayerGivenUp(RunnerPlayer player)
    {
        
    }
    public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<RunnerPlayer> args)
    {
        
    }
    public override async Task OnAPlayerRevivedAnotherPlayer(RunnerPlayer from, RunnerPlayer to)
    {
        
    }
    public override async Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional)
    {
        
    }
    public override async Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        Console.WriteLine($"{timestampString} | {"T-GSC",8} | New State: {newState.ToString()} | Old State: {oldState.ToString()}");
        Console.WriteLine("---------------------------------------------------------------------");
    }
    public override async Task OnRoundStarted()
    {
        Console.WriteLine($"{timestampString} | {"T-ORS",8} | On Round Started");
    }
    public override async Task OnRoundEnded()
    {
        Console.WriteLine($"{timestampString} | {"T-ORE",8} | On Round Ended");
        Console.WriteLine("---------------------------------------------------------------------");
    }
    public override async Task OnSquadPointsChanged(Squad<RunnerPlayer> squad, int newPoints)
    {
        
    }
    public override async Task OnSquadLeaderChanged(Squad<RunnerPlayer> squad, RunnerPlayer newLeader)
    {
        
    }
}