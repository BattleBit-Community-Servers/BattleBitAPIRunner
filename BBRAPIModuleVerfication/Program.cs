﻿using BattleBitAPIRunner;
using Newtonsoft.Json;

namespace BBRAPIModuleVerfication;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: BBRAPIModuleVerfication <path to module.cs>");
            return;
        }

        string filePath = args[0];

        Module module;
        try
        {
            module = new Module(filePath);
        }
        catch (Exception e)
        {
            Console.WriteLine(JsonConvert.SerializeObject(new VerificationResponse(false, null, null, null, null, null, e.Message)));
            return;
        }

        List<Module> modules = new();
        List<Module> newModules = new() { module };
        DependencyListResponse dependencyFiles;
        do
        {
            Console.WriteLine(JsonConvert.SerializeObject(new DependencyListResponse() { Dependencies = newModules.SelectMany(m => m.RequiredDependencies).Distinct().ToArray() }));
            dependencyFiles = JsonConvert.DeserializeObject<DependencyListResponse>(Console.In.ReadToEnd());
            modules.AddRange(newModules);
            newModules.Clear();
            newModules.AddRange(dependencyFiles.Dependencies.Select(x => new Module(x)));
        } while (newModules.Count > 0);

        ModuleDependencyResolver dependencies = new(modules.ToArray());
        try
        {
            

        }
        catch (Exception e)
        {
            Console.WriteLine(JsonConvert.SerializeObject(new VerificationResponse(false, null, null, null, null, null, e.Message)));
            return;
        }

        try
        {
            Module[] allModules = dependencies.GetDependencyOrder().ToArray();
            
            module.Compile();
        }
        catch (Exception e)
        {
            Console.WriteLine(JsonConvert.SerializeObject(new VerificationResponse(false, module.Name, module.Description, module.Version, module.RequiredDependencies, module.OptionalDependencies, e.Message)));
            return;
        }
    }
}

public enum ResponseType
{
    DependencyList,
    VerificationResult
}

public interface IResponse
{
    public ResponseType ResponseType { get; }
}

internal class VerificationResponse : IResponse
{
    public ResponseType ResponseType => ResponseType.VerificationResult;

    public VerificationResponse(bool success, string name, string description, string version, string[] requiredDependencies, string[] optionalDependencies, string errors)
    {
        this.Success = success;
        this.Name = name;
        this.Description = description;
        this.Version = version;
        this.RequiredDependencies = requiredDependencies;
        this.OptionalDependencies = optionalDependencies;
        this.Errors = errors;
    }

    public bool Success { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string[]? RequiredDependencies { get; set; }
    public string[]? OptionalDependencies { get; set; }
    public string? Errors { get; set; }
}

internal class DependencyListResponse : IResponse
{
    public ResponseType ResponseType => ResponseType.DependencyList;

    public string[] Dependencies { get; set; }
}