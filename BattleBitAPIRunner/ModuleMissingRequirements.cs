using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleBitAPIRunner
{
    internal class ModuleMissingRequirements
    {
        public ModuleMissingRequirements(Module module, Type[] missingModules)
        {
            this.Module = module;
            this.MissingModules = missingModules;
        }

        public Module Module { get; }
        public Type[] MissingModules { get; }
    }
}
