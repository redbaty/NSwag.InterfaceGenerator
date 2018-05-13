using System.Collections.Generic;
using System.Text;

namespace NSwag.InterfaceGenerator
{
    public class ClassWrapper
    {
        private string ClassCode { get; }

        private string Namespace { get; }

        private IEnumerable<string> Usings { get; }

        public ClassWrapper(string classCode, string ns, IEnumerable<string> usings)
        {
            ClassCode = classCode;
            Namespace = ns;
            Usings = usings;
        }

        public string Get()
        {
            return Build();
        }

        private string Build()
        {
            var sb = new StringBuilder();
            AddUsings(sb);
            AddNamespaceHeader(sb);
            AddCode(sb);
            EndNamespace(sb);
            return sb.ToString();
        }

        private static void EndNamespace(StringBuilder sb)
        {
            sb.AppendLine("}");
        }

        private void AddCode(StringBuilder sb)
        {
            foreach (var c in ClassCode.Split('\n'))
                sb.AppendLine(c.Contains("\t") || c.Contains("    ") ? $"\t{c}" : $"\t\t{c}");
        }

        private void AddNamespaceHeader(StringBuilder sb)
        {
            sb.AppendLine($"namespace {Namespace}");
            sb.AppendLine("{");
        }

        private void AddUsings(StringBuilder sb)
        {
            foreach (var @using in Usings) sb.AppendLine(@using.StartsWith("using") ? @using : $"using {@using};");

            sb.AppendLine();
        }
    }
}