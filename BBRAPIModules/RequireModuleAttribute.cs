using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBRAPIModules
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireModuleAttribute : Attribute
    {
        public Type ModuleType { get; }

        public RequireModuleAttribute(Type moduleType)
        {
            this.ModuleType = moduleType;
        }
    }
}
