using System.Linq;
using System.Text;
using Lakshmi.SyntaxReceiver;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
                    var entryPoint = importAttribute.NamedArguments.Length == 1
                        ? importAttribute.NamedArguments[0].Value.Value.ToString()
                        : methodName;
                    var returnType = GetReturnType(methodSymbol);
                    var wrappedReturnType =
                        methodSymbol.ReturnsVoid ? "void" : methodSymbol.ReturnType.ToDisplayString();

                    var modifier = GetModifier(methodSymbol);

                    methodsSource.AppendLine($@"
    [DllImport(""{@namespace}"", EntryPoint = ""{entryPoint}"")]
    private static extern {returnType} {methodSymbol.Name}FFI({GetImportParameterList(methodSymbol)}); // -> {methodSymbol.ReturnType}");

                    methodsSource.AppendLine($@"
    {modifier} static {wrappedReturnType} {methodSymbol.Name}()
    {{");
                    var paramsCall = GenerateParameterInitCode(methodSymbol, methodsSource);

                    if (methodSymbol.ReturnsVoid)
                    {
                        methodsSource.AppendLine($"        {methodName}FFI({paramsCall});");
                    }
                    else
                    {
                        methodsSource.AppendLine($"        var result = {methodName}FFI({paramsCall});");
                        if (!IsWasmPrimitive(methodSymbol))
                        {
                            methodsSource.AppendLine("        var block = Extism.MemoryBlock.Find(result);");
                            methodsSource.AppendLine("        var json = block.ReadString();");
                            methodsSource.AppendLine();
                            methodsSource.AppendLine(
                                $"        return PolyType.Examples.JsonSerializer.JsonSerializerTS.Deserialize<{methodSymbol.ReturnType.Name}>(json);");
                        }
                        else
                        {
                            methodsSource.AppendLine("        return result;");
                        }
                    }

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

    private static string GetReturnType(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.ReturnsVoid) return "void";

        if (IsWasmPrimitive(methodSymbol))
        {
            return methodSymbol.ReturnType.ToDisplayString();
        }

        return "ulong";
    }

    private static string GetModifier(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.DeclaredAccessibility == Accessibility.Public)
            return "public";

        if (methodSymbol.DeclaredAccessibility == Accessibility.Internal)
            return "internal";

        if (methodSymbol.DeclaredAccessibility == Accessibility.Private)
            return "private";

        if (methodSymbol.DeclaredAccessibility == Accessibility.Protected)
            return "protected";

        if (methodSymbol.DeclaredAccessibility == Accessibility.NotApplicable) return string.Empty;

        return string.Empty;
    }

    private static string GetImportParameterList(IMethodSymbol methodSymbol)
    {
        var parameters = methodSymbol.Parameters
            .Select(p => $"{p.Type} {p.Name}")
            .ToArray();

        return string.Join(", ", parameters);
    }
}