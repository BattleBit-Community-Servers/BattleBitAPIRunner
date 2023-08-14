using BattleBitAPIRunner;

namespace ExampleModuleIntegration
{
    public class Example : BattleBitModule
    {
        public Example(RunnerServer server) : base(server)
        {
        }

        public void DoSomething()
        {
            this.Server.SayToChat("Something has been done!");
        }
    }
}