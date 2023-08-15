using BattleBitAPI.Server;
using BBRAPIModules;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
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

                                Type[] missingRequiredModules = this.unsatisfiedRequiredModules(moduleContext);
                                if (missingRequiredModules.Length > 0)
                                {
                                    Console.WriteLine($"Failed to load module {moduleContext.Context.Name} because of unsatisfied required modules:");
                                    Console.WriteLine(string.Join(Environment.NewLine, missingRequiredModules.Select(m => $"- {m.Name}")));
                                    Console.WriteLine("Add them to your module directory and load them before loading this module.");
                                    this.unloadModule(moduleContext);
                                    return;
                                }

                                foreach (RunnerServer server in this.servers)
                                {
                                    try
                                    {
                                        // TODO: consolidate with module loading
                                        BattleBitModule module = Activator.CreateInstance(moduleContext.Module, server) as BattleBitModule;
                                        if (module is null)
                                        {
                                            // this can't really happen
                                            throw new Exception($"Module {moduleContext.Context.Name} does not inherit from {nameof(BattleBitModule)}");
                                        }
                                        server.AddModule(module);
                                        Task.Run(async () =>
                                        {
                                            module.OnModulesLoaded();
                                            if (server.IsConnected)
                                            {
                                                await module.OnConnected();
                                            }
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to load module {moduleContext.Context.Name} for server {server.GameIP}:{server.GamePort}: {ex}");
                                    }
                                }

                                Console.WriteLine($"Module {moduleContext.Context.Name} loaded.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load module {(moduleContext?.Context?.Name ?? moduleName)}: {ex}");
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
            List<ModuleContext> newlyLoadedModules = new();
            foreach (string moduleDirectory in Directory.GetDirectories(this.configuration.ModulesPath).Union(this.configuration.Modules))
            {
                ModuleContext? moduleContext = null;
                try
                {
                    moduleContext = ModuleProvider.LoadModule(moduleDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {Path.GetFileName(moduleDirectory)}: {ex}");
                    continue;
                }

                this.modules.Add(moduleContext);
                newlyLoadedModules.Add(moduleContext);
                Console.WriteLine($"Loaded module {moduleContext.Context.Name}");
            }

            // TODO: issue: module a requires b, which requires c. a and b exist, c doesn't. a gets validated, all requirements met. b gets validated, missing requirements. b gets unloaded.
            // at this point a requirements are no longer met but validation for a has already passed.
            // Any module that is removed due to having missing requirements must also trigger unloading of all modules where it is a requirement in
            foreach (ModuleMissingRequirements moduleMissingRequirements in this.getModulesWithUnsatisfiedRequiredModules(this.modules.ToArray()))
            {
                Console.WriteLine($"Failed to load module {moduleMissingRequirements.ModuleContext.Context.Name} because of unsatisfied required modules:");
                Console.WriteLine(string.Join(Environment.NewLine, moduleMissingRequirements.MissingModules.Select(m => $"- {m.Name}")));
                Console.WriteLine("Add them to your module directory and load them before loading this module.");
                this.unloadModule(moduleMissingRequirements.ModuleContext);
                newlyLoadedModules.Remove(moduleMissingRequirements.ModuleContext);
            }
        }

        private ModuleMissingRequirements[] getModulesWithUnsatisfiedRequiredModules(ModuleContext[] modules)
        {
            List<ModuleMissingRequirements> modulesMissingRequirements = new();

            foreach (ModuleContext moduleContext in modules)
            {
                Type[] missingModules = this.unsatisfiedRequiredModules(moduleContext);

                if (missingModules.Any())
                {
                    modulesMissingRequirements.Add(new ModuleMissingRequirements(moduleContext, missingModules));
                }
            }

            return modulesMissingRequirements.ToArray();
        }

        private Type[] unsatisfiedRequiredModules(ModuleContext context)
        {
            List<Type> requiredMissingModules = new();
            foreach (RequireModuleAttribute requireModuleAttribute in context.Module.GetCustomAttributes<RequireModuleAttribute>())
            {
                if (this.modules.All(m => m.Module != requireModuleAttribute.ModuleType))
                {
                    requiredMissingModules.Add(requireModuleAttribute.ModuleType);
                }
            }
            return requiredMissingModules.ToArray();
        }

        private void hookModules()
        {
            this.serverListener.OnCreatingGameServerInstance = initializeGameServer;
        }

        private Task initializeGameServer(GameServer<RunnerPlayer> server)
        {
            this.servers.Add((RunnerServer)server);

            List<BattleBitModule> battleBitModules = new();

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