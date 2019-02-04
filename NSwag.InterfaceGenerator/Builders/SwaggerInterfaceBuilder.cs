using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp;
using NSwag.InterfaceGenerator.Collectors;
using NSwag.InterfaceGenerator.Contexts;
using NSwag.InterfaceGenerator.Extensions;
using Proxier.Builders;
using Proxier.Managers;
using Serilog;

namespace NSwag.InterfaceGenerator.Builders
{
    public class SwaggerInterfaceBuilder : ISwaggerInterfaceBuilderContext
    {
        private DirectoryInfo _generalOutputDirectory;
        private DirectoryInfo _implementationsOutputDirectory;
        private DirectoryInfo _interfaceOutputDirectory;

        public ILogger Logger { get; }

        /// <inheritdoc />
        public DirectoryInfo InterfaceOutputDirectory
        {
            get => _interfaceOutputDirectory ?? GeneralOutputDirectory.CreateSubdirectory("Interfaces");
            private set => _interfaceOutputDirectory = value;
        }

        public DirectoryInfo ImplementationsOutputDirectory
        {
            get => _implementationsOutputDirectory ?? GeneralOutputDirectory.CreateSubdirectory("Implementations");
            private set => _implementationsOutputDirectory = value;
        }

        /// <inheritdoc />
        public DirectoryInfo GeneralOutputDirectory
        {
            get => _generalOutputDirectory ??
                   new DirectoryInfo(Path.Combine(
                       Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ??
                       throw new InvalidOperationException(), "Output"));
            private set => _generalOutputDirectory = value;
        }

        public CompilationUnitSyntax Root { get; private set; }

        public SwaggerToCSharpClientGeneratorSettings Settings { get; }

        public string Content { get; private set; }

        public SwaggerInterfaceBuilder(ILogger logger = null)
        {
            Settings = new SwaggerToCSharpClientGeneratorSettings
            {
                ClassName = "MyClass",
                CSharpGeneratorSettings =
                {
                    Namespace = "MyNamespace",
                    GenerateDataAnnotations = false,
                    ClassStyle = CSharpClassStyle.Inpc
                },
                GenerateClientClasses = true,
                GenerateExceptionClasses = true,
                GenerateResponseClasses = false
            };

            Logger = logger ?? new LoggerConfiguration()
                         .WriteTo.Console()
                         .CreateLogger();
        }

        public SwaggerInterfaceBuilder CleanOutputDirectories()
        {
            foreach (var propertyInfo in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(i => i.PropertyType == typeof(DirectoryInfo)))
            {
                if (propertyInfo.GetValue(this) is DirectoryInfo val)
                    foreach (var enumerateFile in val.EnumerateFiles())
                    {
                        enumerateFile.Delete();
                    }
            }

            Logger.Information("Output folders has been cleaned.");

            return this;
        }

        public SwaggerInterfaceBuilder WithInterfaceOutput(DirectoryInfo interfaceDirectory)
        {
            InterfaceOutputDirectory = interfaceDirectory;
            return this;
        }

        public SwaggerInterfaceBuilder WithGeneralOutput(DirectoryInfo generalDirectory)
        {
            GeneralOutputDirectory = generalDirectory;
            return this;
        }

        public SwaggerInterfaceBuilder WithImplementationsOutput(DirectoryInfo implementationsDirectory)
        {
            ImplementationsOutputDirectory = implementationsDirectory;
            return this;
        }

        public SwaggerInterfaceBuilder WithNamespace(string nameSpace)
        {
            Settings.CSharpGeneratorSettings.Namespace = nameSpace;

            Logger.Information($"Namespace {nameSpace} will be used.");

            return this;
        }

        public SwaggerInterfaceBuilder WithContent(string content)
        {
            Content = content;
            return this;
        }

        public SwaggerInterfaceBuilder WithUrl(string url)
        {
            using (var httpClient = new HttpClient())
            {
                Content = httpClient.GetStringAsync(url).Result;
            }

            Logger.Information($"Swagger will be gathered at {url}");

            return this;
        }

        public Task Build()
        {
            return Task.Run(async () =>
            {
                Logger.Information("Swagger Interface Builder is starting.");

                var code = RunNSwag(await GetSwaggerSpecification());
                var apiCode = Root.GetApiClass(this);
                var assembly = await CreateAssembly(code);

                WriteNewTypes(GetTypes(assembly));
                WriteImplementations(GetTypes(assembly));
                WriteApiClass(assembly.GetTypes().ToDictionary(i => i.Name, i => $"I{i.Name}"), apiCode);
                WriteExceptionClass(Root.GetSwaggerExceptionClass(this).UseClassWrapper(this));
                Logger.Information("Done!");
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Logger.Fatal(t.Exception,
                        "A fatal error occurred, please report this at https://github.com/redbaty/NSwag.InterfaceGenerator");
            });
        }

