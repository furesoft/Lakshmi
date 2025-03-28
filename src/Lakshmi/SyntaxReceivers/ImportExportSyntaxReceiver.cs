namespace Lakshmi.SyntaxReceiver;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

internal class ImportExportSyntaxReceiver : ISyntaxReceiver
{
    public List<MethodDeclarationSyntax> ExportMethods { get; } = new();
    public List<MethodDeclarationSyntax> ImportMethods { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is MethodDeclarationSyntax methodDeclarationSyntax)
        {
            var attributes = methodDeclarationSyntax.AttributeLists
                .SelectMany(al => al.Attributes);

            if (attributes.Any(a => a.Name.ToString() == "Export"))
            {
                ExportMethods.Add(methodDeclarationSyntax);
            }
            else if (attributes.Any(a => a.Name.ToString() == "Import"))
            {
                ImportMethods.Add(methodDeclarationSyntax);
            }
        }
    }
}