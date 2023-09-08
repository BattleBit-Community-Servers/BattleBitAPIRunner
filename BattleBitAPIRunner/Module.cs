using BattleBitAPI;
using BBRAPIModules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BattleBitAPIRunner
{
    internal class Module
    {
        private static List<Module> modules = new();
        public static IReadOnlyList<Module> Modules => modules;
        private static AssemblyLoadContext moduleContext = new AssemblyLoadContext("Modules", true);

        public AssemblyLoadContext? Context { get; private set; }
        public Type? ModuleType { get; private set; }
        public string? Name { get; private set; }
        public string[]? RequiredDependencies { get; private set; }
        public string[]? OptionalDependencies { get; private set; }
        public byte[] AssemblyBytes { get; private set; }
        public byte[] PDBBytes { get; private set; }
        public string ModuleFilePath { get; }
        public Assembly? ModuleAssembly { get; private set; }

        private SyntaxTree syntaxTree;
        private string code;

        public static void LoadContext(string[] dependencies)
        {
            if (baseReferences is null)
            {
                loadReferences(dependencies);
            }

            loadDepedencies(dependencies);
        }

        private static void loadReferences(string[] dependencies)
        {
            List<PortableExecutableReference> references = new()
            {
                MetadataReference.CreateFromFile(typeof(BattleBitModule).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Player<>).Assembly.Location),
            };

            foreach (string dll in Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location), "*.dll"))
            {

                if (!IsAssemblyValidReference(dll))
                {
                    continue;
                }

                references.Add(MetadataReference.CreateFromFile(dll));

            }

            foreach (string dependency in dependencies)
            {

                references.Add(MetadataReference.CreateFromFile(Path.GetFullPath(dependency)));
            }

            baseReferences = references.ToArray();
        }

        private static void loadDepedencies(string[] dependencies)
        {
            foreach (string dependency in dependencies)
            {
                moduleContext.LoadFromAssemblyPath(Path.GetFullPath(dependency));
            }
        }

        public static void UnloadContext()
        {
            moduleContext.Unload();
            moduleContext = new AssemblyLoadContext("Modules", true);
        }

        public static void RemoveModule(Module module)
        {
            modules.Remove(module);
        }

        public Module(string moduleFilePath)
        {
            this.ModuleFilePath = moduleFilePath;
            this.initialize();
        }

        private void initialize()
        {
            Console.Write("Parsing module from file ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Path.GetFileName(this.ModuleFilePath));
            Console.ResetColor();

            this.code = File.ReadAllText(this.ModuleFilePath);
            this.syntaxTree = CSharpSyntaxTree.ParseText(code, null, this.ModuleFilePath, Encoding.UTF8);
            this.Name = this.getName();
            if (Path.GetFileNameWithoutExtension(this.ModuleFilePath) != this.Name)
            {
                throw new Exception($"Module {Path.GetFileName(this.ModuleFilePath)} does not have the same name as the class {this.Name} that inherits from {nameof(BattleBitModule)}");
            }
            this.getDependencies();

            Console.Write("Module ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(this.Name);
            Console.ResetColor();
            Console.WriteLine($" has {this.RequiredDependencies.Length} required and {this.OptionalDependencies.Length} optional dependencies");
            Console.WriteLine();
        }

        private void getDependencies()
        {
            IEnumerable<AttributeSyntax> attributeSyntaxes = syntaxTree.GetRoot().DescendantNodes().OfType<AttributeSyntax>();
            IEnumerable<AttributeSyntax> requireModuleAttributes = attributeSyntaxes.Where(x => x.Name.ToString() + "Attribute" == nameof(RequireModuleAttribute));
            IEnumerable<AttributeSyntax> publicRequireModuleAttributes = requireModuleAttributes.Where(x => x.Parent?.Parent is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.Modifiers.Any(x => x.ToString() == "public"));
            IEnumerable<string> requiredModuleTypes = publicRequireModuleAttributes.Select(x => x.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString().Trim('"')[6..].Trim('(', ')').Split('.').Last()).Where(x => !string.IsNullOrWhiteSpace(x));
            IEnumerable<AttributeSyntax> moduleReferenceAttributes = attributeSyntaxes.Where(x => x.Name.ToString() + "Attribute" == nameof(ModuleReferenceAttribute));
            IEnumerable<AttributeSyntax> publicModuleReferenceAttributes = moduleReferenceAttributes.Where(x => x.Parent?.Parent is PropertyDeclarationSyntax propertyDeclarationSyntax && propertyDeclarationSyntax.Modifiers.Any(x => x.ToString() == "public"));
            IEnumerable<string> optionalModuleTypes = publicModuleReferenceAttributes.Select(x => (x.Parent?.Parent as PropertyDeclarationSyntax).Identifier.ValueText);

            this.RequiredDependencies = requiredModuleTypes.ToArray();
            this.OptionalDependencies = optionalModuleTypes.Where(m => !this.RequiredDependencies.Contains(m)).ToArray();
        }

        private string getName()
        {
            IEnumerable<ClassDeclarationSyntax> classDeclarationSyntaxes = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            IEnumerable<ClassDeclarationSyntax> publicClassDeclarationSyntaxes = classDeclarationSyntaxes.Where(x => x.Modifiers.Any(x => x.ToString() == "public"));
            IEnumerable<ClassDeclarationSyntax> moduleClassDeclarationSyntaxes = publicClassDeclarationSyntaxes.Where(x => x.BaseList?.Types.Any(x => x.Type.ToString() == nameof(BattleBitModule)) ?? false);

            if (moduleClassDeclarationSyntaxes.Count() != 1)
            {
                throw new Exception($"Module {Path.GetFileName(ModuleFilePath)} does not contain a class that inherits from {nameof(BattleBitModule)}");
            }

            return moduleClassDeclarationSyntaxes.First().Identifier.ToString();
        }

        public void Load()
        {
            Console.Write("Loading module ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(this.Name);
            Console.ResetColor();

            if (this.AssemblyBytes == null)
            {
                throw new Exception("Module has not been compiled yet");
            }

            this.Context = Module.moduleContext;

            using (MemoryStream assemblyStream = new(this.AssemblyBytes))
            using (MemoryStream pdbStream = new(this.PDBBytes))
            {
                this.ModuleAssembly = this.Context.LoadFromStream(assemblyStream, pdbStream);
            }

            // TODO: may be redundant to the checks in getModuleName() (but better safe than sorry?)
            IEnumerable<Type> moduleTypes = this.ModuleAssembly.GetTypes().Where(x => x.IsSubclassOf(typeof(BattleBitModule)));
            if (moduleTypes.Count() != 1)
            {
                throw new Exception($"Module {this.Name} does not contain a class that inherits from {nameof(BattleBitModule)}");
            }

            this.ModuleType = moduleTypes.First();

            modules.Add(this);
        }

        public static PortableExecutableReference[]? baseReferences = null;

        public void Compile(PortableExecutableReference[]? extraReferences = null)
        {
            if (this.AssemblyBytes is not null)
            {
                return;
            }

            Console.Write("Compiling module ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(this.Name);
            Console.ResetColor();
            List<PortableExecutableReference> references = new(baseReferences);

            foreach (Module module in modules)
            {
                using (MemoryStream assemblyStream = new(module.AssemblyBytes))
                {
                    references.Add(MetadataReference.CreateFromStream(assemblyStream));
                }
            }

            if (extraReferences is not null)
            {
                references.AddRange(extraReferences);
            }

            CSharpCompilation compilation = CSharpCompilation.Create(this.Name)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Debug)
                    .WithPlatform(Platform.AnyCpu))
                .WithReferences(references)
                .AddSyntaxTrees(this.syntaxTree);

            using (MemoryStream assemblyStream = new())
            using (MemoryStream pdbStream = new())
            {
                EmitResult result = compilation.Emit(assemblyStream, pdbStream, embeddedTexts: new[] { EmbeddedText.FromSource(this.ModuleFilePath, SourceText.From(code, Encoding.UTF8)) }, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));

                if (!result.Success)
                {
                    var errors = result.Diagnostics
                        .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                        .Select(diagnostic => $"{diagnostic.Id}: {diagnostic.GetMessage()}")
                        .ToList();

                    throw new Exception(string.Join(Environment.NewLine, errors));
                }

                assemblyStream.Seek(0, SeekOrigin.Begin);
                this.AssemblyBytes = assemblyStream.ToArray();

                pdbStream.Seek(0, SeekOrigin.Begin);
                this.PDBBytes = pdbStream.ToArray();
            }
        }

        private static bool IsAssemblyValidReference(string assemblyPath)
        {
            try
            {
                string code = "class Program { static void Main() { } }";

                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
                CSharpCompilation compilation = CSharpCompilation.Create(
                    "TempAssembly",
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { MetadataReference.CreateFromFile(assemblyPath), MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                    options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);

                    return result.Success;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
