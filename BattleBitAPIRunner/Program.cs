using BattleBitAPI.Server;
using BBRAPIModules;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace BattleBitAPIRunner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new Program();
        }

        private ServerConfiguration configuration = new();
        private List<RunnerServer> servers = new();
        private ServerListener<RunnerPlayer, RunnerServer> serverListener = new();

        public Program()
        {
            loadConfiguration();
            validateConfiguration();
            loadDependencies();
            loadModules();
            hookModules();
            startServerListener();

            consoleCommandHandler();
        }

        private void loadDependencies()
        {
            if (!Directory.Exists(this.configuration.DependencyPath))
            {
                Directory.CreateDirectory(this.configuration.DependencyPath);
            }

            foreach (string dependency in Directory.GetFiles(this.configuration.DependencyPath, "*.dll"))
            {
                Assembly.LoadFrom(dependency);
            }
        }

        private void consoleCommandHandler()
        {
            // TODO: Make proper console handler ncurses style (separate line for input, rest of window for output)
            while (true)
            {
                string command = Console.ReadLine();

                string[] commandParts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (commandParts.Length == 0)
                {
                    continue;
                }

                switch (commandParts[0])
                {
                    case "servers":
                        foreach (RunnerServer server in this.servers)
                        {
                            Console.WriteLine($"{server.GameIP}:{server.GamePort} - {server.IsConnected}");
                        }
                        break;
                    case "list":
                        foreach (Module module in Module.Modules)
                        {
                            Console.WriteLine(module.Name);
                        }
                        break;
                    case "unload":
                    case "load":
                        if (commandParts.Length < 2)
                        {
                            Console.WriteLine("Usage: load/unload <module name>");
                            continue;
                        }

                        if (commandParts[0].Equals("load", StringComparison.OrdinalIgnoreCase))
                        {
                            tryLoadModuleFromName(commandParts[1]);
                        }
                        else
                        {
                            tryUnloadModuleFromName(commandParts[1]);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void tryUnloadModuleFromName(string name)
        {
            Module? module = Module.Modules.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (module is null)
            {
                Console.WriteLine($"Module {name} not found");
                return;
            }

            this.unloadModuleFromServers(module);
            module.Unload();

            Console.WriteLine($"Module {module.Name} unloaded.");
        }

        private void tryLoadModuleFromName(string name)
        {
            Module? module = Module.Modules.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (module is not null)
            {
                unloadModuleFromServers(module);
                module.Unload();
            }

            try
            {
                if (module is null)
                {
                    module = new Module(Path.Combine(this.configuration.ModulesPath, $"{name}.cs"));
                }
                else
                {
                    module.Reload();
                }

                foreach (string dependency in module.Dependencies)
                {
                    if (Module.Modules.FirstOrDefault(m => m.Name.Equals(dependency, StringComparison.OrdinalIgnoreCase)) is null)
                    {
                        throw new Exception($"Module {name} requires module {dependency} which is not loaded.");
                    }
                }

                module.Compile();
                module.Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load module {(module?.Name ?? name)}: {ex}");
                return;
            }

            this.loadModuleToServers(module, true);

            Console.WriteLine($"Module {module.Name} loaded.");
        }

        private void unloadModuleFromServers(Module module)
        {
            // TODO: requires unloading of dependant modules as well

            foreach (RunnerServer server in this.servers)
            {
                BattleBitModule? moduleInstance = server.GetModule(module.ModuleType);
                if (moduleInstance is null)
                {
                    continue;
                }

                server.RemoveModule(moduleInstance);
            }
        }

        private void loadModuleToServers(Module module, bool immediateStart = false)
        {
            foreach (RunnerServer server in this.servers)
            {
                try
                {
                    BattleBitModule moduleInstance = Activator.CreateInstance(module.ModuleType, server) as BattleBitModule;
                    if (moduleInstance is null)
                    {
                        // this can't really happen
                        throw new Exception($"Module {module.Name} does not inherit from {nameof(BattleBitModule)}");
                    }
                    server.AddModule(moduleInstance);

                    if (immediateStart)
                    {
                        Task.Run(async () =>
                        {
                            moduleInstance.OnModulesLoaded();
                            if (server.IsConnected)
                            {
                                await moduleInstance.OnConnected();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {module.Context.Name} for server {server.GameIP}:{server.GamePort}: {ex}");
                }
            }
        }

        private void loadModules()
        {
            Module[] modules = Directory.GetFiles(this.configuration.ModulesPath, "*.cs").Union(this.configuration.Modules).Select(m =>
            {
                try { return new Module(m); }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {Path.GetFileName(m)}: {ex}");
                    return null;
                }
            }).Where(m => m is not null).Select(m => m!).ToArray();

            Module[] sortedModules = new ModuleDependencyResolver(modules).GetDependencyOrder().ToArray();

            foreach (Module module in sortedModules)
            {
                try
                {
                    module.Compile();
                    module.Load();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {Path.GetFileName(module.Name)}: {ex}");
                    continue;
                }

                Console.WriteLine($"Loaded module {module.Name}");
            }

            Console.WriteLine($"{Module.Modules.Count} modules loaded.");
        }

        private void hookModules()
        {
            this.serverListener.OnCreatingGameServerInstance = initializeGameServer;
        }

        private Task initializeGameServer(GameServer<RunnerPlayer> server)
        {
            this.servers.Add((RunnerServer)server);

            List<BattleBitModule> battleBitModules = new();

            foreach (Module moduleContext in Module.Modules)
            {
                try
                {
                    BattleBitModule module = Activator.CreateInstance(moduleContext.ModuleType, server) as BattleBitModule;
                    if (module is null)
                    {
                        throw new Exception($"Module {moduleContext.Context.Name} does not inherit from {nameof(BattleBitModule)}");
                    }
                    ((RunnerServer)server).AddModule(module);
                    battleBitModules.Add(module);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {moduleContext.Context.Name}: {ex}");
                }
            }

            foreach (BattleBitModule battleBitModule in battleBitModules)
            {
                battleBitModule.OnModulesLoaded();
            }

            return Task.CompletedTask;
        }

        private void startServerListener()
        {
            this.serverListener.Start(this.configuration.IPAddress, this.configuration.Port!.Value);
            Console.WriteLine($"Listener started at {this.configuration.IPAddress}:{this.configuration.Port.Value}");
        }

        private void loadConfiguration()
        {
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build()
                .Bind(this.configuration);
        }

        private void validateConfiguration()
        {
            List<ValidationResult> validationResults = new();
            IPAddress? ipAddress = null;
            if (!Validator.TryValidateObject(this.configuration, new ValidationContext(this.configuration), validationResults, true) || !IPAddress.TryParse(this.configuration.IP, out ipAddress))
            {
                StringBuilder error = new();
                error.AppendLine($"Invalid configuration:{Environment.NewLine}{string.Join(Environment.NewLine, validationResults.Select(x => x.ErrorMessage))}");

                if (ipAddress is null)
                {
                    error.AppendLine("IP address is invalid.");
                }

                throw new Exception(error.ToString());
            }

            if (!Directory.Exists(this.configuration.ModulesPath))
            {
                Directory.CreateDirectory(this.configuration.ModulesPath);
            }

            // TODO: this sucks.
            configuration.IPAddress = ipAddress;
        }
    }
}