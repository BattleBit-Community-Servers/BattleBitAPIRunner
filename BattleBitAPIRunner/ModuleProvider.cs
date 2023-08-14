using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BattleBitAPIRunner
{
    internal static class ModuleProvider
    {
        private const string PUBLISH_DIR = @"bin\Release\Publish";

        public static Type LoadModule(string modulePath)
        {
            if (!Directory.Exists(modulePath))
            {
                throw new FileNotFoundException("Module not found", modulePath);
            }

            string[] modulesCsprojFiles = Directory.GetFiles(modulePath, "*.csproj").ToArray();
            if (modulesCsprojFiles.Length != 1)
            {
                throw new Exception($"Module {Path.GetDirectoryName(modulePath)} does not contain one singular csproj file");
            }

            compileProject(modulesCsprojFiles.First());

            string moduleDllPath = Path.Combine(modulePath, PUBLISH_DIR, Path.GetFileNameWithoutExtension(modulesCsprojFiles.First()) + ".dll");
            if (!File.Exists(moduleDllPath))
            {
                throw new FileNotFoundException("Module dll not found", moduleDllPath);
            }

            Assembly assembly = Assembly.LoadFrom(moduleDllPath);
            IEnumerable<Type> moduleTypes = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(BattleBitModule)));
            if (moduleTypes.Count() != 1)
            {
                throw new Exception($"Module {Path.GetDirectoryName(modulePath)} does not contain a class that inherits from {nameof(BattleBitModule)}");
            }

            return moduleTypes.First();
        }

        private static void compileProject(string csprojFilePath)
        {
            Console.Write($"Compiling module {Path.GetFileNameWithoutExtension(csprojFilePath)}... ");

            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{csprojFilePath}\" -c Release -o \"{Path.Combine(Path.GetDirectoryName(csprojFilePath)!, PUBLISH_DIR)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = processInfo;
                process.Start();

                _ = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine();
                    throw new Exception($"Failed to compile module {Path.GetDirectoryName(csprojFilePath)}. Errors:{Environment.NewLine}{errors}");
                }
            }

            Console.WriteLine("Done");
        }

        //private void downloadNugetPackage(string packageName, string version)
        //{
        //    Process process = new Process();
        //    process.StartInfo.FileName = "nuget.exe";
        //    process.StartInfo.Arguments = $"install {packageName} -Version {version} -OutputDirectory {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libraries")}";
        //    process.StartInfo.UseShellExecute = false;
        //    process.StartInfo.RedirectStandardOutput = true;
        //    process.Start();
        //    string output = process.StandardOutput.ReadToEnd();
        //    process.WaitForExit();
        //    if (process.ExitCode != 0)
        //    {
        //        throw new Exception($"Failed to download nuget package {packageName} version {version}:{Environment.NewLine}{output}");
        //    }
        //}

        //private static Type compile(string code, string name)
        //{



        //    //List<PortableExecutableReference> refs = AppDomain.CurrentDomain.GetAssemblies().Where(x => !string.IsNullOrWhiteSpace(x.Location)).Select(x => MetadataReference.CreateFromFile(x.Location)).ToList();
        //    //refs.Add(MetadataReference.CreateFromFile(typeof(BattleBitModule).Assembly.Location));
        //    //refs.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));

        //    //SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(code);
        //    //CSharpCompilation compilation = CSharpCompilation.Create(name)
        //    //    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
        //    //    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
        //    //    .WithReferences(refs)
        //    //    .AddSyntaxTrees(syntaxTree);

        //    //Assembly? assembly = null;

        //    //using (Stream codeStream = new MemoryStream())
        //    //{
        //    //    EmitResult compilationResult = compilation.Emit(codeStream);

        //    //    if (!compilationResult.Success)
        //    //    {
        //    //        var sb = new StringBuilder();
        //    //        foreach (var diag in compilationResult.Diagnostics)
        //    //        {
        //    //            sb.AppendLine(diag.ToString());
        //    //        }

        //    //        throw new Exception(sb.ToString());
        //    //    }


        //    //    assembly = Assembly.Load(((MemoryStream)codeStream).ToArray());
        //    //}

        //    //// Make sure there is exactly one type that inherits from BattleBitModule
        //    //IEnumerable<Type> moduleTypes = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(BattleBitModule)));

        //    //if (moduleTypes.Count() != 1)
        //    //{
        //    //    throw new Exception($"Module {name} must have exactly one type that inherits from {nameof(BattleBitModule)}");
        //    //}

        //    //return moduleTypes.First();
        //}


    }
}
