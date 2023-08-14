using BattleBitAPI.Server;
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
                    case "unload":
                    case "load":
                        if (commandParts.Length < 2)
                        {
                            Console.WriteLine("Usage: load/unload <module name>");
                            continue;
                        }

                        string moduleName = commandParts[1];
                        ModuleContext? moduleContext = this.modules.FirstOrDefault(x => x.Module.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase));


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
                                moduleContext = ModuleProvider.LoadModule(Directory.GetDirectories(this.configuration.ModulePath).Union(this.configuration.Modules).First(m => Path.GetDirectoryName(m).Equals(moduleName, StringComparison.OrdinalIgnoreCase)));
                                this.modules.Add(moduleContext);

                                foreach (RunnerServer server in this.servers)
                                {
                                    try
                                    {
                                        // TODO: consolidate with module loading
                                        BattleBitModule module = Activator.CreateInstance(moduleContext.Module, server) as BattleBitModule;
                                        if (module is null)
                                        {
                                            throw new Exception($"Module {moduleContext.Module.Name} does not inherit from {nameof(BattleBitModule)}");
                                        }
                                        if (server.IsConnected)
                                        {
                                            module.OnConnected();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to load module {moduleContext.Module.Name} for server {server.GameIP}:{server.GamePort}: {ex.Message}");
                                    }
                                }

                                Console.WriteLine($"Module {moduleContext.Module.Name} loaded.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load module {moduleContext.Module.Name}: {ex.Message}");
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

                            Console.WriteLine($"Module {moduleContext.Module.Name} unloaded.");
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

            // TODO: does this work?
            moduleContext.Context.Unload();
        }

        private void loadModules()
        {
            foreach (string moduleDirectory in Directory.GetDirectories(this.configuration.ModulePath).Union(this.configuration.Modules))
            {
                ModuleContext moduleContext;
                try
                {
                    moduleContext = ModuleProvider.LoadModule(moduleDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {moduleDirectory}: {ex.Message}");
                    continue;
                }

                this.modules.Add(moduleContext);
                Console.WriteLine($"Loaded module {moduleContext.Module.Name}");
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
                        throw new Exception($"Module {moduleContext.Module.Name} does not inherit from {nameof(BattleBitModule)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {moduleContext.Module.Name}: {ex.Message}");
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
            if (!Validator.TryValidateObject(this.configuration, new ValidationContext(this.configuration), validationResults, true) || !IPAddress.TryParse(this.configuration.IP, out ipAddress) || !Directory.Exists(this.configuration.ModulePath))
            {
                StringBuilder error = new();
                error.AppendLine($"Invalid configuration:{Environment.NewLine}{string.Join(Environment.NewLine, validationResults.Select(x => x.ErrorMessage))}");

                if (ipAddress is null)
                {
                    error.AppendLine("IP address is invalid");
                }

                // TODO: this sucks.
                if (!Directory.Exists(this.configuration.ModulePath))
                {
                    error.AppendLine("Module path does not exist");
                }

                throw new Exception(error.ToString());
            }

            // TODO: this sucks.
            configuration.IPAddress = ipAddress;
        }

        //private async Task moduleWatcher()
        //{
        //    Dictionary<string, DateTime> modulesLastModified = new();

        //    while (true)
        //    {
        //        foreach (string moduleDirectory in Directory.GetDirectories(this.configuration.ModulePath))
        //        {
        //            DateTime lastModified = File.GetLastWriteTime(moduleDirectory);
        //            if (modulesLastModified.TryGetValue(moduleDirectory, out DateTime moduleLastModified) && lastModified <= moduleLastModified)
        //            {
        //                continue;
        //            }

        //            modulesLastModified[moduleDirectory] = lastModified;

        //            Type? module = null;
        //            try
        //            {
        //                module = ModuleProvider.LoadModule(moduleDirecctory);
        //            }
        //            catch (Exception ex)
        //            {
        //                await Console.Error.WriteLineAsync($"Failed to load module {Path.GetFileName(moduleDirectory)}:{Environment.NewLine}{ex}");
        //                continue;
        //            }

        //            await Console.Out.WriteLineAsync($"Found new or updated module {module.Name}");

        //            try
        //            {
        //                BattleBitModule? instance = Activator.CreateInstance(module) as BattleBitModule;
        //                if (instance is null)
        //                {
        //                    // This can not happen because it is validated by the ModuleProvider
        //                    throw new Exception($"Module {module.Name} does not inherit from {nameof(BattleBitModule)}");
        //                }

        //                instance.OnLoad();
        //            }
        //            catch (Exception ex)
        //            {
        //                await Console.Error.WriteLineAsync($"Failed to load module {module.Name}:{Environment.NewLine}{ex}");
        //            }
        //        }

        //        await Task.Delay(this.configuration.ModuleScanInterval);
        //    }
        //}
    }
}