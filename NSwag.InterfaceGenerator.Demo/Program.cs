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
            var builder = new SwaggerInterfaceBuilder().WithUrl(opts.SwaggerSpecification);

            if (opts.CleanOutputDirectories)
                builder.CleanOutputDirectories();

            if (!string.IsNullOrEmpty(opts.Namespace))
                builder.WithNamespace(opts.Namespace);

            builder.CleanOutputDirectories().Build().Wait();
        }
    }
}