        private CompilationUnitSyntax ParseSyntaxTree(string code)
        {
            var compilationUnitSyntax = (CompilationUnitSyntax) CSharpSyntaxTree.ParseText(code).GetRoot();

            Logger.Information("Syntax tree parsed sucessfully.");

            return compilationUnitSyntax;
        }

        private void CreateFileAndWrite(string name, string content)
        {
            EnsureDirectory();

            File.WriteAllText(Path.Combine(GeneralOutputDirectory.FullName, name), content);
        }

        private void WriteApiClass(Dictionary<string, string> typesChanges, string apiCode)
        {
            CreateFileAndWrite("Api.cs",
                new MethodsCollector(typesChanges).Visit(ParseSyntaxTree(apiCode)).ToString().UseClassWrapper(this));

            Logger.Information("API class written.");
        }

        private void WriteExceptionClass(string exceptionClass)
        {
            CreateFileAndWrite("SwaggerException.cs", exceptionClass);

            Logger.Information("API Exception class written.");
        }

        private void EnsureDirectory()
        {
            if (!ImplementationsOutputDirectory.Exists) ImplementationsOutputDirectory.Create();

            Logger.Information($"Results will be written to {ImplementationsOutputDirectory.FullName}");
        }

        private void WriteImplementations(IEnumerable<Type> types)
        {
            foreach (var type in types)
                BuildAndWrite(type, true, ImplementationsOutputDirectory);
        }

        private IEnumerable<Type> GetTypes(Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(i => !i.IsGenericType && i.Name != Settings.ClassName &&
                            !i.IsNested && i.IsClass && i.Name != Settings.ResponseClass &&
                            i.Name != Settings.ExceptionClass);
        }

        private void WriteNewTypes(IEnumerable<Type> types)
        {
            foreach (var type in types)
                BuildAndWrite(type, false, InterfaceOutputDirectory);
        }

        private async Task<Assembly> CreateAssembly(string code)
        {
            var referencedAssemblies =
                new List<Assembly>
                {
                    Assembly.GetAssembly(typeof(HttpClient)), Assembly.GetAssembly(typeof(object)),
                    Assembly.GetAssembly(typeof(AssemblyTargetedPatchBandAttribute)),
                    Assembly.GetAssembly(typeof(INotifyPropertyChanged)),
                    Assembly.GetAssembly(typeof(JsonConvert))
                }.Select(i =>
                    MetadataReference.CreateFromFile(i.Location)).ToList();
            var assembly = await CodeManager.GenerateAssembly(code, referencedAssemblies);

            Logger.Information("Assembly created.");
            return assembly;
        }


        private async Task<SwaggerDocument> GetSwaggerSpecification()
        {
            var document = Content.StartsWith("{")
                ? await SwaggerDocument.FromJsonAsync(Content)
                : await SwaggerYamlDocument.FromYamlAsync(Content);

            Logger.Information("Swagger JSON sucessfully gathered.");
            return document;
        }

        private string RunNSwag(SwaggerDocument document)
        {
            var generator = new SwaggerToCSharpClientGenerator(document, Settings);
            var code = generator.GenerateFile().RemoveAttributes();

            Logger.Information("NSwagger ran sucessfully.");

            Root = ParseSyntaxTree(code);

            return code;
        }

        private void BuildAndWrite(Type type, bool implementation, FileSystemInfo toWrite)
        {
            var newTypeName = implementation ? type.Name : $"I{type.Name}";

            try
            {
                var builder = new ClassBuilder().FromType(type)
                    .OnNamespace(Settings.CSharpGeneratorSettings.Namespace)
                    .Using(Settings.CSharpGeneratorSettings.Namespace)
                    .WithName(newTypeName);

                if (implementation)
                    builder.InheritsFrom($"I{type.Name}");
                else
                    builder.AsInterface();

                File.WriteAllText(Path.Combine(toWrite.FullName, $"{newTypeName}.cs"), builder.GetAsCode());

                Logger.Information($"{newTypeName} sucessfully generated.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    $"Failed to write the type '{ImplementationsOutputDirectory.FullName}\\{newTypeName}'");
            }
        }
    }
}