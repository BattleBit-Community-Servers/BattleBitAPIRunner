using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BBRAPIModules
{
    public class ModuleConfiguration
    {
        public void Load()
        {
            this.OnLoadingRequest?.Invoke(this, this.module, this.property, serverName);
        }

        public void Save()
        {
            this.OnSavingRequest?.Invoke(this, this.module, this.property, serverName);
        }

        private BattleBitModule module;
        private PropertyInfo property;
        private string serverName;


        internal void Initialize(BattleBitModule module, PropertyInfo property, string serverName)
        {
            this.module = module;
            this.property = property;
            this.serverName = serverName;
        }

        public delegate void ConfigurationEventHandler(object sender, BattleBitModule module, PropertyInfo property, string serverName);
        public event ConfigurationEventHandler? OnLoadingRequest;
        public event ConfigurationEventHandler? OnSavingRequest;
    }
}
