using BattleBitAPI.Common;
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
                List<string> changedModules = new();
                while (true)
                {
                    changedModules.Clear();

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
                            changedModules.Add(moduleFile);
                        }

                        this.watchedFiles[moduleFile] = (fileHash, lastWrite);
                    }

                    if (changedModules.Any())
                    {
                        try
                        {
                            unloadModules();

                            foreach (string moduleFile in changedModules)
                            {
                                string moduleName = Path.GetFileNameWithoutExtension(moduleFile);
                                Module? module = Module.Modules.FirstOrDefault(m => m.Name == moduleName);
                                if (module is not null)
                                {
                                    Module.RemoveModule(module);
                                }
                            }

                            loadModules();
                        }
                        catch (Exception ex)
                        {
                            await Console.Out.WriteLineAsync($"Failed dynamic loading of modules {string.Join(", ", changedModules.Select(f => Path.GetFileNameWithoutExtension(f)))}: {ex}");
                        }
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
                            Console.Write($"{server.GameIP}:{server.GamePort} is");
                            if (server.IsConnected)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Connected");
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Not connected");
                            }
                            Console.ResetColor();
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
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Usage: reload <module>");
                            Console.ResetColor();
                            break;
                        }

                        string moduleName = commandParts[1];
                        Module? moduleToLoad = Module.Modules.FirstOrDefault(m => m.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
                        if (moduleToLoad is null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Module {moduleName} not found.");
                            Console.ResetColor();
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
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to load module {Path.GetFileName(m)}");
                    Console.ResetColor();
                    Console.WriteLine(ex.ToString());
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
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Duplicate modules found for {duplicate[0].Name}:");
                    Console.ResetColor();
                    foreach (Module module in duplicate)
                    {
                        Console.WriteLine($"  {module.ModuleFilePath}");
                    }
                }
                throw new Exception("Duplicate modules found, aborting startup.");
            }

            Module[] sortedModules = new ModuleDependencyResolver(modules).GetDependencyOrder().ToArray();

            int compiledModuleCount = 0;

            foreach (Module module in sortedModules)
            {
                try
                {
                    string[] missingRequirements = module.RequiredDependencies.Where(r => sortedModules.All(m => m.Name != r)).ToArray();
                    if (missingRequirements.Length > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Module {module.Name} is missing required dependencies:");
                        Console.ResetColor();
                        Console.WriteLine($"{string.Join(Environment.NewLine, missingRequirements)}");
                        continue;
                    }

                    if (module.AssemblyBytes is null)
                    {
                        module.Compile();
                        compiledModuleCount++;
                    }

                    module.Load();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to load module {Path.GetFileName(module.Name)}");
                    Console.ResetColor();
                    Console.WriteLine(ex.ToString());
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"Loaded module ");
                Console.ResetColor();
                Console.WriteLine(module.Name);
            }

            Console.WriteLine();
            Console.WriteLine($"{(compiledModuleCount == Module.Modules.Count ? Module.Modules.Count.ToString() : $"{compiledModuleCount} changed, {Module.Modules.Count} total")} module{(Module.Modules.Count != 1 ? "s" : "")} loaded.");

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
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to load module {module.Name}:");
                    Console.ResetColor();
                    Console.WriteLine(ex.ToString());
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
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Failed to load module {module.Name} configuration {property.Name}:");
                        Console.ResetColor();
                        Console.WriteLine(ex.ToString());
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
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Method {nameof(battleBitModule.OnModulesLoaded)} on module {battleBitModule.GetType().Name} threw an exception:");
                    Console.ResetColor();
                    Console.WriteLine(ex.ToString());
                }
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > this.configuration.WarningThreshold)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Method {nameof(battleBitModule.OnModulesLoaded)} on module {battleBitModule.GetType().Name} took {stopwatch.ElapsedMilliseconds}ms to execute.");
                    Console.ResetColor();
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
            this.serverListener.LogLevel = this.configuration.LogLevel;
            this.serverListener.OnLog += this.serverListener_OnLog;
            this.serverListener.Start(this.configuration.IPAddress, this.configuration.Port!.Value);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"Listener started at ");
            Console.ResetColor();
            Console.WriteLine($"{this.configuration.IPAddress}:{this.configuration.Port.Value}");
        }

        private void serverListener_OnLog(LogLevel level, string message, object? obj)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{level}] {message}");
            Console.ResetColor();
        }

        private void loadConfiguration()
        {
            if (!File.Exists("appsettings.json"))
            {
                File.WriteAllText("appsettings.json", JsonConvert.SerializeObject(this.configuration));
            }

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