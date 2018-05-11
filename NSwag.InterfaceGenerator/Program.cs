using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NSwag.CodeGeneration.CSharp;
using Proxier.Builders;
using Proxier.Managers;

namespace NSwag.InterfaceGenerator
{
    static class StringExtensions
    {
        public static string RemoveAttributes(this string code)
        {
            return Regex.Replace(code, @"^[ \t]+\[.*", string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var document = SwaggerDocument.FromUrlAsync("http://petstore.swagger.io/v2/swagger.json").Result;

            var settings = new SwaggerToCSharpClientGeneratorSettings
            {
                ClassName = "MyClass",
                CSharpGeneratorSettings =
                {
                    Namespace = "MyNamespace",
                    GenerateDataAnnotations = false
                },
                GenerateClientClasses = false,
                GenerateExceptionClasses = false,
                GenerateResponseClasses = false,

            };

            var generator = new SwaggerToCSharpClientGenerator(document, settings);
            var code = generator.GenerateFile().RemoveAttributes();

            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }

            foreach (var type in CodeManager.GenerateAssembly(code).Result.GetTypes().Where(i => !i.IsGenericType && i.Name != settings.ClassName && i.Name != "ApiResponse" && !i.IsNested && i.IsClass && i.Name != settings.ResponseClass))
            {
                var formattableString = $"I{type.Name}";
                var builder = new ClassBuilder().FromType(type).AsInterface().WithName(formattableString).GetAsCode();
                File.WriteAllText($"output/{formattableString}.cs", builder);
            }
        }
    }
}
