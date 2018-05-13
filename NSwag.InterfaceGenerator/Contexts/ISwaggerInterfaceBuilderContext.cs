using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSwag.CodeGeneration.CSharp;
using Serilog;

namespace NSwag.InterfaceGenerator.Contexts
{
    internal interface ISwaggerInterfaceBuilderContext
    {
        ILogger Logger { get; }
        DirectoryInfo OutputDirectory { get; }
        CompilationUnitSyntax Root { get; }
        SwaggerToCSharpClientGeneratorSettings Settings { get; }
        string Url { get; }
    }
}