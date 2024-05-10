﻿using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BBRAPIModules;
using log4net;
using log4net.Config;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BattleBitAPIRunner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new Program();
        }

        internal static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            AllowTrailingCommas = true
        };

        private ILog logger = null!;

        private ServerConfiguration configuration = new();
        private List<RunnerServer> servers = new();
        private ServerListener<RunnerPlayer, RunnerServer> serverListener = new();
        private Dictionary<string, (string Hash, DateTime LastModified)> watchedFiles = new();

        public Program()
        {
            configureLogger();

            try
            {
                loadConfiguration();
            }
            catch (Exception ex)
            {
                logger.Error("Failed to load configuration", ex);
                Environment.Exit(-1);
                return;
            }

            try
            {
                validateConfiguration();
            }
            catch (Exception ex)
            {
                logger.Error("Failed to validate configuration", ex);
                Environment.Exit(-1);
                return;
            }

            try
            {
                prepareDirectories();
            }
            catch (Exception ex)
            {
                logger.Error("Failed to prepare directories", ex);
                Environment.Exit(-1);
                return;
            }

            this.logger.Info("Loading dependencies");
            loadDependencies();
            loadModules();
            hookModules();
            fileWatchers();
            startServerListener();

            consoleCommandHandler();

            Thread.Sleep(-1);
        }

        private void prepareDirectories()
        {
            if (!Directory.Exists(this.configuration.ConfigurationPath))
            {
                Directory.CreateDirectory(this.configuration.ConfigurationPath);
            }
        }

        private void configureLogger()
        {
            string log4netConfig = "log4net.config";
            if (!File.Exists(log4netConfig))
            {
                File.WriteAllText(log4netConfig, @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<log4net>
    <root>
        <level value=""INFO"" />
        <appender-ref ref=""ManagedColoredConsoleAppender"" />
    </root>
    <appender name=""ManagedColoredConsoleAppender"" type=""log4net.Appender.ManagedColoredConsoleAppender"">
        <layout type=""log4net.Layout.PatternLayout"">
            <conversionPattern value=""%date [%logger] %level - %message%newline"" />
        </layout>
		<mapping>
			<level value=""WARN"" />
			<foreColor value=""Yellow"" />
		</mapping>
		<mapping>
			<level value=""ERROR"" />
			<foreColor value=""Red"" />
		</mapping>
    </appender>
</log4net>");
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            XmlConfigurator.Configure(new FileInfo(log4netConfig));

            logger = LogManager.GetLogger("Runner");
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
                            // Try compile changed modules before unloading old ones
                            foreach (string moduleFile in changedModules.ToArray())
                            {
                                Module? changedModule = null;
                                try
                                {
                                    changedModule = new(moduleFile);
                                    changedModule.Compile(this.binaryDependencies);
                                }
                                catch (Exception ex)
                                {
                                    logger.Error($"Could not hot reload module {changedModule?.Name ?? Path.GetFileNameWithoutExtension(moduleFile)}. Running module will be kept", ex);
                                    changedModules.Remove(moduleFile);
                                }
                            }

                            if (!changedModules.Any())
                            {
                                continue;
                            }

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
                            logger.Error($"Failed dynamic loading of modules {string.Join(", ", changedModules.Select(f => Path.GetFileNameWithoutExtension(f)))}", ex);
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

            List<PortableExecutableReference> binaryDependencies = new();

            foreach (string dependency in Directory.GetFiles(this.configuration.DependencyPath, "*.dll"))
            {
                binaryDependencies.Add(MetadataReference.CreateFromFile(dependency));
            }

            this.binaryDependencies = binaryDependencies.ToArray();

            Module.LoadContext(Directory.GetFiles(this.configuration.DependencyPath, "*.dll"));
        }

        private PortableExecutableReference[] binaryDependencies = Array.Empty<PortableExecutableReference>();

        private void consoleCommandHandler()
        {
            if (Console.In is null)
            {
                logger.Info("No std in stream available.");
                return;
            }

            while (true)
            {
                string? command = Console.ReadLine();
                if (command is null)
                {
                    logger.Info("No std in stream available.");
                    return;
                }

                string[] commandParts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (commandParts.Length == 0)
                {
                    continue;
                }

                foreach (RunnerServer server in this.servers)
                {
                    List<BattleBitModule> instances = new();
                    foreach (Module module in Module.Modules)
                    {
                        BattleBitModule? moduleInstance = server.GetModule(module.ModuleType!);
                        if (moduleInstance is null)
                        {
                            continue;
                        }

                        instances.Add(moduleInstance);
                        moduleInstance.OnConsoleCommand(command);
                    }
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
                            logger.Error("Usage: reload <module>");
                            break;
                        }

                        string moduleName = commandParts[1];

                        Module? moduleToLoad = Module.Modules.FirstOrDefault(m => m.Name!.Equals(moduleName, StringComparison.OrdinalIgnoreCase));

                        if (moduleToLoad is null)
                        {
                            logger.Error($"Module {moduleName} not found.");
                            break;
                        }

                        Module? loadedModule = null;
                        try
                        {
                            loadedModule = new(moduleToLoad.ModuleFilePath);
                            loadedModule.Compile(this.binaryDependencies);
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Could not hot reload module {loadedModule?.Name ?? moduleToLoad.Name}. Running module will be kept.", ex);
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
                    BattleBitModule? moduleInstance = server.GetModule(module.ModuleType!);
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
            Module.LoadContext(Directory.GetFiles(this.configuration.DependencyPath, "*.dll"));
        }

        private void loadModules()
        {
            string[] moduleFiles = Directory.GetFiles(this.configuration.ModulesPath, "*.cs").Union(this.configuration.Modules).ToArray();
            Module[] modules = moduleFiles.Where(f => Module.Modules.All(m => m.Name != Path.GetFileNameWithoutExtension(f))).Select(m =>
            {
                try { return new Module(m); }
                catch (Exception ex)
                {
                    logger.Error($"Failed to load module {Path.GetFileName(m)}", ex);
                    return null;
                }
            }).Where(m => m is not null).Select(m => m!).Union(Module.Modules).ToArray();

            foreach (Module toRemove in Module.Modules.ToArray())
            {
                Module.RemoveModule(toRemove);
            }

            foreach (string toWatch in moduleFiles)
            {
                if (this.watchedFiles.ContainsKey(toWatch))
                {
                    continue;
                }

                this.watchedFiles.Add(toWatch, (CalculateFileHash(new FileInfo(toWatch)), File.GetLastWriteTime(toWatch)));
            }

            Module[][] duplicateModules = modules.GroupBy(m => m.Name).Where(g => g.Count() > 1).Select(g => g.ToArray()).ToArray();
            if (duplicateModules.Length > 0)
            {
                foreach (Module[] duplicate in duplicateModules)
                {
                    logger.Error($"Duplicate modules found for {duplicate[0].Name}");
                    foreach (Module module in duplicate)
                    {
                        logger.Error($"  {module.ModuleFilePath}");
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
                    string[] missingRequirements = module.RequiredDependencies!.Where(r => sortedModules.All(m => m.Name != r)).ToArray();
                    if (missingRequirements.Length > 0)
                    {
                        logger.Error($"Module {module.Name} is missing required dependencies:");
                        foreach (string missingRequirement in missingRequirements)
                        {
                            logger.Error($"  {missingRequirement}");
                        }
                        continue;
                    }

                    if (module.AssemblyBytes is null)
                    {
                        module.Compile(this.binaryDependencies);
                        compiledModuleCount++;
                    }

                    module.Load();
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to load module {Path.GetFileName(module.Name)}", ex);
                    continue;
                }

                logger.Info($"Loaded module {module.Name}");
            }

            logger.Info($"{(compiledModuleCount == Module.Modules.Count ? Module.Modules.Count.ToString() : $"{compiledModuleCount} changed, {Module.Modules.Count} total")} module{(Module.Modules.Count != 1 ? "s" : "")} loaded.");

            foreach (RunnerServer server in this.servers)
            {
                loadServerModules(server);
                _ = server.OnConnected();
            }
        }

        private void hookModules()
        {
            this.serverListener.OnCreatingGameServerInstance = initializeGameServer;
        }

        private RunnerServer initializeGameServer(IPAddress ip, ushort port)
        {
            RunnerServer server = new RunnerServer(ip, port, this.configuration.WarningThreshold);
            this.servers.Add(server);

            loadServerModules(server, ip, port);

            return server;
        }

        private void loadServerModules(RunnerServer server, IPAddress? ip = null, ushort? port = null)
        {
            ILog logger = LogManager.GetLogger($"Runner of {ip ?? server.GameIP}:{port ?? server.GamePort}");
            List<BattleBitModule> battleBitModules = new();

            foreach (Module module in Module.Modules)
            {
                BattleBitModule? moduleInstance;
                try
                {
                    moduleInstance = Activator.CreateInstance(module.ModuleType!) as BattleBitModule;
                    if (moduleInstance is null)
                    {
                        throw new Exception($"Not inheriting from {nameof(BattleBitModule)}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to load module {module.Name}", ex);
                    continue;
                }

                // Module configurations
                foreach (PropertyInfo property in module.ModuleType!.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(p => p.PropertyType.IsAssignableTo(typeof(ModuleConfiguration))))
                {
                    try
                    {
                        ModuleConfiguration moduleConfiguration = (Activator.CreateInstance(property.PropertyType) as ModuleConfiguration)!;
                        moduleConfiguration.Initialize(moduleInstance, property, $"{ip ?? server.GameIP}_{port ?? server.GamePort}");
                        moduleConfiguration.OnLoadingRequest += ModuleConfiguration_OnLoadingRequest;
                        moduleConfiguration.OnSavingRequest += ModuleConfiguration_OnSavingRequest;
                        moduleConfiguration.Load();
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to load configuration {property.Name} for module {module.Name}, not loading module.", ex);
                        continue;
                    }
                }

                moduleInstance.SetLogger(LogManager.GetLogger($"{module.Name} of {ip ?? server.GameIP}:{port ?? server.GamePort}"));
                moduleInstance.SetServer(server);
                server.AddModule(moduleInstance);
                battleBitModules.Add(moduleInstance);
            }

            battleBitModules = battleBitModules.Where(m => m.Server is not null).ToList();

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
                    logger.Error($"Method {nameof(battleBitModule.OnModulesLoaded)} on module {battleBitModule.GetType().Name} threw an exception", ex.InnerException);
                }
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > this.configuration.WarningThreshold)
                {
                    logger.Warn($"Method {nameof(battleBitModule.OnModulesLoaded)} on module {battleBitModule.GetType().Name} took {stopwatch.ElapsedMilliseconds}ms to execute.");
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
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            }

            object? configurationValue = property.GetValue(module);
            if (configurationValue is null)
            {
                return; // nothing to save
            }

            try
            {
                File.WriteAllText(filePath, JsonSerializer.Serialize(configurationValue, configurationValue.GetType(), Program.JsonSerializerOptions));
            }
            catch (Exception ex)
            {
                this.logger.Error($"Failed to save configuration {property.Name} for module {module.GetType().Name}.", ex);
            }
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
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            }

            // Create instance of type of the property if it doesn't exist
            ModuleConfiguration? configurationValue = property.GetValue(module) as ModuleConfiguration;

            if (File.Exists(filePath))
            {
                configurationValue = JsonSerializer.Deserialize(File.ReadAllText(filePath), property.PropertyType) as ModuleConfiguration;

                if (configurationValue is null)
                {
                    logger.Error($"Failed to load configuration {property.Name} for module {module.GetType().Name}.");

                    module.Unload();
                    return;
                }
            }

            if (configurationValue is null)
            {
                configurationValue = Activator.CreateInstance(property.PropertyType) as ModuleConfiguration;

                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, JsonSerializer.Serialize(configurationValue, configurationValue!.GetType(), Program.JsonSerializerOptions));
                }
            }

            configurationValue!.Initialize(module, property, serverName);
            configurationValue.OnLoadingRequest += ModuleConfiguration_OnLoadingRequest;
            configurationValue.OnSavingRequest += ModuleConfiguration_OnSavingRequest;
            property.SetValue(module, configurationValue);
        }

        private void startServerListener()
        {
            this.serverListener.LogLevel = this.configuration.LogLevel;
            this.serverListener.OnLog += this.serverListener_OnLog;
            this.serverListener.Start(this.configuration.IPAddress, this.configuration.Port!.Value);

            logger.Info($"Listener started at {this.configuration.IPAddress}:{this.configuration.Port.Value}");
        }

        private void serverListener_OnLog(LogLevel level, string message, object? obj)
        {
            logger.Info($"[{level}] {message}");
        }

        private void loadConfiguration()
        {
            if (!File.Exists("appsettings.json"))
            {
                File.WriteAllText("appsettings.json", JsonSerializer.Serialize(this.configuration, Program.JsonSerializerOptions));
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