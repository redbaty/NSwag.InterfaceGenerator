using System.IO;
using CommandLine;
using NSwag.InterfaceGenerator.Builders;

namespace NSwag.InterfaceGenerator.Demo
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptionsAndReturnExitCode);
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            var builder = new SwaggerInterfaceBuilder();
            builder = opts.SwaggerSpecification.StartsWith("http") ? builder.WithUrl(opts.SwaggerSpecification) : builder.WithContent(File.ReadAllText(opts.SwaggerSpecification));

            if (opts.CleanOutputDirectories)
                builder.CleanOutputDirectories();

            if (!string.IsNullOrEmpty(opts.Namespace))
                builder.WithNamespace(opts.Namespace);

            builder.CleanOutputDirectories().Build().Wait();
        }
    }
}