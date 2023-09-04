using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BBRAPIModules;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Testing;

public class Testing : BattleBitModule
{
        public override async Task OnConnected()
        {
            Console.WriteLine("T-OC | Connected.");
        }
        public override async Task OnTick()
        {
            
        }
        public override async Task OnSessionChanged(long oldSessionID, long newSessionID)
        {
            Console.WriteLine("T-OSC | OnSessionChanged: " + oldSessionID + " | " + newSessionID);
        }
        public override async Task OnDisconnected()
        {
            
        }
        public override async Task OnPlayerConnected(RunnerPlayer player)
        {
            
        }
        public override async Task OnPlayerDisconnected(RunnerPlayer player)
        {
            
        }
        public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            return true;
        }
        public override async Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args)
        {
            
        }
        public override async Task OnSavePlayerStats(ulong steamID, PlayerStats stats)
        {
            
        }
        public override async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole)
        {
            Console.WriteLine("T-RCR | Player: " + player.ToString() + " Requested Role: " + requestedRole);
            return true;
        }
        public override async Task<bool> OnPlayerRequestingToChangeTeam(RunnerPlayer player, Team requestedTeam)
        {
            
            return true;
        }
        public override async Task OnPlayerChangedRole(RunnerPlayer player, GameRole role)
        {
            
        }
        public override async Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
        {
             Console.WriteLine("T-PJS| Player: " + player.ToString() + " Role: " + player.Role);
        }
        public override async Task OnPlayerLeftSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
        {
            
        }
        public override async Task OnPlayerChangeTeam(RunnerPlayer player, Team team)
        {
            Console.WriteLine("T-CT | ChangeTeam: " + player.Role.ToString());
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
            Console.WriteLine("T-GSC | Old State: " + oldState.ToString() + " | New State: " + newState.ToString());
        }
        public override async Task OnRoundStarted()
        {
            Console.WriteLine("T-ORS | On Round Started");
        }
        public override async Task OnRoundEnded()
        {
            Console.WriteLine("T-ORE | On Round Ended");
        }
        public override async Task OnSquadPointsChanged(Squad<RunnerPlayer> squad, int newPoints)
        {
            
        }
        public override async Task OnSquadLeaderChanged(Squad<RunnerPlayer> squad, RunnerPlayer newLeader)
        {
            
        }
}