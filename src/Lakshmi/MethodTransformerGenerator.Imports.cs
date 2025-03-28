using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Lakshmi.SyntaxReceiver;

namespace Lakshmi;

public partial class MethodTransformerGenerator : ISourceGenerator
{
    private static void GenerateImports(GeneratorExecutionContext context, ImportExportSyntaxReceiver receiver)
    {
        /*
    [DllImport(Functions.DLL, EntryPoint = "moss_api_content_new_notebook")]
    public static extern ulong NewContentNotebook(ulong pageCount); // -> int
        */

        var compilation = context.Compilation;

        foreach (var classGroup in receiver.ImportMethods.GroupBy(method => method.Parent))
        {
            if (classGroup.Key is not ClassDeclarationSyntax classDeclaration) continue;

            var className = classDeclaration.Identifier.Text;
            var namespaceName = compilation.GetSemanticModel(classDeclaration.SyntaxTree)
                .GetDeclaredSymbol(classDeclaration)!
                .ContainingNamespace;

            var jsonContextType = GetJsonContextType(compilation, classDeclaration);

            var methodsSource = new StringBuilder();

            foreach (var method in classGroup)
            {
                var model = compilation.GetSemanticModel(method.SyntaxTree);
                var methodSymbol = model.GetDeclaredSymbol(method);

                var importAttribute = methodSymbol?.GetAttributes().FirstOrDefault(ad =>
                    ad.AttributeClass?.ToDisplayString() == "ImportAttribute");

                if (importAttribute != null)
                {
                    var methodName = methodSymbol!.Name;

                    var @namespace = importAttribute.ConstructorArguments[0].Value?.ToString();
                    var entryPoint = importAttribute.NamedArguments.Length == 1 ? importAttribute.NamedArguments[0].Value.Value.ToString() : methodName;
                    var returnType = methodSymbol.ReturnsVoid ? "void" : "ulong";
                    var wrappedReturnType = methodSymbol.ReturnsVoid ? "void" : methodSymbol.ReturnType.ToDisplayString();

                    methodsSource.AppendLine($@"
    [DllImport({@namespace}, EntryPoint = ""{entryPoint}"")]
    public static extern {returnType} {methodSymbol.Name}({GetImportParameterList(methodSymbol)}); // -> {methodSymbol.ReturnType}");

                    methodsSource.AppendLine($@"
    public static partial {wrappedReturnType} {methodSymbol.Name}()
    {{");

                    methodsSource.AppendLine("    }");
                }
            }

            var source = @$"
using System.Runtime.InteropServices;
using System;

namespace {namespaceName};

public static partial class {className}
{{
    {methodsSource}
}}";

            context.AddSource($"{className}_Imports_generated.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GetImportParameterList(IMethodSymbol methodSymbol)
    {
        var parameters = methodSymbol.Parameters
            .Select(p => $"{p.Type} {p.Name}")
            .ToArray();

        return string.Join(", ", parameters);
    }
}