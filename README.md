# BattleBitAPIRunner

Modular battlebit community server api runner. Lets you host community servers with modules without having to code. If you know Umod/Rust.Oxide then you will be right at home.

Join our Discord: https://discord.gg/FTkj9xUvHh

# THIS IS NOT AN OFFICIAL BATTLEBIT TOOL, IT IS JUST AN IMPLEMENTATION OF THE OFFICIAL API THAT CAN BE USED INSTEAD OF THE API. IT IS NOT ASSOCIATED WITH BATTLEBIT OR THEIR DEVELOPERS BUT IS A COMMUNITY MADE PRODUCT

Hosting tutorial: https://www.youtube.com/watch?v=B8JNTL4-AxU  
Module programming tutorial: https://www.youtube.com/watch?v=RN8891f0B14

Currently rewriting this page to wiki: https://github.com/RainOrigami/BattleBitAPIRunner/wiki Check it out, it contains some more detailed information and guides.

## Features

- Start the community server API endpoint for your server to connect to
- Modules are simple C# source files `.cs`
- Modules support hot reloading
- Modules support module dependencies
- Modules support optional module dependencies
- Modules support binary dependencies (Newtonsoft.Json, System.Net.Http, ...)
- Integrated Per-module and per-server configuration files

## Configuration
Configure the runner in the `appsettings.json`:
```
{
  "IP": "127.0.0.1",
  "Port": 29595,
  "ModulesPath": "./modules",
  "Modules": [ "C:\\path\\to\\specific\\ModuleFile.cs" ],
  "DependencyPath": "./dependencies",
  "ConfigurationPath": "./configurations",
  "LogLevel": "None",
  "WarningThreshold": 250,
}
```
- IP: Listening IP
- Port: Listening port
- ModulePath: Path to the folder containing all modules (is created if not exist), default value see above
- Modules: Array of individual module file paths, default value `[]`
- DependencyPath: Path to the folder containing all binary (dll) dependencies (is created if not exist), default value see above
- ConfigurationPath: Path to the folder containing all module and per-server module configuration files (is created if not exist), default value see above
- LogLevel: Logging level, default value `None`
- WarningThreshold: Time in milliseconds after which a warning is logged if a server method on a module takes too long, default value `250`

Module and per-server module configurations are located in the configurations subdirectory, if you have not changed the path.

## Usage

### Prerequisites
- [.net 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) (x64, 64-bit version) - [Installation Guide](https://learn.microsoft.com/en-us/dotnet/core/install/).

### Installation

Download the [latest release](https://github.com/RainOrigami/BattleBitAPIRunner/releases), unpack, configure and start `BattleBitAPIRunner`.
The modules, dependencies and configurations folder will be created in the same directory as the executable, if you have not specified a different path in the configuration file.
Modules are loaded upon startup. To reload modules or their configuration,type `reload modulename` or `reloadall` or simply replace the module file, it will automatically be reloaded.

### Installing modules

You can find approved modules on [our Discord](https://discord.gg/FTkj9xUvHh). We recommend to only download approved modules which have been checked by our staff for malicious or dangerous code.
Place modules in the modules folder or specify their path in the configuration file.
Place binary dependencies in the dependencies folder.

### Configuring modules
Global and per-server module configurations are located in the `./configurations` subdirectory, if you have not changed the path ([`ConfigurationPath`](https://github.com/BattleBit-Community-Servers/BattleBitAPIRunner#configuration))
Per-server module configurations are located in a subdirectory of the configurations directory named after the IP and port of the server.
For modules that require a configuration, the folders and configuration files will be automatically created when a BattleBit server connects to the API.
Currently, only whitelisted servers can connect to the API and therefore it is necessary to be whitelisted for the configuration files to be created.
To reload a changed configuration, type `reload modulename` or `reloadall` in the API console.

## Developing modules

```cs
using BBRAPIModules;
using System.Threading.Tasks;
namespace MyBattleBitModule;

public class LoadingScreenText : BattleBitModule
{
    public LoadingScreenTextConfiguration Configuration { get; set; }
    public override async Task OnConnected()
    {
        this.Server.LoadingScreenText = this.Configuration.LoadingScreenText;
    }
}
public class LoadingScreenTextConfiguration : ModuleConfiguration
{
    public string LoadingScreenText { get; set; } = "This is a community server!";
}
```
*A simple module example*

Modules are .net 6.0 C# source code files. They are compiled in runtime when the application starts.
To debug a module, simply attach your debugger to the BattleBitAPIRunner process.

To create a module, create a (library) .net 6.0 C# project.
Set `ImplicitUsings` to `disabled`, for example by unchecking `Enable implicit global usings to be declared by the project SDK.` in the project settings.
Add a nuget dependency to [BBRAPIModules](https://www.nuget.org/packages/BBRAPIModules).
In your module source file, have exactly one public class which has the same name as your file and inherit `BBRAPIModules.BattleBitModule`.
Your module class now has all methods of the BattleBit API, such as `OnConnected`.

### Special callback cases
- `OnModulesLoaded` is called after all modules have been loaded.
- `OnPlayerSpawning` will provide the OnPlayerSpawnArguments of the previous (non-null) module output. If any module returns null, the final result will be null.
- `OnPlayerTypedMessage` final result will be false if any module output is false.
- `OnPlayerRequestingToChangeRole` final result will be false if any module output is false.
- `OnPlayerRequestingToChangeTeam` final result will be false if any module output is false.

### Optional module dependencies
To optionally use specific modules, add a public property of type or `dynamic?` to your module and add the `[ModuleReference]` attribute to it. Make sure the name of the property is the name of the required module.
```cs
[ModuleReference]
public dynamic? RichText { get; set; }
```
When all modules are loaded (`OnModulesLoaded`) the dependant module will be available on this property, if it was loaded.

You can call methods on that module by invoking the method dynamically.

```cs
this.RichText?.TargetMethod();
this.RichText?.TargetMethodWithParams("param1", 2, 3);
bool? result = this.RichText?.TargetMethodWithReturnValue();
```

### Required module dependencies
To require a dependency to another module, include the required module source file in your project (optional, only for syntax validation and autocomplete).
Add a `[RequireModule(typeof(YourModuleDependency))]` attribute to your module class. Multiple attributes for multiple required dependencies are supported.

```cs
[RequireModule(typeof(PlayerPermissions))]
[RequireModule(typeof(CommandHandler))]
public class MyModule : BattleBitModule
```

You will also have to add the module properties to your class as you would do with optional module dependencies, except they will be guaranteed to not be null after `OnModulesLoaded`.

### Module Configuration
Create a class containing public properties of all your configuration variables and inherit from `ModuleConfiguration`.
Add a public property of your configuration class to your module.
If the property is static it will be a global configuration shared by all instances of your module across all servers.
If the property is not static, it will be a per-server configuration.
You can have multiple configurations, static and non-static, per module.
The configuration file will be called like the property name.

```cs
public class MyModuleConfiguration : ModuleConfiguration
{
    public string SomeConfigurationValue { get; set; } = string.Empty;
}
public class MyModule : BattleBitModule
{
    public MyModuleConfiguration GlobalConfig { get; set; }
    public MyModuleConfiguration PerServerConfig { get; set; }
}
```
This will create a `./configurations/MyModule/GlobalConfig.json` and a `./configurations/127.0.0.1_29595/MyModule/PerServerConfig.json` (for each server) configuration file.

# Example modules
- https://github.com/RainOrigami/BattleBitExamples

# Features to come
- You can suggest some over at the [BattleBit Modular API Discord](https://discord.gg/FTkj9xUvHh)
