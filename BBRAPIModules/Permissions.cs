using log4net;
using System.Security.Cryptography;
using System.Text.Json;

namespace BBRAPIModules;

public class Permissions
{
    public const string CatchAll = "*";
    public const string RevokePrefix = "-";
    public const string PermissionSeparator = ".";
    public const string PermissionsFile = "permissions.json";
    public const string PlayerGroupsFile = "player_groups.json";
    public const string PlayerPermissionsFile = "player_permissions.json";

    private Dictionary<string, List<string>> groups = new() {
        { CatchAll, new() }
    };
    private Dictionary<ulong, List<string>> playerGroups = new();
    private Dictionary<ulong, List<string>> playerPermissions = new();

    private Dictionary<string, (string Hash, DateTime LastModified)> watchedFiles = new();

    private ILog logger = LogManager.GetLogger(nameof(Permissions));

    private string configurationsDirectory;

    public Permissions(string configurationsDirectory)
    {
        this.configurationsDirectory = configurationsDirectory;

        if (!File.Exists(Path.Combine(this.configurationsDirectory, PermissionsFile)) || !File.Exists(Path.Combine(this.configurationsDirectory, PlayerGroupsFile)) || !File.Exists(Path.Combine(this.configurationsDirectory, PlayerPermissionsFile)))
        {
            this.Save();
        }
        else
        {
            this.Load();
        }


        Task.Run(permissionsConfigurationWatcher);
    }

    private void permissionsConfigurationWatcher()
    {
        while (true)
        {
            bool reload = false;
            lock (watchedFiles)
            {
                foreach (KeyValuePair<string, (string Hash, DateTime LastModified)> file in watchedFiles)
                {
                    DateTime lastModified = File.GetLastWriteTime(Path.Combine(this.configurationsDirectory, file.Key));
                    if (file.Value.LastModified == lastModified)
                    {
                        continue;
                    }

                    string hash = CalculateFileHash(new FileInfo(Path.Combine(this.configurationsDirectory, file.Key)));
                    if (file.Value.Hash == hash)
                    {
                        continue;
                    }

                    this.logger.Debug($"File {file.Key} has changed.");

                    reload = true;
                    watchedFiles[file.Key] = (hash, lastModified);
                }

                if (reload)
                {
                    this.logger.Debug("Reloading permissions.");
                    this.Load();
                }
            }

            Task.Delay(1000).Wait();
        }
    }

