using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleBitAPIRunner
{
    internal class ModuleDependencyResolver
    {
        private readonly Dictionary<string, string[]> dependencyGraph = new();
        private readonly Dictionary<string, Module> moduleLoaders = new();

        public ModuleDependencyResolver(Module[] moduleLoaders)
        {
            foreach (Module moduleLoader in moduleLoaders)
            {
                this.moduleLoaders.Add(moduleLoader.Name, moduleLoader);
                this.dependencyGraph.Add(moduleLoader.Name, moduleLoader.RequiredDependencies.Union(moduleLoader.OptionalDependencies).Distinct().ToArray());
            }

            foreach (Module moduleLoader in moduleLoaders)
            {
                foreach (string dependency in moduleLoader.OptionalDependencies)
                {
                    if (!this.dependencyGraph.ContainsKey(dependency))
                    {
                        this.dependencyGraph.Add(dependency, Array.Empty<string>());
                    }
                }
            }
        }

        public IEnumerable<Module> GetDependencyOrder()
        {
            HashSet<string> visited = new();
            List<string> order = new();

            foreach (string module in this.dependencyGraph.Keys)
            {
                VisitModule(module, visited, order);
            }

            return order.Where(o => this.moduleLoaders.ContainsKey(o)).Select(o => this.moduleLoaders[o]);
        }

        private void VisitModule(string module, HashSet<string> visited, List<string> order)
        {
            if (visited.Contains(module))
            {
                return;
            }

            visited.Add(module);

            foreach (var dependency in this.dependencyGraph[module])
            {
                VisitModule(dependency, visited, order);
            }

            order.Add(module);
        }
    }

}
