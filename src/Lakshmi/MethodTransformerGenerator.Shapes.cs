using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lakshmi;

public partial class MethodTransformerGenerator
{
    private static Dictionary<string, IMethodSymbol> _exportMethods = new();

    private static void GenerateParameterClassesShapes(GeneratorExecutionContext context, List<MethodDeclarationSyntax> methods)
    {
        var sb = new StringBuilder();

        foreach (var method in methods)
        {
            var model = context.Compilation.GetSemanticModel(method.SyntaxTree);
            var methodSymbol = (IMethodSymbol)model.GetDeclaredSymbol(method)!;

            if (methodSymbol == null) continue;
            if (methodSymbol.Parameters.Length == 0) continue;

            var className = GetParameterClassName(methodSymbol);
            _exportMethods.Add(className, methodSymbol);

            sb.AppendLine($@"public partial class {className} : PolyType.IShapeable<{className}> {{");

            sb.AppendLine($@"    public static PolyType.Abstractions.ITypeShape<{className}> GetShape()
    {{
        return System.TypeShapeProvider.Default.{className};
    }}");

            sb.AppendLine("}");
        }

        context.AddSource("ParameterClasses.Shapes.g.cs", @"
using System;

namespace System;" + "\n\n" + sb);
    }

    private static void GenerateParameterClassesShapesProvider(GeneratorExecutionContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("public partial class TypeShapeProvider : PolyType.ITypeShapeProvider {{");

        sb.AppendLine("    public static TypeShapeProvider Default { get; } = new();");
        sb.AppendLine();

        foreach (var method in _exportMethods)
        {
            var methodSymbol = method.Value;

            if (methodSymbol == null) continue;
            if (methodSymbol.Parameters.Length == 0) continue;

            var className = method.Key;

            sb.AppendLine($@"    public PolyType.Abstractions.ITypeShape<{className}> {className} =>
    
        new PolyType.SourceGenModel.SourceGenObjectTypeShape<{className}>()
            {{
                IsRecordType = false,
                IsTupleType = false,
                Provider = this,
                //CreatePropertiesFunc = Create{className}Properties,
                //CreateConstructorFunc = CreateConstructor_{className},
            }};");

            foreach (var parameter in methodSymbol.Parameters)
            {

            }


        }

        sb.AppendLine(@"    public PolyType.Abstractions.ITypeShape? GetShape(Type type)
    {");
        foreach (var method in _exportMethods)
        {
            sb.AppendLine($@"        if (type == typeof({method.Key}))
        {{
            return {method.Key};
        }}");
        }

        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("ParameterClasses.ShapesProvder.g.cs", @"
using System;

namespace System;" + "\n\n" + sb);
    }

}

