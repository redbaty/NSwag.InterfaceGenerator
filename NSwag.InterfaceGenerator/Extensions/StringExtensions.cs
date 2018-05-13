using System.Text.RegularExpressions;
using NSwag.InterfaceGenerator.Contexts;

namespace NSwag.InterfaceGenerator.Extensions
{
    internal static class StringExtensions
    {
        public static string UseClassWrapper(this string code, ISwaggerInterfaceBuilderContext context)
        {
            var wrapper = new ClassWrapper(code, context.Settings.CSharpGeneratorSettings.Namespace,
                context.Root.GetUsingDirectives());
            return wrapper.Get();
        }

        public static string RemoveAttributes(this string code)
        {
            return Regex.Replace(code, @"^[ \t]+\[.*", string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }
    }
}