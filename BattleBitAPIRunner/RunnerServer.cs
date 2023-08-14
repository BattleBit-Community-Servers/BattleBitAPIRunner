using BattleBitAPI.Common;
using BattleBitAPI.Server;
using System.Diagnostics;
using System.Reflection;

namespace BattleBitAPIRunner
{
    public class RunnerServer : GameServer<RunnerPlayer>
    {
        private List<BattleBitModule> modules = new();

        internal void AddModule(BattleBitModule module)
        {
            if (this.modules.Any(m => m.GetType() == module.GetType()))
            {
                throw new Exception($"Module {module.GetType().Name} is already loaded.");
            }

            this.modules.Add(module);
        }

        internal void RemoveModule(BattleBitModule module)
        {
            if (this.modules.All(m => m.GetType() != module.GetType()))
            {
                throw new Exception($"Module {module.GetType().Name} is not loaded.");
            }

            this.modules.Remove(module);
        }

        public T? GetModule<T>() where T : BattleBitModule
        {
            return this.modules.FirstOrDefault(m => m.GetType() == typeof(T)) as T;
        }

        private async Task<object?> invokeOnModulesWithReturnValue(string method, params object?[] parameters)
        {
            Stopwatch stopwatch = new();
            object? result = null;
            foreach (BattleBitModule module in this.modules)
            {
                try
                {
                    stopwatch.Start();
                    object? moduleResult = await (Task<object?>)typeof(BattleBitModule).GetMethod(method).Invoke(module, parameters);

                    if (moduleResult is bool boolResult && !boolResult)
                    {
                        result = false;
                    }
                    else
                    {
                        // TODO: I hate this.
                        result = moduleResult;
                    }
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync($"Method {method} on module {module.GetType().Name} threw an exception: {ex.Message}");
                }
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 250)
                {
                    // TODO: move this to a configurable field in ServerConfiguration
                    await Console.Out.WriteLineAsync($"Method {method} on module {module.GetType().Name} took {stopwatch.ElapsedMilliseconds}ms to execute.");
                }
            }
            return result;
        }

        private async Task invokeOnModules(string method, params object?[] parameters)
        {
            Stopwatch stopwatch = new();
            foreach (BattleBitModule module in this.modules)
            {
                try
                {
                    stopwatch.Start();
                    await (Task)typeof(BattleBitModule).GetMethod(method).Invoke(module, parameters);
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync($"Method {method} on module {module.GetType().Name} threw an exception: {ex.Message}");
                }
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 250)
                {
                    // TODO: move this to a configurable field in ServerConfiguration
                    await Console.Out.WriteLineAsync($"Method {method} on module {module.GetType().Name} took {stopwatch.ElapsedMilliseconds}ms to execute.");
                }
            }
        }

        // TODO: there must be a better way to do this!?
        public override async Task OnConnected()
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name);
        }
        public override async Task OnTick()
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name);
        }
        public override async Task OnReconnected()
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name);
        }
        public override async Task OnDisconnected()
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name);
        }
        public override async Task OnPlayerConnected(RunnerPlayer player)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, player);
        }
        public override async Task OnPlayerDisconnected(RunnerPlayer player)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, player);
        }
        public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            return (bool)await this.invokeOnModulesWithReturnValue(MethodBase.GetCurrentMethod().Name, player, channel, msg);
        }
        public override async Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, steamID, args);
        }
        public override async Task OnSavePlayerStats(ulong steamID, PlayerStats stats)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, steamID, stats);
        }
        public override async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole)
        {
            return (bool)await this.invokeOnModulesWithReturnValue(MethodBase.GetCurrentMethod().Name, player, requestedRole);
        }
        public override async Task<bool> OnPlayerRequestingToChangeTeam(RunnerPlayer player, Team requestedTeam)
        {
            return (bool)await this.invokeOnModulesWithReturnValue(MethodBase.GetCurrentMethod().Name, player, requestedTeam);
        }
        public override async Task OnPlayerChangedRole(RunnerPlayer player, GameRole role)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, player, role);
        }
        public override async Task OnPlayerJoinedSquad(RunnerPlayer player, Squads squad)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, player, squad);
        }
        public override async Task OnPlayerLeftSquad(RunnerPlayer player, Squads squad)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, player, squad);
        }
        public override async Task OnPlayerChangeTeam(RunnerPlayer player, Team team)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, player, team);
        }
        public override async Task<OnPlayerSpawnArguments> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
        {
            return (OnPlayerSpawnArguments)await this.invokeOnModulesWithReturnValue(MethodBase.GetCurrentMethod().Name, player, request);
        }
        public override async Task OnPlayerSpawned(RunnerPlayer player)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, player);
        }
        public override async Task OnPlayerDied(RunnerPlayer player)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, player);
        }
        public override async Task OnPlayerGivenUp(RunnerPlayer player)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, player);
        }
        public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<RunnerPlayer> args)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, args);
        }
        public override async Task OnAPlayerRevivedAnotherPlayer(RunnerPlayer from, RunnerPlayer to)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, from, to);
        }
        public override async Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, from, to, reason, additional);
        }
        public override async Task OnGameStateChanged(GameState oldState, GameState newState)
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name, oldState, newState);
        }
        public override async Task OnRoundStarted()
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name);
        }

        public override async Task OnRoundEnded()
        {
            await this.invokeOnModules(MethodBase.GetCurrentMethod().Name);
        }
    }
}
