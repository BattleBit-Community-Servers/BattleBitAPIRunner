using BattleBitAPIRunner;
using BBRAPIModules;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using System.Collections.ObjectModel;

namespace BBRAPIModuleVerification;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: BBRAPIModuleVerification <path to module.cs>");
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
            Console.WriteLine(JsonConvert.SerializeObject(new VerificationResponse(false, Path.GetFileNameWithoutExtension(filePath), null, null, null, null, e.Message)));
            return;
        }

        List<string> missingDependencies = new();
        List<PortableExecutableReference> references = new();

        foreach (string dependency in module.RequiredDependencies)
        {
            if (!Directory.Exists($"./cache/modules/{dependency}"))
            {
                missingDependencies.Add(dependency);
            }
            else
            {
                references.Add(MetadataReference.CreateFromFile($"./cache/modules/{dependency}/{dependency}.dll"));
            }
        }

        if (missingDependencies.Count > 0)
        {
            Console.WriteLine(JsonConvert.SerializeObject(new VerificationResponse(false, module.Name, module.Description, module.Version, module.RequiredDependencies, module.OptionalDependencies, $"Missing dependencies: {string.Join(", ", missingDependencies)}")));
            return;
        }

        try
        {
            module.Compile(references.ToArray());
            if (!Directory.Exists($"./cache/modules/{module.Name}"))
            {
                Directory.CreateDirectory($"./cache/modules/{module.Name}");
            }
            File.WriteAllBytes($"./cache/modules/{module.Name}/{module.Name}.dll", module.AssemblyBytes);

            Console.WriteLine(JsonConvert.SerializeObject(new VerificationResponse(true, module.Name, module.Description, module.Version, module.RequiredDependencies, module.OptionalDependencies, null)));
        }
        catch (Exception e)
        {
            Console.WriteLine(JsonConvert.SerializeObject(new VerificationResponse(false, module.Name, module.Description, module.Version, module.RequiredDependencies, module.OptionalDependencies, e.Message)));
            return;
        }
    }


}

internal class VerificationResponse
{
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