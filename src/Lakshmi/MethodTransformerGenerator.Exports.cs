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
    private static void GenerateExports(GeneratorExecutionContext context, ImportExportSyntaxReceiver receiver)
    {
        var compilation = context.Compilation;

        foreach (var classGroup in receiver.ExportMethods.GroupBy(method => method.Parent))
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

                var exportAttribute = methodSymbol?.GetAttributes().FirstOrDefault(ad =>
                    ad.AttributeClass?.ToDisplayString() == "ExportAttribute");

                if (exportAttribute != null)
                {
                    var name = exportAttribute.ConstructorArguments[0].Value?.ToString();
                    var methodName = methodSymbol!.Name;

                    methodsSource.AppendLine($@"
    [UnmanagedCallersOnly(EntryPoint = ""{name}"")]
    public static ulong {methodName}_Entry()
    {{");

                    var paramsCall = GenerateParameterInitCode(methodSymbol, methodsSource, jsonContextType);

                    if (methodSymbol.ReturnsVoid)
                    {
                        methodsSource.AppendLine($"        {className}.{methodName}({paramsCall});");
                    }
                    else
                    {
                        methodsSource.AppendLine($"        var result = {className}.{methodName}({paramsCall});");
                        methodsSource.AppendLine($"        Extism.Pdk.SetOutputJson(result, {jsonContextType}.Default.{methodSymbol.ReturnType.Name});\n");
                    }

                    methodsSource.AppendLine(@"        return 0;
    }");
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

            context.AddSource($"{className}_Exports_generated.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateParameterInitCode(IMethodSymbol? method, StringBuilder builder, string jsonContextType)
    {
        if (method!.Parameters.Length == 0) return string.Empty;

        builder.AppendLine($"        var parameters = Extism.Pdk.GetInputJson<{GetParameterClassName(method)}>({jsonContextType}.Default.{GetParameterClassName(method)});\n");

        return string.Join(",", method!.Parameters.Select(_ => $"parameters.{_.Name}"));
    }
}