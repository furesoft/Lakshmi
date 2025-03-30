using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Lakshmi.SyntaxReceiver;

namespace Lakshmi;

[Generator]
public partial class MethodTransformerGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(_ =>
        {
            GenerateEntryPoint(_);
            GenerateAttributes(_);
        });

        context.RegisterForSyntaxNotifications(() => new ImportExportSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        List<MethodDeclarationSyntax> candidateMethods = new();

        if (context.SyntaxReceiver is ImportExportSyntaxReceiver receiver)
        {
            candidateMethods.AddRange(receiver.ExportMethods);
            GenerateExports(context, receiver);

            candidateMethods.AddRange(receiver.ImportMethods);
            GenerateImports(context, receiver);
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
    }
}