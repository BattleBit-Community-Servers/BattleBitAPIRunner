using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

/// <summary>
/// Author: @RainOrigami modified by @_dx2
/// Version: 0.4.7
/// </summary>

public class MapRotation : BattleBitModule
{
    public MapRotationConfiguration Configuration { get; set; }

    public override Task OnConnected()
    {
        this.Server.MapRotation.SetRotation(this.Configuration.Maps);
        return Task.CompletedTask;
    }

    public override Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        if (newState == GameState.CountingDown)
        {
            if (Configuration.MatchesSinceSelection.Length != Configuration.Maps.Length)
            {
                ReinicializeCounters();
            }
            Configuration.MatchesSinceSelection[Array.IndexOf(Configuration.Maps, Server.Map)] = 0;

            var mapsThisRound = this.Configuration.Maps.Zip(Configuration.MatchesSinceSelection)
                .OrderByDescending(map => map.Second).ToList()
                .GetRange(0, this.Configuration.MapCountInRotation).ConvertAll(m => m.First).ToArray();

            this.Server.MapRotation.SetRotation(mapsThisRound);
            for (int i = 0; i < Configuration.MatchesSinceSelection.Length; i++)
            {
                Configuration.MatchesSinceSelection[i]++;
            }
        }
        return Task.CompletedTask;
    }

    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }

    public override void OnModulesLoaded()
    {
        this.CommandHandler.Register(this);
        if (Configuration.MatchesSinceSelection.Length != Configuration.Maps.Length)
        {
            ReinicializeCounters();
        }
    }

    private void ReinicializeCounters()
    {
        Configuration.MatchesSinceSelection = new int[Configuration.Maps.Length];
        Random r = new();
        for (int i = 0; i < Configuration.Maps.Length; i++)
        {
            Configuration.MatchesSinceSelection[i] = r.Next(5);
        }
    }

    [CommandCallback("Maps", Description = "Shows current rotation")]
    public void Maps(RunnerPlayer commandSource)
    {
        string maps = "";
        foreach (var map in Configuration.Maps)
        {
            maps += map + ", ";
        }
        Server.MessageToPlayer(commandSource, $"Current map rotation is: {maps}");
    }
    /*//use for debugging
    [CommandCallback("M", Description = "Shows how many matches since the last time a map was played")]
    public void M(RunnerPlayer commandSource)
    {
        string maps = "";
        int i = 0;
        foreach (var map in Configuration.Maps)
        {
            maps += map + " " + matchesSinceSelection[i] + ", ";
            i++;
        }
        Server.SayToChat($"maps played and times since last played: {maps}", commandSource);
    }*/
}

public class MapRotationConfiguration : ModuleConfiguration
{
    public int MapCountInRotation { get; set; } = 8;
    public int[] MatchesSinceSelection { get; set; } = new int[1];
    public string[] Maps { get; set; } = new[]
    {
        "Azagor",
        "Basra",
        "Construction",
        "District",
        "Dustydew",
        "Eduardovo",
        "Frugis",
        "Isle",
        "Lonovo",
        "MultuIslands",
        "Namak",
        "OilDunes",
        "River",
        "Salhan",
        "SandySunset",
        "TensaTown",
        "Valley",
        "Wakistan",
        "WineParadise"
    };
}