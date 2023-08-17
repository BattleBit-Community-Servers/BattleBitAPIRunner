using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBRAPIModules
{
    public class ModuleConfiguration
    {
        public void Load()
        {
            this.OnLoadingRequest?.Invoke(this, EventArgs.Empty);
        }

        public void Save()
        {
            this.OnSavingRequest?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? OnLoadingRequest;
        public event EventHandler? OnSavingRequest;
    }
}
