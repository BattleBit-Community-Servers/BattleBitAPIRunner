using BattleBitAPI.Server;
using BBRAPIModules;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;

namespace BattleBitAPIRunner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new Program();
        }

        private ServerConfiguration configuration = new();
        private List<ModuleContext> modules = new();
        private List<RunnerServer> servers = new();
        private ServerListener<RunnerPlayer, RunnerServer> serverListener = new();

        public Program()
        {
            loadConfiguration();
            validateConfiguration();
            loadModules();
            hookModules();
            startServerListener();

            consoleCommandHandler();
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
                        foreach (ModuleContext context in this.modules)
                        {
                            Console.WriteLine(context.Context.Name);
                        }
                        break;
                    case "unload":
                    case "load":
                        if (commandParts.Length < 2)
                        {
                            Console.WriteLine("Usage: load/unload <module name>");
                            continue;
                        }

                        string moduleName = commandParts[1];
                        ModuleContext? moduleContext = this.modules.FirstOrDefault(x => x.Context.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase));

                        if (commandParts[0].Equals("load", StringComparison.OrdinalIgnoreCase))
                        {
                            // Load
                            if (moduleContext is not null)
                            {
                                // Remove module first
                                unloadModule(moduleContext);
                            }

                            try
                            {
                                string? modulePath = Directory.GetDirectories(this.configuration.ModulesPath).Union(this.configuration.Modules).FirstOrDefault(m => Path.GetFileName(m).Equals(moduleName, StringComparison.OrdinalIgnoreCase));

                                if (string.IsNullOrEmpty(modulePath))
                                {
                                    throw new FileNotFoundException("Module not found", modulePath);
                                }

                                moduleContext = ModuleProvider.LoadModule(modulePath);
                                this.modules.Add(moduleContext);

                                foreach (RunnerServer server in this.servers)
                                {
                                    try
                                    {
                                        // TODO: consolidate with module loading
                                        BattleBitModule module = Activator.CreateInstance(moduleContext.Module, server) as BattleBitModule;
                                        if (module is null)
                                        {
                                            throw new Exception($"Module {moduleContext.Context.Name} does not inherit from {nameof(BattleBitModule)}");
                                        }
                                        server.AddModule(module);
                                        if (server.IsConnected)
                                        {
                                            module.OnConnected();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to load module {moduleContext.Context.Name} for server {server.GameIP}:{server.GamePort}: {ex.Message}");
                                    }
                                }

                                Console.WriteLine($"Module {moduleContext.Context.Name} loaded.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load module {moduleContext.Context.Name}: {ex.Message}");
                                continue;
                            }
                        }
                        else
                        {
                            // Unload
                            if (moduleContext is null)
                            {
                                Console.WriteLine($"Module {moduleName} not found");
                                continue;
                            }

                            unloadModule(moduleContext);

                            Console.WriteLine($"Module {moduleContext.Context.Name} unloaded.");
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void unloadModule(ModuleContext moduleContext)
        {
            this.modules.Remove(moduleContext);

            foreach (RunnerServer server in this.servers)
            {
                BattleBitModule? module = server.GetModule(moduleContext.Module);
                if (module is null)
                {
                    continue;
                }

                server.RemoveModule(module);
            }

            ModuleProvider.UnloadModule(moduleContext);
        }

        private void loadModules()
        {
            foreach (string moduleDirectory in Directory.GetDirectories(this.configuration.ModulesPath).Union(this.configuration.Modules))
            {
                ModuleContext moduleContext;
                try
                {
                    moduleContext = ModuleProvider.LoadModule(moduleDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {Path.GetFileName(moduleDirectory)}: {ex.Message}");
                    continue;
                }

                this.modules.Add(moduleContext);
                Console.WriteLine($"Loaded module {moduleContext.Context.Name}");
            }
        }

        private void hookModules()
        {
            this.serverListener.OnCreatingGameServerInstance = initializeGameServer;
        }

        private Task initializeGameServer(GameServer<RunnerPlayer> server)
        {
            // TODO: does this work?
            this.servers.Add((RunnerServer)server);

            foreach (ModuleContext moduleContext in this.modules)
            {
                try
                {
                    BattleBitModule module = Activator.CreateInstance(moduleContext.Module, server) as BattleBitModule;
                    if (module is null)
                    {
                        throw new Exception($"Module {moduleContext.Context.Name} does not inherit from {nameof(BattleBitModule)}");
                    }
                    ((RunnerServer)server).AddModule(module);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {moduleContext.Context.Name}: {ex.Message}");
                }
            }

            return Task.CompletedTask;
        }

        private void startServerListener()
        {
            this.serverListener.Start(this.configuration.IPAddress, this.configuration.Port!.Value);
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