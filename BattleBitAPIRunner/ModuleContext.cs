using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace BattleBitAPIRunner
{
    internal class ModuleContext
    {
        public AssemblyLoadContext Context { get; set; }
        public Type Module { get; set; }

        public ModuleContext(AssemblyLoadContext context, Type module)
        {
            this.Context = context;
            this.Module = module;
        }
    }
}
