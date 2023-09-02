using BattleBitAPI.Common;
using BBRAPIModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Commands;

/// <summary>
/// Author: @RainOrigami
/// Version: 0.4.9
/// </summary>

public class CommandConfiguration : ModuleConfiguration
{
    public string CommandPrefix { get; set; } = "!";
    public int CommandsPerPage { get; set; } = 10;
}

public class CommandHandler : BattleBitModule
{
    public static CommandConfiguration CommandConfiguration { get; set; }

    private Dictionary<string, (BattleBitModule Module, MethodInfo Method)> commandCallbacks = new();

    [ModuleReference]
    public BattleBitModule? PlayerFinder { get; set; }
    [ModuleReference]
    public BattleBitModule? PlayerPermissions { get; set; }

    public override void OnModulesLoaded()
    {
        this.Register(this);
    }

    public void Register(BattleBitModule module)
    {
        foreach (MethodInfo method in module.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            CommandCallbackAttribute? attribute = method.GetCustomAttribute<CommandCallbackAttribute>();
            if (attribute != null)
            {
                // Validate parameter
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 0 && parameters[0].ParameterType != typeof(RunnerPlayer))
                {
                    throw new Exception($"Command callback method {method.Name} in module {module.GetType().Name} has invalid first parameter. Must be of type RunnerPlayer.");
                }

                string command = attribute.Name.Trim().ToLower();

                // Prevent duplicate command names in different methods or modules
                if (this.commandCallbacks.ContainsKey(command))
                {
                    if (this.commandCallbacks[command].Method == method)
                    {
                        continue;
                    }

                    throw new Exception($"Command callback method {method.Name} in module {module.GetType().Name} has the same name as another command callback method in the same module.");
                }

                // Prevent parent commands of subcommands (!perm command does not allow !perm add and !perm remove)
                foreach (string subcommand in this.commandCallbacks.Keys.Where(c => c.Contains(' ')))
                {
                    if (!subcommand.StartsWith(command))
                    {
                        continue;
                    }

                    throw new Exception($"Command callback {command} in module {module.GetType().Name} conflicts with subcommand {subcommand}.");
                }

                // Prevent subcommands of existing commands (!perm add and !perm remove do not allow !perm)
                if (command.Contains(' '))
                {
                    string[] subcommandChain = command.Split(' ');
                    string subcommand = "";
                    for (int i = 0; i < subcommandChain.Length; i++)
                    {
                        subcommand += $"{subcommandChain[i]} ";
                        if (this.commandCallbacks.ContainsKey(subcommand.Trim()))
                        {
                            throw new Exception($"Command callback {command} in module {module.GetType().Name} conflicts with parent command {subcommand.Trim()}.");
                        }
                    }
                }

                this.commandCallbacks.Add(command, (module, method));
            }
        }
    }

    public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string message)
    {
        if (!message.StartsWith(CommandConfiguration.CommandPrefix) || (message.StartsWith(CommandConfiguration.CommandPrefix) && message.Length <= CommandConfiguration.CommandPrefix.Length))
        {
            return Task.FromResult(true);
        }

        Task.Run(() => this.handleCommand(player, message));

        return Task.FromResult(false);
    }

    private void handleCommand(RunnerPlayer player, string message)
    {
        string[] fullCommand = parseCommandString(message);
        string command = fullCommand[0].Trim().ToLower()[CommandConfiguration.CommandPrefix.Length..];

        int subCommandSkip;
        for (subCommandSkip = 1; subCommandSkip < fullCommand.Length && !this.commandCallbacks.ContainsKey(command); subCommandSkip++)
        {
            command += $" {fullCommand[subCommandSkip]}";
        }

        if (!this.commandCallbacks.ContainsKey(command))
        {
            player.Message("Command not found");
            return;
        }

        fullCommand = new[] { command }.Concat(fullCommand.Skip(subCommandSkip)).ToArray();

        (BattleBitModule module, MethodInfo method) = this.commandCallbacks[command];
        CommandCallbackAttribute commandCallbackAttribute = method.GetCustomAttribute<CommandCallbackAttribute>()!;

        // Permissions
        if (this.PlayerPermissions is not null)
        {
            if (commandCallbackAttribute.AllowedRoles != Roles.None && (this.PlayerPermissions.Call<Roles>("GetPlayerRoles", player.SteamID) & commandCallbackAttribute.AllowedRoles) == 0)
            {
                player.Message($"You don't have permission to use this command.");
                return;
            }
        }

        ParameterInfo[] parameters = method.GetParameters();

        if (parameters.Length == 0)
        {
            method.Invoke(module, null);
            return;
        }

        bool hasOptional = parameters.Any(p => p.IsOptional);
        if (fullCommand.Length - 1 < parameters.Skip(1).Count(p => !p.IsOptional) || fullCommand.Length - 1 > parameters.Length - 1)
        {
            messagePlayerCommandUsage(player, method, $"Require {(hasOptional ? $"between {parameters.Skip(1).Count(p => !p.IsOptional)} and {parameters.Length - 1}" : $"{parameters.Length - 1}")} but got {fullCommand.Length - 1} argument{((fullCommand.Length - 1) == 1 ? "" : "s")}.");
            return;
        }

        object?[] args = new object[parameters.Length];
        args[0] = player;

        for (int i = 1; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];

            if (parameter.IsOptional && i >= fullCommand.Length)
            {
                args[i] = parameter.DefaultValue;
                continue;
            }

            string argument = fullCommand[i].Trim();

            if (parameter.ParameterType == typeof(string))
            {
                args[i] = argument;
            }
            else if (parameter.ParameterType == typeof(RunnerPlayer))
            {
                RunnerPlayer? targetPlayer = null;

                if (this.PlayerFinder is not null)
                {
                    try
                    {
                        targetPlayer = this.PlayerFinder.Call<RunnerPlayer?>("ByNamePart", argument);
                    }
                    catch (Exception ex)
                    {
                        player.Message(ex.ToString());
                        return;
                    }

                    if (targetPlayer == null)
                    {
                        player.Message($"Could not find player name containing {argument}.");
                        return;
                    }
                }
                else
                {
                    targetPlayer = this.Server.AllPlayers.FirstOrDefault(p => p.Name.Equals(argument, StringComparison.OrdinalIgnoreCase));
                }

                if (targetPlayer == null)
                {
                    player.Message($"Could not find player {argument}.");
                    return;
                }

                args[i] = targetPlayer;
            }
            else
            {
                if (!tryParseParameter(parameter, argument, out object? parsedValue))
                {
                    messagePlayerCommandUsage(player, method, $"Couldn't parse value {argument} to type {parameter.ParameterType.Name}");
                    return;
                }

                args[i] = parsedValue;
            }
        }

        method.Invoke(module, args);
    }

    private static void messagePlayerCommandUsage(RunnerPlayer player, MethodInfo method, string? error = null)
    {
        CommandCallbackAttribute commandCallbackAttribute = method.GetCustomAttribute<CommandCallbackAttribute>()!;
        bool hasOptional = method.GetParameters().Any(p => p.IsOptional);
        player.Message($"<color=\"red\">Invalid command usage{(error == null ? "" : $" ({error})")}.<color=\"white\"><br><b>Usage</b>: {CommandConfiguration.CommandPrefix}{commandCallbackAttribute.Name} {string.Join(' ', method.GetParameters().Skip(1).Select(s => $"{s.Name}{(s.IsOptional ? "*" : "")}"))}{(hasOptional ? "<br><size=80%>* Parameter is optional." : "")}");
    }

    private static bool tryParseParameter(ParameterInfo parameterInfo, string input, out object? parsedValue)
    {
        parsedValue = null;

        try
        {
            if (parameterInfo.ParameterType.IsEnum)
            {
                parsedValue = Enum.Parse(parameterInfo.ParameterType, input, true);
            }
            else
            {
                Type? targetType = targetType = Nullable.GetUnderlyingType(parameterInfo.ParameterType);
                if (targetType is null)
                {
                    targetType = parameterInfo.ParameterType;
                }
                parsedValue = Convert.ChangeType(input, targetType);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string[] parseCommandString(string command)
    {
        List<string> parameterValues = new();
        string[] tokens = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        bool insideQuotes = false;
        StringBuilder currentValue = new();

        foreach (var token in tokens)
        {
            if (!insideQuotes)
            {
                if (token.StartsWith("\""))
                {
                    insideQuotes = true;
                    currentValue.Append(token.Substring(1));
                }
                else
                {
                    parameterValues.Add(token);
                }
            }
            else
            {
                if (token.EndsWith("\""))
                {
                    insideQuotes = false;
                    currentValue.Append(" ").Append(token.Substring(0, token.Length - 1));
                    parameterValues.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(" ").Append(token);
                }
            }
        }

        return parameterValues.Select(unescapeQuotes).ToArray();
    }

    private static string unescapeQuotes(string input)
    {
        return input.Replace("\\\"", "\"");
    }

    [CommandCallback("help", Description = "Shows this help message")]
    public void HelpCommand(RunnerPlayer player, int page = 1)
    {
        List<string> helpLines = new();
        //helpLines.Add($"<b>{CommandConfiguration.CommandPrefix}help command</b>: Shows the command syntax");
        foreach (var (commandKey, (module, method)) in this.commandCallbacks)
        {
            CommandCallbackAttribute commandCallbackAttribute = method.GetCustomAttribute<CommandCallbackAttribute>()!;

            if (this.PlayerPermissions is not null)
            {
                if (commandCallbackAttribute.AllowedRoles != Roles.None && (this.PlayerPermissions.Call<Roles>("GetPlayerRoles", player.SteamID) & commandCallbackAttribute.AllowedRoles) == 0)
                {
                    continue;
                }
            }

            helpLines.Add($"<b>{CommandConfiguration.CommandPrefix}{commandCallbackAttribute.Name}</b>{(string.IsNullOrEmpty(commandCallbackAttribute.Description) ? "" : $": {commandCallbackAttribute.Description}")}");
        }

        int pages = (int)Math.Ceiling((double)helpLines.Count / CommandConfiguration.CommandsPerPage);

        if (page < 1 || page > pages)
        {
            player.Message($"<color=\"red\">Invalid page number. Must be between 1 and {pages}.");
            return;
        }

        player.Message($"<#FFA500>Available commands<br><color=\"white\">{Environment.NewLine}{string.Join(Environment.NewLine, helpLines.Skip((page - 1) * CommandConfiguration.CommandsPerPage).Take(CommandConfiguration.CommandsPerPage))}{(pages > 1 ? $"{Environment.NewLine}Page {page} of {pages}{(page < pages ? $" - type !help {page + 1} for next page" : "")}" : "")}");
    }

    [CommandCallback("cmdhelp", Description = "Shows help for a specific command")]
    public void CommandHelpCommand(RunnerPlayer player, string command)
    {
        if (!this.commandCallbacks.TryGetValue(command, out var commandCallback))
        {
            player.Message($"<color=\"red\">Command {command} not found.<color=\"white\">");
            return;
        }

        CommandCallbackAttribute commandCallbackAttribute = commandCallback.Method.GetCustomAttribute<CommandCallbackAttribute>()!;

        bool hasOptional = commandCallback.Method.GetParameters().Any(p => p.IsOptional);
        player.Message($"<size=120%>{commandCallback.Module.GetType().Name} {commandCallbackAttribute.Name}<size=100%><br>{commandCallbackAttribute.Description}<br><#F5F5F5>{CommandConfiguration.CommandPrefix}{commandCallbackAttribute.Name} {string.Join(' ', commandCallback.Method.GetParameters().Skip(1).Select(s => $"{s.Name}{(s.IsOptional ? "*" : "")}"))}{(hasOptional ? "<br><color=\"white\"><size=80%>* Parameter is optional." : "")}");
    }
}

public class CommandCallbackAttribute : Attribute
{
    public string Name { get; set; }

    public string Description { get; set; } = string.Empty;
    public Roles AllowedRoles { get; set; }

    public CommandCallbackAttribute(string name)
    {
        this.Name = name;
    }
}
