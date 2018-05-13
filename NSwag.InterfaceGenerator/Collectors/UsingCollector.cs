using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NSwag.InterfaceGenerator.Collectors
{
    internal class UsingCollector : CSharpSyntaxWalker
    {
        private readonly List<string> _usings = new List<string>
        {
            "using System;"
        };

        public IEnumerable<string> Usings => _usings.Distinct();

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            _usings.Add(node.Name.ToString());
        }
    }
}