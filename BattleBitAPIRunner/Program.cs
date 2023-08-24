using BattleBitAPI.Server;
using BBRAPIModules;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
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
        private Dictionary<string, (string Hash, DateTime LastModified)> watchedFiles = new();

        public Program()
        {
            loadConfiguration();
            validateConfiguration();
            loadDependencies();
            loadModules();
            hookModules();
            fileWatchers();
            startServerListener();

            consoleCommandHandler();
        }

        private void fileWatchers()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    foreach (string moduleFile in Directory.GetFiles(this.configuration.ModulesPath, "*.cs").Union(this.configuration.Modules))
                    {
                        if (!this.watchedFiles.ContainsKey(moduleFile))
                        {
                            this.watchedFiles.Add(moduleFile, (string.Empty, DateTime.MinValue));
                        }

                        DateTime lastWrite = File.GetLastWriteTime(moduleFile);
                        if (this.watchedFiles[moduleFile].LastModified == lastWrite)
                        {
                            continue;
                        }

                        string fileHash = CalculateFileHash(new FileInfo(moduleFile));

                        if (this.watchedFiles[moduleFile].Hash != fileHash)
                        {
                            string moduleName = Path.GetFileNameWithoutExtension(moduleFile);
                            unloadModules();
                            Module.RemoveModule(Module.Modules.First(m => m.Name == moduleName));
                            loadModules();
                        }

                        this.watchedFiles[moduleFile] = (fileHash, lastWrite);
                    }

                    await Task.Delay(1000);
                }
            });
        }

        static string CalculateFileHash(FileInfo fileInfo)
        {
            using (var md5 = MD5.Create())
            using (var stream = fileInfo.OpenRead())
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
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
                    case "reloadall":
                        unloadModules();
                        foreach (Module module in Module.Modules.ToArray())
                        {
                            Module.RemoveModule(module);
                        }
                        loadModules();
                        break;
                    case "reload":
                        if (commandParts.Length < 2)
                        {
                            Console.WriteLine("Usage: reload <module>");
                            break;
                        }

                        string moduleName = commandParts[1];
                        Module? moduleToLoad = Module.Modules.FirstOrDefault(m => m.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
                        if (moduleToLoad is null)
                        {
                            Console.WriteLine($"Module {moduleName} not found.");
                            break;
                        }

                        unloadModules();
                        Module.RemoveModule(moduleToLoad);
                        loadModules();
                        break;
                    default:
                        break;
                }
            }
        }

        private void unloadModules()
        {
            foreach (RunnerServer server in this.servers)
            {
                List<BattleBitModule> instances = new();
                foreach (Module module in Module.Modules)
                {
                    BattleBitModule? moduleInstance = server.GetModule(module.ModuleType);
                    if (moduleInstance is null)
                    {
                        continue;
                    }

                    instances.Add(moduleInstance);
                    moduleInstance.OnModuleUnloading();
                }

                foreach (BattleBitModule moduleInstance in instances)
                {
                    moduleInstance.Unload();
                }
            }

            Module.UnloadContext();
        }

        private void loadModules()
        {
            Module[] modules = Directory.GetFiles(this.configuration.ModulesPath, "*.cs").Union(this.configuration.Modules).Where(f => Module.Modules.All(m => m.Name != Path.GetFileNameWithoutExtension(f))).Select(m =>
            {
                try { return new Module(m); }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {Path.GetFileName(m)}: {ex}");
                    return null;
                }
            }).Where(m => m is not null).Select(m => m!).Union(Module.Modules).ToArray();

            foreach (Module toRemove in Module.Modules.ToArray())
            {
                Module.RemoveModule(toRemove);
            }

            foreach (Module toWatch in modules)
            {
                if (this.watchedFiles.ContainsKey(toWatch.ModuleFilePath))
                {
                    continue;
                }

                this.watchedFiles.Add(toWatch.ModuleFilePath, (CalculateFileHash(new FileInfo(toWatch.ModuleFilePath)), File.GetLastWriteTime(toWatch.ModuleFilePath)));
            }

            Module[][] duplicateModules = modules.GroupBy(m => m.Name).Where(g => g.Count() > 1).Select(g => g.ToArray()).ToArray();
            if (duplicateModules.Length > 0)
            {
                foreach (Module[] duplicate in duplicateModules)
                {
                    Console.WriteLine($"Duplicate modules found for {duplicate[0].Name}:");
                    foreach (Module module in duplicate)
                    {
                        Console.WriteLine($"  {module.ModuleFilePath}");
                    }
                }
                throw new Exception("Duplicate modules found, aborting startup.");
            }

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

            foreach (RunnerServer server in this.servers)
            {
                loadServerModules(server);
                server.OnConnected();
            }
        }

        private void hookModules()
        {
            this.serverListener.OnCreatingGameServerInstance = initializeGameServer;
        }
        private RunnerServer initializeGameServer(IPAddress ip, ushort port)
        {
            RunnerServer server = new RunnerServer();
            this.servers.Add(server);

            loadServerModules(server, ip, port);

            return server;
        }

        private void loadServerModules(RunnerServer server, IPAddress? ip = null, ushort? port = null)
        {
            List<BattleBitModule> battleBitModules = new();

            foreach (Module module in Module.Modules)
            {
                BattleBitModule moduleInstance;
                try
                {
                    moduleInstance = Activator.CreateInstance(module.ModuleType) as BattleBitModule;
                    if (moduleInstance is null)
                    {
                        throw new Exception($"Not inheriting from {nameof(BattleBitModule)}");
                    }
                    moduleInstance.SetServer(server);
                    server.AddModule(moduleInstance);
                    battleBitModules.Add(moduleInstance);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load module {module.Name}: {ex}");
                    continue;
                }

                // Module configurations
                foreach (PropertyInfo property in module.ModuleType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(p => p.PropertyType.IsAssignableTo(typeof(ModuleConfiguration))))
                {
                    try
                    {
                        if (!property.PropertyType.IsAssignableTo(typeof(ModuleConfiguration)))
                        {
                            throw new Exception($"Configuration does not inherit from {nameof(ModuleConfiguration)}");
                        }

                        ModuleConfiguration moduleConfiguration = Activator.CreateInstance(property.PropertyType) as ModuleConfiguration;
                        moduleConfiguration.Initialize(moduleInstance, property, $"{ip ?? server.GameIP}_{port ?? server.GamePort}");
                        moduleConfiguration.OnLoadingRequest += ModuleConfiguration_OnLoadingRequest;
                        moduleConfiguration.OnSavingRequest += ModuleConfiguration_OnSavingRequest;
                        moduleConfiguration.Load();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load module {module.Name} configuration {property.Name}: {ex}");
                        continue;
                    }
                }
            }

            foreach (BattleBitModule battleBitModule in battleBitModules)
            {
                // Resolve references
                foreach (PropertyInfo property in battleBitModule.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    ModuleReferenceAttribute? moduleReference = property.GetCustomAttribute<ModuleReferenceAttribute>();
                    if (moduleReference is null)
                    {
                        continue;
                    }

                    BattleBitModule? referencedModule = battleBitModules.FirstOrDefault(m => m.GetType().Name == property.Name);
                    if (referencedModule is null)
                    {
                        continue;
                    }

                    property.SetValue(battleBitModule, referencedModule);
                }
            }

            foreach (BattleBitModule battleBitModule in battleBitModules)
            {
                Stopwatch stopwatch = new();
                try
                {
                    stopwatch.Start();
                    battleBitModule.IsLoaded = true;
                    battleBitModule.OnModulesLoaded();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Method {nameof(battleBitModule.OnModulesLoaded)} on module {battleBitModule.GetType().Name} threw an exception: {ex}");
                }
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 250)
                {
                    // TODO: move this to a configurable field in ServerConfiguration
                    Console.WriteLine($"Method {nameof(battleBitModule.OnModulesLoaded)} on module {battleBitModule.GetType().Name} took {stopwatch.ElapsedMilliseconds}ms to execute.");
                }
            }
        }

        private void ModuleConfiguration_OnSavingRequest(object? sender, BattleBitModule module, PropertyInfo property, string serverName)
        {
            string fileName = $"{property.Name}.json";
            string filePath = Path.Combine(this.configuration.ConfigurationPath, module.GetType().Name, fileName);
            if (property.GetMethod?.IsStatic != true)
            {
                filePath = Path.Combine(this.configuration.ConfigurationPath, serverName, module.GetType().Name, fileName);
            }

            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }

            object? configurationValue = property.GetValue(module);
            if (configurationValue is null)
            {
                return; // nothing to save
            }

            File.WriteAllText(filePath, JsonConvert.SerializeObject(configurationValue, Formatting.Indented));
        }

        private void ModuleConfiguration_OnLoadingRequest(object? sender, BattleBitModule module, PropertyInfo property, string serverName)
        {
            string fileName = $"{property.Name}.json";
            string filePath = Path.Combine(this.configuration.ConfigurationPath, module.GetType().Name, fileName);
            if (property.GetMethod?.IsStatic != true)
            {
                filePath = Path.Combine(this.configuration.ConfigurationPath, serverName, module.GetType().Name, fileName);
            }

            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }

            // Create instance of type of the property if it doesn't exist
            ModuleConfiguration? configurationValue = property.GetValue(module) as ModuleConfiguration;
            if (configurationValue is null)
            {
                configurationValue = Activator.CreateInstance(property.PropertyType) as ModuleConfiguration;
                configurationValue!.Initialize(module, property, serverName);
                configurationValue.OnLoadingRequest += ModuleConfiguration_OnLoadingRequest;
                configurationValue.OnSavingRequest += ModuleConfiguration_OnSavingRequest;

                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(configurationValue, Formatting.Indented));
                }
            }

            if (File.Exists(filePath))
            {
                configurationValue = JsonConvert.DeserializeObject(File.ReadAllText(filePath), property.PropertyType) as ModuleConfiguration;
                configurationValue.Initialize(module, property, serverName);
                configurationValue.OnLoadingRequest += ModuleConfiguration_OnLoadingRequest;
                configurationValue.OnSavingRequest += ModuleConfiguration_OnSavingRequest;
                property.SetValue(module, configurationValue);
            }
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