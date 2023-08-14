using BattleBitAPI.Server;
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
        private List<Type> modules = new();
        private ServerListener<RunnerPlayer, RunnerServer> serverListener = new();

        public Program()
        {
            loadConfiguration();
            validateConfiguration();
            loadModules();
            hookModules();
            startServerListener();

            Thread.Sleep(-1);
        }

        private void loadModules()
        {
            foreach (string moduleDirectory in Directory.GetDirectories(this.configuration.ModulePath).Union(this.configuration.Modules))
            {
                Type moduleType;
                try
                {
                    moduleType = ModuleProvider.LoadModule(moduleDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {moduleDirectory}: {ex.Message}");
                    continue;
                }

                this.modules.Add(moduleType);
                Console.WriteLine($"Loaded module {moduleType.Name}");
            }
        }

        private void hookModules()
        {
            this.serverListener.OnCreatingGameServerInstance = initializeGameServer;
        }

        private Task initializeGameServer(GameServer<RunnerPlayer> server)
        {
            foreach (Type moduleType in this.modules)
            {
                try
                {
                    BattleBitModule module = Activator.CreateInstance(moduleType, server) as BattleBitModule;
                    if (module is null)
                    {
                        throw new Exception($"Module {moduleType.Name} does not inherit from {nameof(BattleBitModule)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {moduleType.Name}: {ex.Message}");
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