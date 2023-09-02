using BattleBitAPI.Common;
using BBRAPIModules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Permissions;

public class PlayerPermissions : BattleBitModule
{
    public static PlayerPermissionsConfiguration Configuration { get; set; }

    public override Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args)
    {
        args.Stats.Roles = this.GetPlayerRoles(steamID);
        return Task.CompletedTask;
    }

    public override Task OnPlayerConnected(RunnerPlayer player)
    {
        lock (Configuration.PlayerRoles)
        {
            if (!Configuration.PlayerRoles.ContainsKey(player.SteamID))
            {
                Configuration.PlayerRoles.Add(player.SteamID, Roles.None);
            }
        }

        return Task.CompletedTask;
    }

    public bool HasPlayerRole(ulong steamID, Roles role)
    {
        return (this.GetPlayerRoles(steamID) & role) == role;
    }

    public Roles GetPlayerRoles(ulong steamID)
    {
        lock (Configuration.PlayerRoles)
        {
            if (Configuration.PlayerRoles.ContainsKey(steamID))
            {
                return Configuration.PlayerRoles[steamID];
            }
        }

        return Roles.None;
    }

    public void SetPlayerRoles(ulong steamID, Roles roles)
    {
        lock (Configuration.PlayerRoles)
        {
            if (Configuration.PlayerRoles.ContainsKey(steamID))
            {
                Configuration.PlayerRoles[steamID] = roles;
            }
            else
            {
                Configuration.PlayerRoles.Add(steamID, roles);
            }
        }

        Configuration.Save();
    }

    public void AddPlayerRoles(ulong steamID, Roles role)
    {
        this.SetPlayerRoles(steamID, this.GetPlayerRoles(steamID) | role);
    }

    public void RemovePlayerRoles(ulong steamID, Roles role)
    {
        this.SetPlayerRoles(steamID, this.GetPlayerRoles(steamID) & ~role);
    }
}

public class PlayerPermissionsConfiguration : ModuleConfiguration
{
    public Dictionary<ulong, Roles> PlayerRoles { get; set; } = new();
}