    private static string CalculateFileHash(FileInfo fileInfo)
    {
        using (var md5 = MD5.Create())
        using (var stream = fileInfo.OpenRead())
        {
            byte[] hashBytes = md5.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    public void Load()
    {
        this.logger.Debug($"Loading permissions from {this.configurationsDirectory}.");

        Dictionary<string, List<string>>? groups;
        try
        {
            groups = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(Path.Combine(this.configurationsDirectory, PermissionsFile)))!;

            if (groups == null)
            {
                this.logger.Warn("Permissions file is empty, default permissions will be enforced.");
            }
            else
            {
                this.groups = groups;
            }
        }
        catch (Exception ex)
        {
            this.logger.Error("Failed to load permissions file, default permissions will be enforced.", ex);
        }

        Dictionary<ulong, List<string>>? playerGroups;
        try
        {
            playerGroups = JsonSerializer.Deserialize<Dictionary<ulong, List<string>>>(File.ReadAllText(Path.Combine(this.configurationsDirectory, PlayerGroupsFile)))!;

            if (playerGroups == null)
            {
                this.logger.Warn("Player groups file is empty, default permissions will be enforced.");
            }
            else
            {
                this.playerGroups = playerGroups;
            }
        }
        catch (Exception ex)
        {
            this.logger.Error("Failed to load player groups file, default permissions will be enforced.", ex);
        }

        Dictionary<ulong, List<string>>? playerPermissions;
        try
        {
            playerPermissions = JsonSerializer.Deserialize<Dictionary<ulong, List<string>>>(File.ReadAllText(Path.Combine(this.configurationsDirectory, PlayerPermissionsFile)))!;

            if (playerPermissions == null)
            {
                this.logger.Warn("Player permissions file is empty, default permissions will be enforced.");
            }
            else
            {
                this.playerPermissions = playerPermissions;
            }
        }
        catch (Exception ex)
        {
            this.logger.Error("Failed to load player permissions file, default permissions will be enforced.", ex);
        }
    }

    public void Save()
    {
        lock (watchedFiles)
        {
            this.logger.Debug($"Saving permissions to {this.configurationsDirectory}.");
            File.WriteAllText(Path.Combine(this.configurationsDirectory, PermissionsFile), JsonSerializer.Serialize(this.groups));
            File.WriteAllText(Path.Combine(this.configurationsDirectory, PlayerGroupsFile), JsonSerializer.Serialize(this.playerGroups));
            File.WriteAllText(Path.Combine(this.configurationsDirectory, PlayerPermissionsFile), JsonSerializer.Serialize(this.playerPermissions));

            watchedFiles = new()
            {
                { PlayerPermissionsFile, (CalculateFileHash(new FileInfo(Path.Combine(this.configurationsDirectory, PlayerPermissionsFile))), File.GetLastWriteTime(Path.Combine(this.configurationsDirectory, PlayerPermissionsFile))) },
                { PlayerGroupsFile, (CalculateFileHash(new FileInfo(Path.Combine(this.configurationsDirectory, PlayerGroupsFile))), File.GetLastWriteTime(Path.Combine(this.configurationsDirectory, PlayerGroupsFile))) },
                { PermissionsFile, (CalculateFileHash(new FileInfo(Path.Combine(this.configurationsDirectory, PermissionsFile))), File.GetLastWriteTime(Path.Combine(this.configurationsDirectory, PermissionsFile))) }
            };
            this.logger.Debug("Permissions saved.");
        }
    }

    public bool HasPermission(ulong steamId, string permission)
    {
        this.logger.Debug($"Checking if player {steamId} has permission {permission}.");

        if (permission == CatchAll)
        {
            this.logger.Debug($"Player {steamId} has permission {permission} because it is a catch-all permission.");
            return true;
        }

        string[] playerGroups = this.playerGroups.ContainsKey(steamId) ? this.playerGroups[steamId].ToArray() : Array.Empty<string>();
        if (this.groups.ContainsKey(CatchAll))
        {
            playerGroups = playerGroups.Append(CatchAll).ToArray();
        }

        string[] permissionPath = permission.Split(PermissionSeparator);

        bool permitted = false;

        // Player permissions
        if (playerPermissions.ContainsKey(steamId))
        {
            if (playerPermissions[steamId].Any(p => p.Equals(RevokePrefix + permission, StringComparison.InvariantCultureIgnoreCase)))
            {
                this.logger.Debug($"Player {steamId} does not have permission {permission} because they have revoked permission {permission}.");
                return false;
            }

            if (playerPermissions[steamId].Any(p => p.Equals(permission, StringComparison.InvariantCultureIgnoreCase)))
            {
                this.logger.Debug($"Player {steamId} has permission {permission} because they have permission {permission}.");
                permitted = true;
            }
        }

        // Group permissions
        foreach (string group in playerGroups)
        {
            if (!this.groups.ContainsKey(group))
            {
                this.logger.Error($"Group {group} of player {steamId} does not exist.");
                continue;
            }

            if (this.groups[group].Contains("-*"))
            {
                this.logger.Debug($"Player {steamId} does not have permission {permission} because group {group} has revoked all permissions.");
                return false;
            }

            if (this.groups[group].Any(p => p.Equals(RevokePrefix + permission, StringComparison.InvariantCultureIgnoreCase)))
            {
                this.logger.Debug($"Player {steamId} does not have permission {permission} because group {group} has revoked permission {permission}.");
                return false;
            }

            if (this.groups[group].Any(p => p.Equals(permission, StringComparison.InvariantCultureIgnoreCase)))
            {
                permitted = true;
            }
        }

        // Partial permissions with catch-all
        string partialPermissionPath = string.Empty;
        foreach (string permissionPart in permissionPath)
        {
            partialPermissionPath += $"{permissionPart}{PermissionSeparator}";

            // Partial player permissions

            if (playerPermissions.ContainsKey(steamId))
            {
                if (playerPermissions[steamId].Any(p => p.Equals(RevokePrefix + partialPermissionPath + CatchAll, StringComparison.InvariantCultureIgnoreCase)))
                {
                    this.logger.Debug($"Player {steamId} does not have permission {permission} because they have revoked permission {partialPermissionPath + CatchAll}.");
                    return false;
                }

                if (playerPermissions[steamId].Any(p => p.Equals(partialPermissionPath + CatchAll, StringComparison.InvariantCultureIgnoreCase)))
                {
                    this.logger.Debug($"Player {steamId} has permission {permission} because they have permission {partialPermissionPath + CatchAll}.");
                    permitted = true;
                }
            }

            // Partial group permissions

            foreach (string group in playerGroups)
            {
                if (!this.groups.ContainsKey(group))
                {
                    // We already logged this error above.
                    continue;
                }

                if (this.groups[group].Any(p => p.Equals(RevokePrefix + partialPermissionPath + CatchAll, StringComparison.InvariantCultureIgnoreCase)))
                {
                    this.logger.Debug($"Player {steamId} does not have permission {permission} because group {group} has revoked permission {partialPermissionPath + CatchAll}.");
                    return false;
                }

                if (this.groups[group].Any(p => p.Equals(partialPermissionPath + CatchAll, StringComparison.InvariantCultureIgnoreCase)))
                {
                    this.logger.Debug($"Player {steamId} has permission {permission} because group {group} has permission {partialPermissionPath + CatchAll}.");
                    permitted = true;
                }
            }
        }

        this.logger.Debug($"Player {steamId} {(permitted ? "has" : "does not have")} permission {permission}.");

        return permitted;
    }

    public void AddGroup(string group)
    {
        this.logger.Debug($"Adding group {group}.");

        if (this.groups.ContainsKey(group))
        {
            this.logger.Error($"Group {group} already exists.");
            return;
        }

        this.groups.Add(group, new());
    }

    public void RemoveGroup(string group)
    {
        this.logger.Debug($"Removing group {group}.");

        if (!this.groups.ContainsKey(group))
        {
            this.logger.Error($"Group {group} does not exist.");
            return;
        }

        this.groups.Remove(group);
    }

    public void AddGroupPermission(string group, string permission)
    {
        this.logger.Debug($"Adding permission {permission} to group {group}.");

        if (!this.groups.ContainsKey(group))
        {
            this.logger.Error($"Group {group} does not exist.");
            return;
        }

        if (this.groups[group].Contains(permission))
        {
            this.logger.Error($"Group {group} already has permission {permission}.");
            return;
        }

        this.groups[group].Add(permission);
    }

    public void RemoveGroupPermission(string group, string permission)
    {
        this.logger.Debug($"Removing permission {permission} from group {group}.");

        if (!this.groups.ContainsKey(group))
        {
            this.logger.Error($"Group {group} does not exist.");
            return;
        }

        if (!this.groups[group].Contains(permission))
        {
            this.logger.Error($"Group {group} does not have permission {permission}.");
            return;
        }

        this.groups[group].Remove(permission);
    }

    public void AddRevokedGroupPermission(string group, string permission)
    {
        this.logger.Debug($"Adding revoked permission {permission} to group {group}.");

        this.AddGroupPermission(group, $"{RevokePrefix}{permission}");
    }

    public void RemoveRevokedGroupPermission(string group, string permission)
    {
        this.logger.Debug($"Removing revoked permission {permission} from group {group}.");

        this.RemoveGroupPermission(group, $"{RevokePrefix}{permission}");
    }

    public void AddPlayerGroup(ulong steamId, string group)
    {
        this.logger.Debug($"Adding player {steamId} to group {group}.");

        if (!this.playerGroups.ContainsKey(steamId))
        {
            this.logger.Debug($"Player {steamId} does not have any groups.");
            this.playerGroups.Add(steamId, new());
        }

        if (this.playerGroups[steamId].Contains(group))
        {
            this.logger.Error($"Player {steamId} already has group {group}.");
            return;
        }

        this.playerGroups[steamId].Add(group);
    }

    public void RemovePlayerGroup(ulong steamId, string group)
    {
        this.logger.Debug($"Removing player {steamId} from group {group}.");

        if (!this.playerGroups.ContainsKey(steamId))
        {
            this.logger.Error($"Player {steamId} does not have any groups.");
            return;
        }

        if (!this.playerGroups[steamId].Contains(group))
        {
            this.logger.Error($"Player {steamId} does not have group {group}.");
            return;
        }

        this.playerGroups[steamId].Remove(group);
    }

    public void AddPlayerPermission(ulong steamId, string permission)
    {
        this.logger.Debug($"Adding permission {permission} to player {steamId}.");

        if (!this.playerPermissions.ContainsKey(steamId))
        {
            this.logger.Debug($"Player {steamId} does not have any permissions.");
            this.playerPermissions.Add(steamId, new());
        }

        if (this.playerPermissions[steamId].Contains(permission))
        {
            this.logger.Error($"Player {steamId} already has permission {permission}.");
            return;
        }

        this.playerPermissions[steamId].Add(permission);
    }

    public void RemovePlayerPermission(ulong steamId, string permission)
    {
        this.logger.Debug($"Removing permission {permission} from player {steamId}.");

        if (!this.playerPermissions.ContainsKey(steamId))
        {
            this.logger.Error($"Player {steamId} does not have any permissions.");
            return;
        }

        if (!this.playerPermissions[steamId].Contains(permission))
        {
            this.logger.Error($"Player {steamId} does not have permission {permission}.");
            return;
        }

        this.playerPermissions[steamId].Remove(permission);
    }

    public void AddRevokedPlayerPermission(ulong steamId, string permission)
    {
        this.logger.Debug($"Adding revoked permission {permission} to player {steamId}.");

        this.AddPlayerPermission(steamId, $"{RevokePrefix}{permission}");
    }

    public void RemoveRevokedPlayerPermission(ulong steamId, string permission)
    {
        this.logger.Debug($"Removing revoked permission {permission} from player {steamId}.");

        this.RemovePlayerPermission(steamId, $"{RevokePrefix}{permission}");
    }
}
