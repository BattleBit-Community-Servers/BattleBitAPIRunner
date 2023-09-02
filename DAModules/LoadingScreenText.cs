using BBRAPIModules;
using System.Threading.Tasks;

namespace BattleBitBaseModules;

/// <summary>
/// Author: @RainOrigami
/// Version: 0.4.6
/// </summary>

public class LoadingScreenText : BattleBitModule
{
    public LoadingScreenTextConfiguration Configuration { get; set; }

    public override Task OnConnected()
    {
        this.Server.LoadingScreenText = this.Configuration.LoadingScreenText;

        return Task.CompletedTask;
    }
}

public class LoadingScreenTextConfiguration : ModuleConfiguration
{
    public string LoadingScreenText { get; set; } = "This is a community server!";
}