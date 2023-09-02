using BattleBitAPI.Common;
using BBRAPIModules;
using Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

/// <summary>
/// Author: @RainOrigami
/// Version: 0.4.11
/// </summary>

[RequireModule(typeof(CommandHandler))]
public class MOTD : BattleBitModule
{
    public MOTDConfiguration Configuration { get; set; }

    [ModuleReference]
    public CommandHandler CommandHandler { get; set; }
    
    public override void OnModulesLoaded()
    {
        this.CommandHandler.Register(this);
    }

    private List<ulong> greetedPlayers = new();

    public override Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        if (newState == GameState.EndingGame)
        {
            greetedPlayers.Clear();
            greetedPlayers.AddRange(this.Server.AllPlayers.Select(p => p.SteamID));
        }

        return Task.CompletedTask;
    }

    public override Task OnPlayerConnected(RunnerPlayer player)
    {
        if (this.greetedPlayers.Contains(player.SteamID))
        {
            return Task.CompletedTask;
        }

        this.ShowMOTD(player);

        return Task.CompletedTask;
    }

    [CommandCallback("setmotd", Description = "Sets the MOTD", AllowedRoles = Roles.Admin)]
    public void SetMOTD(RunnerPlayer commandSource, string motd)
    {
        this.Configuration.MOTD = motd;
        this.Configuration.Save();
        this.ShowMOTD(commandSource);
    }

    [CommandCallback("motd", Description = "Shows the MOTD")]
    public void ShowMOTD(RunnerPlayer commandSource)
    {
        commandSource.Message(string.Format(this.Configuration.MOTD, commandSource.Name, commandSource.PingMs, this.Server.ServerName, this.Server.Gamemode, this.Server.Map, this.Server.DayNight, this.Server.MapSize.ToString().Trim('_'), this.Server.CurrentPlayerCount, this.Server.InQueuePlayerCount, this.Server.MaxPlayerCount), this.Configuration.MessageTimeout);
    }
}

public class MOTDConfiguration : ModuleConfiguration
{
    public string MOTD { get; set; } = "Welcome!";
    public int MessageTimeout { get; set; } = 30;
}
