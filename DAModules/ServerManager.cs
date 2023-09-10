using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BBRAPIModules;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Cache;
using Newtonsoft.Json;
using System.IO;


namespace ServerManager;

/// <summary>
/// Author: @jhawker09, @LostGardenGnome
/// Version: 0.4.10
/// </summary>

public class ServerManager : BattleBitModule
{
    public ServerManager(){
        string filePath = @"C:\Games\battlebit\config.json";
        try{
            string jsonContent = File.ReadAllText(filePath);

            Config config = JsonConvert.DeserializeObject<Config>(jsonContent);

            Console.WriteLine($"PlayersToStart: {config.PlayersToStart}");
        }
        catch (Exception e){
            Console.WriteLine($"Error occurred: {e.Message}");
        }
    }
}

public class Config
{
    public int PlayersToStart { get; set; }

}