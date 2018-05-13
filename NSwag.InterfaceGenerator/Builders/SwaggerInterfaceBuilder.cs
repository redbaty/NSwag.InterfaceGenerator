using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        private DirectoryInfo _outputDirectory;
        public ILogger Logger { get; }

        public DirectoryInfo OutputDirectory
        {
            get => _outputDirectory ??
                   new DirectoryInfo($"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\Output");
            private set => _outputDirectory = value;
        }

        public CompilationUnitSyntax Root { get; private set; }

        public SwaggerToCSharpClientGeneratorSettings Settings { get; }

        public string Url { get; private set; }

        public SwaggerInterfaceBuilder(ILogger logger = null)
        {
            Settings = new SwaggerToCSharpClientGeneratorSettings
            {
                ClassName = "MyClass",
                CSharpGeneratorSettings =
                {
                    Namespace = "MyNamespace",
                    GenerateDataAnnotations = false
                },
                GenerateClientClasses = true,
                GenerateExceptionClasses = true,
                GenerateResponseClasses = false
            };

            Logger = logger ?? new LoggerConfiguration()
                         .WriteTo.Console()
                         .CreateLogger();
        }

        public SwaggerInterfaceBuilder WithOutput(DirectoryInfo outputDirectory)
        {
            OutputDirectory = outputDirectory;
            return this;
        }

        public SwaggerInterfaceBuilder WithNamespace(string nameSpace)
        {
            Settings.CSharpGeneratorSettings.Namespace = nameSpace;

            Logger.Information($"Namespace {nameSpace} will be used.");

            return this;
        }

        public SwaggerInterfaceBuilder WithUrl(string url)
        {
            Url = url;

            Logger.Information($"Swagger will be gathered at {Url}");

            return this;
        }

        public Task Build()
        {
            return Task.Run(async () =>
            {
                Logger.Information("Swagger Interface Builder is starting.");

                var document = await GetSwaggerSpecification();
                var code = RunNSwag(document);

                Root = ParseSyntaxTree(code);

                var apiCode = Root.GetApiClass(this);

                var assembly = await CreateAssembly(code);

                EnsureDirectory();

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

        private static CompilationUnitSyntax ParseSyntaxTree(string code)
        {
            return (CompilationUnitSyntax) CSharpSyntaxTree.ParseText(code).GetRoot();
        }

        private void WriteApiClass(Dictionary<string, string> typesChanges, string apiCode)
        {
            File.WriteAllText($"{OutputDirectory.FullName}\\Api.cs",
                new MethodsCollector(typesChanges).Visit(ParseSyntaxTree(apiCode)).ToString().UseClassWrapper(this));

            Logger.Information("API class written.");
        }

        private void WriteExceptionClass(string exceptionClass)
        {
            File.WriteAllText($"{OutputDirectory.FullName}\\SwaggerException.cs", exceptionClass);

            Logger.Information("API class written.");
        }

        private void EnsureDirectory()
        {
            if (!OutputDirectory.Exists) OutputDirectory.Create();

            Logger.Information($"Results will be written to {OutputDirectory.FullName}");
        }

        private void WriteImplementations(IEnumerable<Type> types)
        {
            foreach (var type in types)
                BuildAndWrite(type, true);
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
                BuildAndWrite(type, false);
        }

        private async Task<Assembly> CreateAssembly(string code)
        {
            var assembly = await CodeManager.GenerateAssembly(code);

            Logger.Information("Assembly created.");
            return assembly;
        }

        private async Task<SwaggerDocument> GetSwaggerSpecification()
        {
            var document = await SwaggerDocument.FromUrlAsync(Url);

            Logger.Information("Swagger JSON sucessfully gathered.");
            return document;
        }

        private string RunNSwag(SwaggerDocument document)
        {
            var generator = new SwaggerToCSharpClientGenerator(document, Settings);
            var code = generator.GenerateFile().RemoveAttributes();

            Logger.Information("NSwagger ran sucessfully.");
            return code;
        }

        private void BuildAndWrite(Type type, bool implementation)
        {
            var newTypeName = implementation ? type.Name : $"I{type.Name}";

            try
            {
                var builder = new ClassBuilder().FromType(type)
                    .OnNamespace(Settings.CSharpGeneratorSettings.Namespace)
                    .Using(Settings.CSharpGeneratorSettings.Namespace)
                    .WithName(newTypeName);

                if (!implementation)
                    builder.AsInterface();

                File.WriteAllText($"{OutputDirectory.FullName}/{newTypeName}.cs", builder.GetAsCode());

                Logger.Information($"{newTypeName} sucessfully generated.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to write the type '{OutputDirectory.FullName}\\{newTypeName}'");
            }
        }
    }
}