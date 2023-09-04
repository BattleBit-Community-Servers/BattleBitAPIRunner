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
/// Author: @RainOrigami
/// Version: 0.4.10
/// </summary>
/// 

public class SniperTeamLimit : BattleBitModule
{
    // Setting sniper count tracking variables
    public int team_a_sniper_count = 0;
    public int team_b_sniper_count = 0;
    public int team_sniper_limit = 0;
    
    //Set array of mapsize to team sniper limit
    public Dictionary<string, int> maps_to_limits = new Dictionary<string, int>
    {
        {"_8v8", 1},
        {"_16vs16", 2},
        {"_32vs32", 4},
        {"_64vs64", 8},
        {"_127vs127", 16}
    };
    //
 
    public override Task OnConnected()
    // Determine the sniper count based map size
    {   
        team_sniper_limit = maps_to_limits[this.Server.MapSize.ToString()];
        Console.WriteLine("OC | Server size is " + this.Server.MapSize.ToString() + " setting sniper limit to " + team_sniper_limit);
        team_a_sniper_count = 0;
        team_b_sniper_count = 0;
        Console.WriteLine("OC | Team A: " + team_a_sniper_count + " | Team B: " + team_b_sniper_count);

    // // Determing starting sniper count at the beginning of the round for each team
    //     Console.WriteLine("Round Starting... Checking sniper count");
    //     foreach (var player in this.Server.AllPlayers){
    //         string team_name = player.Team.ToString();
            
    //         if (player.Role.ToString() == "Recon" & team_name == "TeamA"){
    //             team_a_sniper_count++;
    //         }
    //         else if (player.Role.ToString() == "Recon" & team_name == "TeamB"){
    //             team_b_sniper_count++;
    //         }
    //     }
    //     Console.WriteLine("Team A: " + team_a_sniper_count + "/" + team_sniper_limit);
    //     Console.WriteLine("Team B: " + team_b_sniper_count + "/" + team_sniper_limit);

        return Task.CompletedTask;
    }
        //
        //
    public override async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole)
    // Check the role the player is requesting (requestedRole) and their current role (player.Role) if either one is a recon
    // we either increment or decrement the appropriate team sniper count. If the role change will exceed the team sniper
    // limit for the map size, then returns false. If it is below the limit return true.
    {
        if (requestedRole == GameRole.Recon){
            string team_name = player.Team.ToString();
            int sniper_count = (team_name == "TeamA") ? team_a_sniper_count : team_b_sniper_count;

            Console.WriteLine("RCR | " + player.ToString() + " is requesting sniper");
            if (sniper_count < team_sniper_limit){
                sniper_count++;
                if (team_name == "TeamA"){
                    team_a_sniper_count = sniper_count;
                    Console.WriteLine("RCR | Team A: " + team_a_sniper_count + "/" + team_sniper_limit);
                }
                else if (team_name == "TeamB"){
                    team_b_sniper_count = sniper_count;
                    Console.WriteLine("RCR | Team B: " + team_b_sniper_count + "/" + team_sniper_limit);
                }
                Console.WriteLine("RCR | Team A: " + team_a_sniper_count + " | Team B: " + team_b_sniper_count);
                return true;
            }
            else{
                Console.WriteLine("RCR | " + team_name + " is at the sniper limit");
                Console.WriteLine("RCR | Team A: " + team_a_sniper_count + " | Team B: " + team_b_sniper_count);
                return false;
            }
        }
        
        else if (player.Role == GameRole.Recon){
            string team_name = player.Team.ToString();
            int sniper_count = (team_name == "TeamA") ? team_a_sniper_count : team_b_sniper_count;
            // Review this
            if (sniper_count > 0){
                sniper_count--;
                if (team_name == "TeamA"){
                    team_a_sniper_count = sniper_count;
                    Console.WriteLine("RCR | Team A: " + team_a_sniper_count + "/" + team_sniper_limit);
                }
                else if (team_name == "TeamB"){
                    team_b_sniper_count = sniper_count;
                    Console.WriteLine("RCR | Team B: " + team_b_sniper_count + "/" + team_sniper_limit);
                }
                Console.WriteLine("RCR | Team A: " + team_a_sniper_count + " | Team B: " + team_b_sniper_count);
                return true;
            }
        }
        // Catch all return true for players not going to or from Recon class
        return true;
    }
    
    public override async Task OnGameStateChanged(GameState oldState, GameState newState){
        //if (newState == GameState.WaitingForPlayers || newState == GameState.CountingDown){
            Console.WriteLine("GSC | " + newState.ToString() + " | " + oldState.ToString());
            team_sniper_limit = maps_to_limits[this.Server.MapSize.ToString()];
            Console.WriteLine("GSC | Server size is " + this.Server.MapSize.ToString() + " setting sniper limit to " + team_sniper_limit);
            team_a_sniper_count = 0;
            team_b_sniper_count = 0;
            Console.WriteLine("GSC | Team A: " + team_a_sniper_count + " | Team B: " + team_b_sniper_count);
        //}
        return;
    }
    public override async Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad){
            player.SetNewRole(GameRole.Assault);
            Console.WriteLine("Role: " + GameRole.Assault);
        return;
    }

}
