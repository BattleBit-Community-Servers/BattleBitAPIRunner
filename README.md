# BattleBitAPIRunner

Modular battlebit community server api runner. Lets you host community servers with modules without having to code.

## Features

- Start the community server API endpoint for your server to connect to
- Dynamically load and unload modules
- Modules are simply git cloned into the module directory
- Modules are compiled in runtime, letting you change them ad-hoc
- Supports debugging of modules (simply attach your debugger)
- Modules support nuget packages, project references

## Configuration

```
{
  "IP": "127.0.0.1",
  "Port": 29595,
  "ModulePath": "./modules",
  "Modules": []
}
```
- IP: Listening IP
- Port: Listening port
- ModulePath: Path to the folder containing all modules (is created if not exist)
- Modules: Array of individual module paths

## Usage

Download a release, unpack, configure and start.
You can then git clone modules into your module directory and load them by typing `load modulefoldername` into the console.
Unload modules by typing `unload modulefoldername`. Reload modules by typing `load modulefoldername`.

## Developing modules

Modules are .net 6 projects. They are compiled in runtime, so you can change them ad-hoc.
To debug a module, simply attach your debugger to the BattleBitAPIRunner process.

To create a module, reference BattleBitAPIRunner in your project and inherit from `BattleBitAPIRunner.BattleBitModule`.
You can then override all regular GameServer methods in the module. The GameServer object is available as `BattleBitModule.Server`.

You can also access other modules by using `BattleBitModule.Server.GetModule<ModuleType>()`. See ExampleModule for an example.

## Example module

```cs
using BattleBitAPI.Common;
using BattleBitAPIRunner;

namespace ExampleModuleIntegration
{
    public class ExampleIntegration : BattleBitModule
    {
        public ExampleIntegration(RunnerServer server) : base(server)
        {
        }

        public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            this.Server.SayToChat($"Player {player.Name} is about to say something!");
            return Task.FromResult(true);
        }
    }
}
```

## Some modules

- https://github.com/RainOrigami/BattleBitDiscordWebhooks - Sends messages to a discord webhook when a player joins, leaves, chats or a server connects or disconnects from the api. Can also be used as a library in other modules to send custom messages.
- https://github.com/RainOrigami/BattleBitZombies - 28 days later inspired zombie pvp game mode module
- https://github.com/RainOrigami/BattleBitPlayerFinder - WORK IN PROGRESS - Library module that enables finding players based on a number of different things (steamid, partial name, ...)
- https://github.com/RainOrigami/BattleBitPermission - WORK IN PROGRESS - Permissions manager for persistent player permissions and permission management. Most useful as a library for other modules to use per-player permissions.
- https://github.com/RainOrigami/BattleBitCommands - WORK IN PROGRESS - Library module for easy chat and console command handling
