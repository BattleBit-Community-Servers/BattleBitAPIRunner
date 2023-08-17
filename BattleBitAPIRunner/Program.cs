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
                    default:
                        break;
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

            foreach (Module module in Module.Modules)
            {
                try
                {
                    BattleBitModule moduleInstance = Activator.CreateInstance(module.ModuleType, server) as BattleBitModule;
                    if (moduleInstance is null)
                    {
                        throw new Exception($"Module {module.Name} does not inherit from {nameof(BattleBitModule)}");
                    }
                    ((RunnerServer)server).AddModule(moduleInstance);
                    battleBitModules.Add(moduleInstance);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {module.Name}: {ex}");
                }
            }

            foreach (BattleBitModule battleBitModule in battleBitModules)
            {
                try
                {
                    battleBitModule.OnModulesLoaded();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
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