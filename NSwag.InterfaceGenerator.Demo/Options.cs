using CommandLine;

namespace NSwag.InterfaceGenerator.Demo
{
    internal class Options
    {
        [Option('u', "url", Required = true, HelpText = "The url to get the swagger specification from.")]
        public string SwaggerSpecification { get; set; }

        [Option('n', "namespace")]
        public string Namespace { get; set; }

        [Option('c', "clean", Default = true, HelpText = "Cleans the output directories before creating the new ones.")]
        public bool CleanOutputDirectories { get; set; }
    }
}