using BattleBitAPI.Common;
using BBRAPIModules;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Reflection;
using System.Threading.Tasks;

/// <summary>
/// Author: @Axiom
/// Version: 1.0.2
/// Dependencies: MongoDB.Driver.Core, MongoDB.Driver, MongoDB.Bson, MongoDB.Libmongocrypt.dll, netstandard.dll, DnsClient.dll
/// 1.0.2 changes: Added channel to chat logs, Changed OnplayerReport "Reason" to "reason.ToString()", this will get the name of the reason rather than the number
/// </summary>

namespace MongoDBLogging
{
    public class MongoDBLogging : BattleBitModule
    {
        public MongoDBLoggingConfiguration Configuration { get; set; }
        private IMongoCollection<BsonDocument> ServerAPILogs;
        private IMongoCollection<BsonDocument> PlayerConnectionLogs;
        private IMongoCollection<BsonDocument> ChatLogs;
        private IMongoCollection<BsonDocument> PlayerReportLogs;

        public override void OnModulesLoaded()
        {
            // Configuration Validation
            PropertyInfo[] properties = typeof(MongoDBLoggingConfiguration).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var propertyValue = property.GetValue(this.Configuration)?.ToString();
                if (string.IsNullOrEmpty(propertyValue))
                {
                    this.Unload();
                    throw new Exception($"{property.Name} is not set. Please set it in the configuration file.");
                }
            }

            MongoDBLoggingInit();
        }

        public void MongoDBLoggingInit()
        {
            var DatabaseName = this.Configuration.DatabaseName;

            ServerAPILogs = GetCollection(DatabaseName, this.Configuration.CollectionNames.ServerAPILogs);
            PlayerConnectionLogs = GetCollection(DatabaseName, this.Configuration.CollectionNames.PlayerConnectionLogs);
            ChatLogs = GetCollection(DatabaseName, this.Configuration.CollectionNames.ChatLogs);
            PlayerReportLogs = GetCollection(DatabaseName, this.Configuration.CollectionNames.PlayerReportLogs);
        }

        private IMongoCollection<BsonDocument> GetCollection(string databaseName, string collectionName)
        {
            return new MongoClient(this.Configuration?.ConnectionString)
                .GetDatabase(databaseName)
                .GetCollection<BsonDocument>(collectionName);
        }

        private async Task<bool> InsertLogAsync(IMongoCollection<BsonDocument> collection, BsonDocument document)
        {
            try
            {
                await collection.InsertOneAsync(document);
                return true;
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"An error occurred while connecting to MongoDB: {ex.Message}");
                return false;
            }
        }

        public override async Task OnConnected()
        {
            var doc = new BsonDocument
            {
                {"timestamp", DateTime.UtcNow},
                {"server", this.Server.ServerName},
                {"connection_type", "Connected"}
            };
            await InsertLogAsync(ServerAPILogs, doc);
        }

        public override async Task OnDisconnected()
        {
            var doc = new BsonDocument
            {
                {"timestamp", DateTime.UtcNow},
                {"server", this.Server.ServerName},
                {"connection_type", "Disconnected"}
            };
            await InsertLogAsync(ServerAPILogs, doc);
        }

        public override async Task OnPlayerConnected(RunnerPlayer player)
        {
            var doc = new BsonDocument
            {
                {"steam_id", player.SteamID.ToString()},
                {"username", player.Name},
                {"connection_type", "Connected"},
                {"timestamp", DateTime.UtcNow},
                {"server_name", this.Server.ServerName}
            };
            await InsertLogAsync(PlayerConnectionLogs, doc);
        }

        public override async Task OnPlayerDisconnected(RunnerPlayer player)
        {
            var doc = new BsonDocument
            {
                {"steam_id", player.SteamID.ToString()},
                {"username", player.Name},
                {"connection_type", "Disconnected"},
                {"timestamp", DateTime.UtcNow},
                {"server_name", this.Server.ServerName}
            };
            await InsertLogAsync(PlayerConnectionLogs, doc);
        }

        public override async Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
        {
            if (msg.Length > 0)
            {
                var doc = new BsonDocument
                {
                    {"steam_id", player.SteamID.ToString()},
                    {"username", player.Name},
                    {"channel", channel.ToString()},
                    {"message", msg},
                    {"timestamp", DateTime.UtcNow},
                    {"server_name", this.Server.ServerName}
                };
                return await InsertLogAsync(ChatLogs, doc);
            }
            else
            {
                return false;
            }
        }

        public override async Task OnPlayerReported(RunnerPlayer from, RunnerPlayer to, ReportReason reason, string additional)
        {
            var doc = new BsonDocument
            {
                {"reporting_steam_id", from.SteamID.ToString()},
                {"reporting_username", from.Name},
                {"reported_steam_id", to.SteamID.ToString()},
                {"reported_username", to.Name},
                {"reason_type", reason.ToString()},
                {"reason", additional},
                {"timestamp", DateTime.UtcNow},
                {"server_name", this.Server.ServerName}
            };
            await InsertLogAsync(PlayerReportLogs, doc);
        }
    }

    public class MongoDBLoggingConfiguration : ModuleConfiguration
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public CollectionNamesConfiguration CollectionNames { get; set; } = new CollectionNamesConfiguration();
    }

    public class CollectionNamesConfiguration
    {
        public string ServerAPILogs { get; set; } = "ServerAPILogs";
        public string PlayerConnectionLogs { get; set; } = "PlayerConnectionLogs";
        public string ChatLogs { get; set; } = "ChatLogs";
        public string PlayerReportLogs { get; set; } = "PlayerReportLogs";
    }
}
