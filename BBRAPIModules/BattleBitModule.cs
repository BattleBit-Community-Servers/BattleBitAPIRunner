using BattleBitAPI.Common;
using BattleBitAPI.Server;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BattleBitAPIRunner")]

namespace BBRAPIModules
{
    public abstract class BattleBitModule
    {
        public RunnerServer Server { get; private set; }

        public bool IsLoaded { get; internal set; }

        internal void SetServer(RunnerServer server)
        {
            this.Server = server;
        }

        public void Unload()
        {
            this.IsLoaded = false;
            this.Server.RemoveModule(this);
            this.Server = null!;
        }

        public virtual void OnModulesLoaded() { } // sighs silently
        public virtual void OnModuleUnloading() { }
        public virtual void OnConsoleCommand(string command) { }

        #region GameServer.cs copy-paste
        // TODO: there must be a better way to do this!?

#pragma warning disable CS1998
        public virtual async Task OnConnected()
        {

        }
        public virtual async Task OnTick()
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
        public virtual async Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
        {

        }
        public virtual async Task OnSquadLeaderChanged(Squad<RunnerPlayer> squad, RunnerPlayer newLeader)
        {

        }
        public virtual async Task OnPlayerLeftSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
        {

        }
        public virtual async Task OnPlayerChangeTeam(RunnerPlayer player, Team team)
        {

        }
        public virtual async Task OnSquadPointsChanged(Squad<RunnerPlayer> squad, int newPoints)
        {

        }
        public virtual async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
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
        public virtual async Task OnSessionChanged(long oldSessionID, long newSessionID)
        {

        }
#pragma warning restore CS1998
        #endregion
    }
}