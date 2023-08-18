using BattleBitAPI.Common;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BattleBitAPIRunner")]

namespace BBRAPIModules
{
    public abstract class BattleBitModule
    {
        protected RunnerServer Server { get; private set; }

        internal void SetServer(RunnerServer server)
        {
            this.Server = server;
        }

        public void Call(string methodName, params object?[]? parameters)
        {
            this.Call<object?>(methodName, parameters);
        }

        public T Call<T>(string methodName, params object?[]? parameters)
        {
            var method = this.GetType().GetMethod(methodName);
            if (method == null)
            {
                return default(T);
            }

            ParameterInfo[] methodParameters = method.GetParameters();

            List<object?> fullParameters = new(parameters ?? new object?[] { });

            if (methodParameters.Length > 0 && methodParameters.Length != parameters?.Length)
            {
                for (int i = parameters?.Length ?? 0; i < methodParameters.Length; i++)
                {
                    if (methodParameters[i].IsOptional || methodParameters[i].HasDefaultValue)
                    {
                        fullParameters.Add(methodParameters[i].DefaultValue);
                    }
                    else
                    {
                        throw new ArgumentException($"Parameter {methodParameters[i].Name} is not optional and does not have a default value.");
                    }
                }
            }

            return (T)method.Invoke(this, fullParameters.ToArray());
        }

        protected void Unload()
        {
            this.Server.RemoveModule(this);
            this.Server = null!;
        }

        public virtual void OnModulesLoaded() { } // sighs silently

        #region GameServer.cs copy-paste
        // TODO: there must be a better way to do this!?

#pragma warning disable CS1998
        public virtual async Task OnConnected()
        {

        }
        public virtual async Task OnTick()
        {

        }
        public virtual async Task OnReconnected()
        {

        }
        public virtual async Task OnDisconnected()
        {

        }
        public virtual async Task OnPlayerConnected(RunnerPlayer player)
        {

        }
        public virtual async Task OnPlayerDisconnected(RunnerPlayer player)
        {

        }
        public virtual async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            return true;
        }
        public virtual async Task OnPlayerJoiningToServer(ulong steamID, PlayerJoiningArguments args)
        {
        }
        public virtual async Task OnSavePlayerStats(ulong steamID, PlayerStats stats)
        {

        }
        public virtual async Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole)
        {
            return true;
        }
        public virtual async Task<bool> OnPlayerRequestingToChangeTeam(RunnerPlayer player, Team requestedTeam)
        {
            return true;
        }
        public virtual async Task OnPlayerChangedRole(RunnerPlayer player, GameRole role)
        {

        }
        public virtual async Task OnPlayerJoinedSquad(RunnerPlayer player, Squads squad)
        {

        }
        public virtual async Task OnPlayerLeftSquad(RunnerPlayer player, Squads squad)
        {

        }
        public virtual async Task OnPlayerChangeTeam(RunnerPlayer player, Team team)
        {

        }
        public virtual async Task<OnPlayerSpawnArguments> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
        {
            return request;
        }
        public virtual async Task OnPlayerSpawned(RunnerPlayer player)
        {

        }
        public virtual async Task OnPlayerDied(RunnerPlayer player)
        {

        }
        public virtual async Task OnPlayerGivenUp(RunnerPlayer player)
        {

        }
        public virtual async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<RunnerPlayer> args)
        {
        }
        public virtual async Task OnAPlayerRevivedAnotherPlayer(RunnerPlayer from, RunnerPlayer to)
        {
        }
        public virtual async Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional)
        {

        }
        public virtual async Task OnGameStateChanged(GameState oldState, GameState newState)
        {

        }
        public virtual async Task OnRoundStarted()
        {

        }

        public virtual async Task OnRoundEnded()
        {

        }
#pragma warning restore CS1998
        #endregion
    }
}