using BattleBitAPI.Common;
using BattleBitAPIRunner;
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
                this.Server.SayToChat("pong");
            }

            // Since modules can be loaded and unloaded at runtime, it's recommended to check for the module every time it's used.
            ExampleModuleIntegration.Example? integratedModule = this.Server.GetModule<ExampleModuleIntegration.Example>();
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