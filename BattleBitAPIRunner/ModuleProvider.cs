using BBRAPIModules;
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
        private static readonly string[] moduleFilter = new[] { "BBRAPIModules.dll", "CommunityServerAPI.dll" };

        public static ModuleContext LoadModule(string modulePath)
        {
            if (!Directory.Exists(modulePath))
            {
                throw new FileNotFoundException("Module not found", modulePath);
            }

            string[] modulesCsprojFiles = Directory.GetFiles(modulePath, "*.csproj").ToArray();
            if (modulesCsprojFiles.Length != 1)
            {
                throw new Exception($"Module {Path.GetFileName(modulePath)} does not contain one singular csproj file");
            }

            compileProject(modulesCsprojFiles.First());

            string moduleDllPath = Path.Combine(modulePath, PUBLISH_DIR, Path.GetFileNameWithoutExtension(modulesCsprojFiles.First()) + ".dll");
            if (!File.Exists(moduleDllPath))
            {
                throw new FileNotFoundException("Module dll not found", moduleDllPath);
            }

            Assembly assembly = Assembly.LoadFrom(Path.GetFullPath(moduleDllPath));

            foreach (string dllFile in Directory.GetFiles(Path.Combine(modulePath, PUBLISH_DIR), "*.dll").Where(f =>
            {
                string fileName = Path.GetFileName(f);
                return !fileName.Equals(Path.GetFileName(moduleDllPath), StringComparison.OrdinalIgnoreCase) && moduleFilter.All(mf => !fileName.Equals(mf, StringComparison.OrdinalIgnoreCase));
            }))
            {
                Assembly.LoadFrom(Path.GetFullPath(dllFile));
            }

            IEnumerable<Type> moduleTypes = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(BattleBitModule)));
            if (moduleTypes.Count() != 1)
            {
                throw new Exception($"Module {Path.GetFileName(modulePath)} does not contain a class that inherits from {nameof(BattleBitModule)}");
            }

            return new ModuleContext(new(Path.GetFileNameWithoutExtension(modulesCsprojFiles.First()), true), moduleTypes.First());
        }

        private static void compileProject(string csprojFilePath)
        {
            Console.WriteLine($"Compiling module {Path.GetFileNameWithoutExtension(csprojFilePath)}... ");
            Stopwatch stopwatch = Stopwatch.StartNew();

            string targetDir = Path.Combine(Path.GetDirectoryName(csprojFilePath), PUBLISH_DIR);
#if DEBUG
            string targetConfiguration = "Debug";
#else
            string targetConfiguration = "Release";
#endif

            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{csprojFilePath}\" -c {targetConfiguration} -o \"{targetDir}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = processInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine();
                    throw new Exception($"Failed to compile module {Path.GetFileName(csprojFilePath)} (took {Math.Round(stopwatch.Elapsed.TotalSeconds, 1)}s). Output:{Environment.NewLine}{output}{Environment.NewLine}{errors}");
                }
            }

            Console.WriteLine($"Completed module {Path.GetFileNameWithoutExtension(csprojFilePath)} in {Math.Round(stopwatch.Elapsed.TotalSeconds, 1)}s");
        }

        public static void UnloadModule(ModuleContext moduleContext)
        {
            moduleContext.Context.Unload();
        }
    }
}
