using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NSwag.InterfaceGenerator.Collectors
{
    internal class MethodsCollector : CSharpSyntaxRewriter
    {
        private bool DoChangeReturnType { get; }

        private Dictionary<string, string> TypesToReplace { get; }

        public MethodsCollector(Dictionary<string, string> typesToReplace, bool changeReturnType = false)
        {
            TypesToReplace = typesToReplace;
            DoChangeReturnType = changeReturnType;
        }


        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (DoChangeReturnType)
                node = ChangeReturnType(node);

            foreach (var parameterListParameter in node.ParameterList.Parameters)
                if (TypesToReplace.ContainsKey(parameterListParameter.Type.ToString()))
                    node = node.ReplaceNode(parameterListParameter, parameterListParameter.Update(
                        parameterListParameter.AttributeLists,
                        parameterListParameter.Modifiers, SyntaxFactory.IdentifierName(
                            TypesToReplace[parameterListParameter.Type.ToString()] + " "),
                        parameterListParameter.Identifier,
                        parameterListParameter.Default));

            node = node.Update(node.AttributeLists, node.Modifiers, node.ReturnType, node.ExplicitInterfaceSpecifier,
                node.Identifier, node.TypeParameterList, node.ParameterList, node.ConstraintClauses, node.Body,
                node.SemicolonToken);


            return base.VisitMethodDeclaration(node);
        }

        private MethodDeclarationSyntax ChangeReturnType(MethodDeclarationSyntax node)
        {
            if (TypesToReplace.Keys.FirstOrDefault(i => node.ReturnType.ToString().Contains(i)) is string stj)
            {
                var newType = node.ReturnType.ToString().Replace(stj, TypesToReplace[stj]);

                node = node.ReplaceNode(node, node.WithReturnType(
                    SyntaxFactory.IdentifierName(newType)));
            }

            return node;
        }
    }
}