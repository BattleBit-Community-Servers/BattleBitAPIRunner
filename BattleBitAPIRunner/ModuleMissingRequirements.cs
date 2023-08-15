using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleBitAPIRunner
{
    internal class ModuleMissingRequirements
    {
        public ModuleMissingRequirements(ModuleContext moduleContext, Type[] missingModules)
        {
            this.ModuleContext = moduleContext;
            this.MissingModules = missingModules;
        }

        public ModuleContext ModuleContext { get; }
        public Type[] MissingModules { get; }
    }
}
