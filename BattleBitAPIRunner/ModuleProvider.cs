using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace BattleBitAPIRunner
{
    internal static class ModuleProvider
    {
        private const string PUBLISH_DIR = @"bin\Release\Publish";

        public static ModuleContext LoadModule(string modulePath)
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

            AssemblyLoadContext assemblyContext = new(Path.GetFileNameWithoutExtension(moduleDllPath));
            Assembly assembly = assemblyContext.LoadFromAssemblyPath(moduleDllPath);

            IEnumerable<Type> moduleTypes = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(BattleBitModule)));
            if (moduleTypes.Count() != 1)
            {
                throw new Exception($"Module {Path.GetDirectoryName(modulePath)} does not contain a class that inherits from {nameof(BattleBitModule)}");
            }

            return new ModuleContext(assemblyContext, moduleTypes.First());
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
    }
}
