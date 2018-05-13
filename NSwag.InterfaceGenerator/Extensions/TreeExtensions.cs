using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSwag.InterfaceGenerator.Collectors;
using NSwag.InterfaceGenerator.Contexts;

namespace NSwag.InterfaceGenerator.Extensions
{
    internal static class TreeExtensions
    {
        public static IEnumerable<string> GetUsingDirectives(this CompilationUnitSyntax tree)
        {
            var collector = new UsingCollector();
            collector.Visit(tree);
            return collector.Usings;
        }

        public static string GetApiClass(this CompilationUnitSyntax tree, ISwaggerInterfaceBuilderContext context)
        {
            return GetClass(tree, context.Settings.ClassName);
        }

        public static string GetSwaggerExceptionClass(this CompilationUnitSyntax tree,
            ISwaggerInterfaceBuilderContext context)
        {
            return GetClass(tree, context.Settings.ExceptionClass);
        }

        private static string GetClass(SyntaxNode root, string className)
        {
            return root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(i => i.Identifier.ToString() == className)?.ToString();
        }
    }
}