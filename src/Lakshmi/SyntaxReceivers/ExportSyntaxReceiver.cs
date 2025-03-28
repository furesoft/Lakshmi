namespace Lakshmi.SyntaxReceiver;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

internal class ImportExportSyntaxReceiver : ISyntaxReceiver
{
    public List<MethodDeclarationSyntax> CandidateMethods { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is MethodDeclarationSyntax methodDeclarationSyntax)
        {
            if (methodDeclarationSyntax.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString() == "Export"))
            {
                CandidateMethods.Add(methodDeclarationSyntax);
            }
        }
    }
}