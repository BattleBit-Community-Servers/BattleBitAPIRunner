using BattleBitAPI.Common;
using BattleBitAPIRunner;
using Newtonsoft.Json;
using System.Diagnostics;

namespace ExampleModule
{
    public class Example : BattleBitModule
    {
        public Example(RunnerServer server) : base(server)
        {
        }

        public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            if (msg == "ping")
            {
                // Usage of packages is supported
                this.Server.SayToChat(JsonConvert.SerializeObject("pong"));
            }

            // Since modules can be loaded and unloaded at runtime, it's recommended to check for the module every time it's used.
            ExampleModuleIntegration.ExampleIntegration? integratedModule = this.Server.GetModule<ExampleModuleIntegration.ExampleIntegration>();
            if (integratedModule is not null)
            {
                // This could, for example, be Discord webhooks that handles sending messages to Discord.
                // Where a module can send its own messages through the existing webhook module, if available.
                integratedModule.DoSomething();
            }

            return Task.FromResult(true);
        }
    }
}