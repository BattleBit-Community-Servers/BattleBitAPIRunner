using BattleBitAPI.Common;
using BattleBitAPI.Server;
using System.Diagnostics;

namespace BBRAPIModules
{
    public class RunnerServer : GameServer<RunnerPlayer>
    {
        private List<BattleBitModule> modules = new();

        public void AddModule(BattleBitModule module)
        {
            if (this.modules.Any(m => m.GetType() == module.GetType()))
            {
                throw new Exception($"Module {module.GetType().Name} is already loaded.");
            }

            this.modules.Add(module);
        }

        public void RemoveModule(BattleBitModule module)
        {
            if (this.modules.All(m => m.GetType() != module.GetType()))
            {
                throw new Exception($"Module {module.GetType().Name} is not loaded.");
            }

            this.modules.Remove(module);
        }

        public T? GetModule<T>() where T : BattleBitModule
        {
            return GetModule(typeof(T)) as T;
        }

        public BattleBitModule? GetModule(Type type)
        {
            return this.modules.FirstOrDefault(m => m.GetType() == type);
        }

        // TODO: this is fucked up and needs to be generalized
        private async Task<bool> invokeOnModulesWithBoolReturnValue(string method, params object?[] parameters)
        {
            Stopwatch stopwatch = new();
            bool result = true;

            foreach (BattleBitModule module in this.modules)
            {
                try
                {
                    stopwatch.Start();
                    bool moduleResult = await (Task<bool>)typeof(BattleBitModule).GetMethod(method).Invoke(module, parameters);

                    if (!moduleResult)
                    {
                        result = false;
                    }
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync($"Method {method} on module {module.GetType().Name} threw an exception: {ex}");
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

        private async Task<OnPlayerSpawnArguments?> invokeOnModulesWithPlayerSpawnArgumentsReturnValue(string method, RunnerPlayer player, OnPlayerSpawnArguments request)
        {
            Stopwatch stopwatch = new();
            OnPlayerSpawnArguments? result = request;
            OnPlayerSpawnArguments previousValidSpawnArguments = request;

            foreach (BattleBitModule module in this.modules)
            {
                try
                {
                    stopwatch.Start();
                    OnPlayerSpawnArguments? moduleResult = await (Task<OnPlayerSpawnArguments?>)typeof(BattleBitModule).GetMethod(method).Invoke(module, new object?[] { player, previousValidSpawnArguments });

                    if (moduleResult is not null)
                    {
                        previousValidSpawnArguments = moduleResult.Value;
                    }

                    // Once any module has declined the spawn request, no more spawn requests can be made
                    if (moduleResult is not null && result is not null)
                    {
                        result = moduleResult;
                    }
                    else
                    {
                        result = null;
                    }
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync($"Method {method} on module {module.GetType().Name} threw an exception: {ex}");
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
                    await Console.Out.WriteLineAsync($"Method {method} on module {module.GetType().Name} threw an exception: {ex}");
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
            await this.invokeOnModules(nameof(OnConnected));
        }
        public override async Task OnTick()
        {
            await this.invokeOnModules(nameof(OnTick));
        }
        public override async Task OnSessionChanged(long oldSessionID, long newSessionID)
        {
            await this.invokeOnModules(nameof(OnSessionChanged), oldSessionID, newSessionID);
        }
        public override async Task OnDisconnected()
        {
            await this.invokeOnModules(nameof(OnDisconnected));
        }
        public override async Task OnPlayerConnected(RunnerPlayer player)
        {
            await this.invokeOnModules(nameof(OnPlayerConnected), player);
        }
        public override async Task OnPlayerDisconnected(RunnerPlayer player)
        {
            await this.invokeOnModules(nameof(OnPlayerDisconnected), player);
        }
        public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            return (bool)await this.invokeOnModulesWithBoolReturnValue(nameof(OnPlayerTypedMessage), player, channel, msg);
        }
        public override async Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args)
        {
            await this.invokeOnModules(nameof(OnPlayerJoiningToServer), steamID, args);
        }
        public override async Task OnSavePlayerStats(ulong steamID, PlayerStats stats)
        {
            await this.invokeOnModules(nameof(OnSavePlayerStats), steamID, stats);
        }
        public override async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole)
        {
            return (bool)await this.invokeOnModulesWithBoolReturnValue(nameof(OnPlayerRequestingToChangeRole), player, requestedRole);
        }
        public override async Task<bool> OnPlayerRequestingToChangeTeam(RunnerPlayer player, Team requestedTeam)
        {
            return (bool)await this.invokeOnModulesWithBoolReturnValue(nameof(OnPlayerRequestingToChangeTeam), player, requestedTeam);
        }
        public override async Task OnPlayerChangedRole(RunnerPlayer player, GameRole role)
        {
            await this.invokeOnModules(nameof(OnPlayerChangedRole), player, role);
        }
        public override async Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
        {
            await this.invokeOnModules(nameof(OnPlayerJoinedSquad), player, squad);
        }
        public override async Task OnPlayerLeftSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
        {
            await this.invokeOnModules(nameof(OnPlayerLeftSquad), player, squad);
        }
        public override async Task OnPlayerChangeTeam(RunnerPlayer player, Team team)
        {
            await this.invokeOnModules(nameof(OnPlayerChangeTeam), player, team);
        }
        public override async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
        {
            return (OnPlayerSpawnArguments?)await this.invokeOnModulesWithPlayerSpawnArgumentsReturnValue(nameof(OnPlayerSpawning), player, request);
        }
        public override async Task OnPlayerSpawned(RunnerPlayer player)
        {
            await this.invokeOnModules(nameof(OnPlayerSpawned), player);
        }
        public override async Task OnPlayerDied(RunnerPlayer player)
        {
            await this.invokeOnModules(nameof(OnPlayerDied), player);
        }
        public override async Task OnPlayerGivenUp(RunnerPlayer player)
        {
            await this.invokeOnModules(nameof(OnPlayerGivenUp), player);
        }
        public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<RunnerPlayer> args)
        {
            await this.invokeOnModules(nameof(OnAPlayerDownedAnotherPlayer), args);
        }
        public override async Task OnAPlayerRevivedAnotherPlayer(RunnerPlayer from, RunnerPlayer to)
        {
            await this.invokeOnModules(nameof(OnAPlayerRevivedAnotherPlayer), from, to);
        }
        public override async Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional)
        {
            await this.invokeOnModules(nameof(OnPlayerReported), from, to, reason, additional);
        }
        public override async Task OnGameStateChanged(GameState oldState, GameState newState)
        {
            await this.invokeOnModules(nameof(OnGameStateChanged), oldState, newState);
        }
        public override async Task OnRoundStarted()
        {
            await this.invokeOnModules(nameof(OnRoundStarted));
        }
        public override async Task OnRoundEnded()
        {
            await this.invokeOnModules(nameof(OnRoundEnded));
        }
        public override async Task OnSquadPointsChanged(Squad<RunnerPlayer> squad, int newPoints)
        {
            await this.invokeOnModules(nameof(OnSquadPointsChanged), squad, newPoints);
        }
        public override async Task OnSquadLeaderChanged(Squad<RunnerPlayer> squad, RunnerPlayer newLeader)
        {
            await this.invokeOnModules(nameof(OnSquadLeaderChanged), squad, newLeader);
        }
    }
}
