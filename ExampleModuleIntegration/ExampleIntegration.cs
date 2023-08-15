using BBRAPIModules;

namespace ExampleModuleIntegration
{
    public class ExampleIntegration : BattleBitModule
    {
        public ExampleIntegration(RunnerServer server) : base(server)
        {
        }

        public void DoSomething()
        {
            this.Server.SayToChat("Something has been done!");
        }
    }
}