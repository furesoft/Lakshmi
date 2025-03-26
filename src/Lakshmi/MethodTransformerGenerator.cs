// MethodTransformerGenerator.cs

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Lakshmi;

[Generator]
public class MethodTransformerGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(_ =>
        {
            _.AddSource("ExportAttribute.g.cs", """
                                                using System;

                                                [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
                                                public sealed class ExportAttribute : Attribute
                                                {
                                                    public string Name { get; }
                                                
                                                    public ExportAttribute(string name) => Name = name;
                                                }
                                                """);
        });

        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            return;

        var compilation = context.Compilation;

        foreach (var method in receiver.CandidateMethods)
        {
            var model = compilation.GetSemanticModel(method.SyntaxTree);
            var methodSymbol = model.GetDeclaredSymbol(method);

            var exportAttribute = methodSymbol?.GetAttributes().FirstOrDefault(ad =>
                ad.AttributeClass?.ToDisplayString() == "ExportAttribute");

            if (exportAttribute != null)
            {
                var name = exportAttribute.ConstructorArguments[0].Value?.ToString();
                var methodName = methodSymbol!.Name;

                var source = $$"""

                               using System.Runtime.InteropServices;

                               namespace {{methodSymbol.ContainingNamespace}};
                               
                               public partial class {{methodSymbol.ContainingType.Name}}Exports
                               {
                                   [UnmanagedCallersOnly(EntryPoint = "{{name}}")]
                                   public static ulong {{methodName}}_Entry()
                                   {
                                       {{methodSymbol.ContainingType.Name}}.{{methodName}}();
                               
                                       return 0;
                                   }
                               }
                               """;

                context.AddSource($"{methodName}_generated.cs", SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    private class SyntaxReceiver : ISyntaxReceiver
    {
        public List<MethodDeclarationSyntax> CandidateMethods { get; } = new List<MethodDeclarationSyntax>();

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
}