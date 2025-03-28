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

[Generator]
public partial class MethodTransformerGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(_ =>
        {
            GenerateEntryPoint(_);
            GenerateAttributes(_);
        });

        context.RegisterForSyntaxNotifications(() => new ExportSyntaxReceiver());
        context.RegisterForSyntaxNotifications(() => new ImportSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        List<MethodDeclarationSyntax> candidateMethods = new();

        if (context.SyntaxReceiver is ExportSyntaxReceiver receiver)
        {
            candidateMethods.AddRange(receiver.CandidateMethods);
            GenerateExports(context, receiver);
        }

        if (context.SyntaxReceiver is ImportSyntaxReceiver importReceiver)
        {
            candidateMethods.AddRange(importReceiver.CandidateMethods);
            GenerateImports(context, importReceiver);
        }

        GenerateParameterClasses(context, candidateMethods);
    }

    private static void GenerateParameterClasses(GeneratorExecutionContext context, List<MethodDeclarationSyntax> methods)
    {
        var sb = new StringBuilder();

        foreach (var method in methods)
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
                                                public sealed class ExportAttribute(string name) : Attribute
                                                {
                                                    public string Name { get; } = name;
                                                }
                                                """);

        context.AddSource("ImportAttribute.g.cs", """
                                                using System;

                                                [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
                                                public sealed class ImportAttribute(string @namespace) : Attribute
                                                {
                                                    public string Namespace { get; } = @namespace;
                                                    public string Entry { get; set; }
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

}