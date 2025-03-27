using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Lakshmi;

[Generator]
public class ExportMethodTransformerGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(_ =>
        {
            GenerateEntryPoint(_);
            GenerateAttributes(_);
        });

        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            return;

        var compilation = context.Compilation;

        GenerateParameterClasses(context, receiver);

        foreach (var classGroup in receiver.CandidateMethods.GroupBy(method => method.Parent))
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

    private static void GenerateParameterClasses(GeneratorExecutionContext context, SyntaxReceiver receiver)
    {
        var sb = new StringBuilder();

        foreach (var method in receiver.CandidateMethods)
        {
            var model = context.Compilation.GetSemanticModel(method.SyntaxTree);
            var methodSymbol = model.GetDeclaredSymbol(method);

            if (methodSymbol == null) continue;
            if (methodSymbol.Parameters.Length == 0) continue;

            var className = GetParameterClassName(methodSymbol);

            sb.AppendLine($@"public class {className} {{");

            foreach (var parameter in methodSymbol.Parameters)
            {
                sb.AppendLine($@"
    [JsonPropertyName(""{parameter.Name}"")]
    public {parameter.Type} {parameter.Name} {{ get; set; }}
");
            }

            sb.AppendLine("}");
        }

        context.AddSource("ParameterClasses.g.cs", @"
using System;
using System.Text.Json.Serialization;

namespace System;" + "\n\n" + sb);
    }

    private static string GetParameterClassName(IMethodSymbol method)
    {
        return $"{method.ContainingType.Name}{method.Name}Parameters";
    }

    private static string GetJsonContextType(Compilation compilation, ClassDeclarationSyntax classDeclaration)
    {
        var classSymbol = compilation.GetSemanticModel(classDeclaration.SyntaxTree).GetDeclaredSymbol(classDeclaration);
        var jsonContextAttribute = classSymbol?.GetAttributes().FirstOrDefault(ad =>
            ad.AttributeClass?.ToDisplayString() == "JsonContextAttribute");

        return jsonContextAttribute != null
            ? jsonContextAttribute.ConstructorArguments[0].Value?.ToString() ?? "JsonContext.Default"
            : "JsonContext.Default";
    }

    private static void GenerateEntryPoint(GeneratorPostInitializationContext context)
    {
        context.AddSource("Entry.g.cs", """
                                      public static class EntryPoint {
                                        public static void Main() {}                                  
                                        }
                                      """);
    }

    private static void GenerateAttributes(GeneratorPostInitializationContext context)
    {
        context.AddSource("ExportAttribute.g.cs", """
                                                using System;

                                                [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
                                                public sealed class ExportAttribute : Attribute
                                                {
                                                    public string Name { get; }
                                                
                                                    public ExportAttribute(string name) => Name = name;
                                                }
                                                """);

        context.AddSource("JsonContextAttribute.g.cs", """
                                                using System;

                                                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                                public sealed class JsonContextAttribute : Attribute
                                                {
                                                    public Type JsonContextType { get; }
                                                
                                                    public JsonContextAttribute(Type jsonContextType)
                                                    {
                                                        JsonContextType = jsonContextType;
                                                    }
                                                }
                                                """);
    }


    private class SyntaxReceiver : ISyntaxReceiver
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
}