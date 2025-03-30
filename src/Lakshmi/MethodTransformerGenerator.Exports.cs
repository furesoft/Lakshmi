using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Lakshmi.SyntaxReceiver;

namespace Lakshmi;

public partial class MethodTransformerGenerator
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

                    var paramsCall = GenerateParameterInitCode(methodSymbol, methodsSource);

                    if (methodSymbol.ReturnsVoid)
                    {
                        methodsSource.AppendLine($"        {className}.{methodName}({paramsCall});");
                    }
                    else if (!IsWasmPrimitive(methodSymbol))
                    {
                        methodsSource.AppendLine($"        var result = {className}.{methodName}({paramsCall});");
                        methodsSource.AppendLine($"        var json = PolyType.Examples.JsonSerializer.JsonSerializerTS.Serialize(result);");
                        methodsSource.AppendLine($"        Extism.Pdk.SetOutput(json);\n");
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

    private static bool IsWasmPrimitive(IMethodSymbol methodSymbol)
    {
        return methodSymbol.ReturnType.Name switch
            {
                "Int32" => true,
                "Int64" => true,
                "UInt32" => true,
                "UInt64" => true,
                "Byte" => true,
                "SByte" => true,
                "Double" => true,
                "Single" => true,
                _ => false
            };
    }

    private static string GenerateParameterInitCode(IMethodSymbol? method, StringBuilder builder)
    {
        if (method!.Parameters.Length == 0) return string.Empty;

        builder.AppendLine("        var input = Extism.Pdk.GetInput();\n");
        builder.AppendLine($"        var parameters = PolyType.Examples.JsonSerializer.JsonSerializerTS.Deserialize<{GetParameterClassName(method)}>(System.Text.Encoding.UTF8.GetString(input));\n");

        return string.Join(",", method!.Parameters.Select(_ => $"parameters.{_.Name}"));
    }
}