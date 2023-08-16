# BattleBitAPIRunner

Modular battlebit community server api runner. Lets you host community servers with modules without having to code.

## Features

- Start the community server API endpoint for your server to connect to
- Dynamically load and unload modules
- Modules are simple C# source files `.cs`
- Modules are compiled in runtime, letting you change them ad-hoc
- Supports debugging of modules (simply attach your debugger)
- Modules support module dependencies
- Modules support binary dependencies (Newtonsoft.Json, System.Net.Http, ...)

## Configuration

```
{
  "IP": "127.0.0.1",
  "Port": 29595,
  "ModulePath": "./modules",
  "Modules": [ "C:\\path\\to\\specific\\ModuleFile.cs" ],
  "DependencyPath": "./dependencies"
}
```
- IP: Listening IP
- Port: Listening port
- ModulePath: Path to the folder containing all modules (is created if not exist)
- Modules: Array of individual module file paths
- DependencyPath: Path to the folder containing all binary dependencies (dlls)

## Usage

Download the latest release, unpack, configure and start.
You can then copy modules into your module directory and load them by typing `load ModuleName` into the console.
Unload modules by typing `unload modulefilename`. Reload modules by typing `load modulefilename`.

## Developing modules

Modules are .net 6.0 C# source code files. They are compiled in runtime, so you can change them ad-hoc.
To debug a module, simply attach your debugger to the BattleBitAPIRunner process.

To create a module, create a (library) .net 6.0 C# project.
Set `ImplicitUsings` to `disabled`, for example by unchecking `Enable implicit global usings to be declared by the project SDK.` in the project settings.
Add a nuget dependency to [BBRAPIModules](https://www.nuget.org/packages/BBRAPIModules).
In your module source file, have exactly one public class which has the same name as your file and inherit `BBRAPIModules.BattleBitModule`.
Your module class now has all methods of the BattleBit API, such as `OnConnected`.

To use module dependencies, for each dependent module, add the module source file to your project and add a `[RequireModule(typeof(DependantModule))]` attribute to your class.
You can assign all your dependant modules to fields in the `OnModulesLoaded` method which gets called once all modules are available.
Use `this.Server.GetModule<DependantModule>()` to retrieve the module instance of the server.

### Example modules

- https://github.com/RainOrigami/BattleBitExamples

## Base modules

- https://github.com/RainOrigami/BattleBitBaseModules - A collection of basic modules to get you started (MOTD, PlayerFinder, PlayerPermissions, CommandHandler, PermissionsCommands, DiscordWebhooks)

## Other modules

- https://github.com/RainOrigami/BattleBitZombies - 28 days later inspired zombie pvp game mode module

## Features to come
- Per-module and per-server built-in configuration files
- Optional module dependencies
- You can suggest more over at the [Blood is Good Discord](https://discord.bloodisgood.org)
