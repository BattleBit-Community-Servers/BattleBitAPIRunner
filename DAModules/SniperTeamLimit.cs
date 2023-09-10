using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BBRAPIModules;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Cache;


namespace SniperTeamLimit;

/// <summary>
/// Authors: @jhawker09, @LostGardenGnome
/// Version: 0.1.0
/// </summary>

public class SniperTeamLimit : BattleBitModule
{
    // Timestamps
    public DateTime timestamp;
    public string timestampString;
    // Sniper Counts and Limit
    public int team_a_sniper_count, team_b_sniper_count, team_sniper_limit;
    public string GAMESTATE = "";
    //Set array of mapsize to team sniper limit
    public Dictionary<string, int> maps_to_limits = new Dictionary<string, int>
    {
        {"_8v8", 1},
        {"_16vs16", 2},
        {"_32vs32", 4},
        {"_64vs64", 8},
        {"_127vs127", 16}
    };

    public SniperTeamLimit()
    {
        timestamp = DateTime.Now;
        timestampString = timestamp.ToString("HH:mm:ss");
    }
    public override async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole)
    // Check the role the player is requesting (requestedRole) and their current role (player.Role) if either one is a recon
    // we either increment or decrement the appropriate team sniper count. If the role change will exceed the team sniper
    // limit for the map size, then returns false. If it is below the limit return true.
    {
        if (GAMESTATE == "WaitingForPlayers"){
            if (requestedRole == GameRole.Medic){
                Console.WriteLine($"{timestampString} | {"RCR",8} | Denied immediate sniper request");
                return false;
            }
        }
        if (requestedRole == GameRole.Medic){
            string team_name = player.Team.ToString();
            int sniper_count = (team_name == "TeamA") ? team_a_sniper_count : team_b_sniper_count;
            Console.WriteLine($"{timestampString} | {"RCR",8} | {player.ToString()} is requesting sniper");
            if (sniper_count < team_sniper_limit){
                sniper_count++;
                if (team_name == "TeamA"){
                    team_a_sniper_count = sniper_count;
                    Console.WriteLine($"{timestampString} | {"RCR",8} | Team A: {team_a_sniper_count}/{team_sniper_limit}");
                }
                else if (team_name == "TeamB"){
                    team_b_sniper_count = sniper_count;
                    Console.WriteLine($"{timestampString} | {"RCR",8} | Team B: {team_b_sniper_count}/{team_sniper_limit}");
                }
                Console.WriteLine($"{timestampString} | {"RCR",8} | Team A: {team_a_sniper_count} | Team B: {team_b_sniper_count}");
                return true;
            }
            else{
                Console.WriteLine($"{timestampString} | {"RCR",8} | {team_name} is at the sniper limit of {team_sniper_limit}");
                Console.WriteLine($"{timestampString} | {"RCR",8} | Team A: {team_a_sniper_count} | Team B: {team_b_sniper_count}");
                player.Message($"Only {team_sniper_limit} snipers are allowed per team.\nPlease pick a different class.", 5);
                return false;
            }
        }
        
        else if (player.Role == GameRole.Medic){
            string team_name = player.Team.ToString();
            int sniper_count = (team_name == "TeamA") ? team_a_sniper_count : team_b_sniper_count;
            // Review this
            if (sniper_count > 0) {
                sniper_count--;
                if (team_name == "TeamA"){
                    team_a_sniper_count = sniper_count;
                    Console.WriteLine($"{timestampString} | {"RCR",8} | Team A: {team_a_sniper_count}/{team_sniper_limit}");
                }
                else if (team_name == "TeamB"){
                    team_b_sniper_count = sniper_count;
                    Console.WriteLine($"{timestampString} | {"RCR",8} | Team B: {team_b_sniper_count}/{team_sniper_limit}");
                }
                Console.WriteLine($"{timestampString} | {"RCR",8} | Team A: {team_a_sniper_count} | Team B: {team_b_sniper_count}");
                return true;
            }
        }
        // Catch all return true for players not going to or from Medic class
        return true;
    }
    
    public override async Task OnGameStateChanged(GameState oldState, GameState newState){
        // Resets sniper limits and counters on change to WaitingForPlayers state
        GAMESTATE = newState.ToString();
        Console.WriteLine($"{timestampString} | {"GSC",8} | New: {newState.ToString()} | Old: {oldState.ToString()}");
        if (newState == GameState.WaitingForPlayers){
            //this.ChangeToAssault();
            team_sniper_limit = maps_to_limits[this.Server.MapSize.ToString()];
            Console.WriteLine($"{timestampString} | {"GSC",8} | Server size is {this.Server.MapSize.ToString()} setting sniper limit to {team_sniper_limit}");
            team_a_sniper_count = 0;
            team_b_sniper_count = 0;
            Console.WriteLine($"{timestampString} | {"GSC",8} | Team A: {team_a_sniper_count} | Team B: {team_b_sniper_count}");
        }
        // On EndingGame state, force Medics to Assault
        else if (newState == GameState.EndingGame){
            foreach (RunnerPlayer player in this.Server.AllPlayers){
                this.ChangeToAssault(player);
            }
            
        }
        return;
    }
    public override async Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad){
        Console.WriteLine($"{timestampString} | {"PJS",8} | Role: {player.Role}");
        return;
    }
    
    public void ChangeToAssault(RunnerPlayer player){
        if (player.Role == BattleBitAPI.Common.GameRole.Medic){
            Console.WriteLine($"{timestampString} | {"CTA",8} | Role: {player.Role}");
            player.SetNewRole(BattleBitAPI.Common.GameRole.Assault);
            this.Server.SetRoleTo(player.SteamID, BattleBitAPI.Common.GameRole.Assault);
            Console.WriteLine($"{timestampString} | {"CTA",8} | Role Changed: {player.Role}");
        }
        Console.WriteLine($"{timestampString} | {"CTA",8} | Not Medic, Skipping: {player.Role}");
    }
    public override Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args){
        foreach (RunnerPlayer player in this.Server.AllPlayers){
            if (player.SteamID == steamID){
                this.ChangeToAssault(player); 
            }
        }
        return Task.CompletedTask;
    }
    public override async Task OnConnected()
    {
        Console.WriteLine($"{timestampString} | {"OC",8} | Setting initial values.");
        // Setting sniper count tracking variables
        team_sniper_limit = maps_to_limits[this.Server.MapSize.ToString()];
        team_a_sniper_count = 0;
        team_b_sniper_count = 0;
    }

}
