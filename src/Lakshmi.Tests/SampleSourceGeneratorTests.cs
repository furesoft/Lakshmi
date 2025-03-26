using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lakshmi.Tests;

public class SampleSourceGeneratorTests
{

    [Fact]
    public void GenerateClassesBasedOnDDDRegistry()
    {
        var generator = new MethodTransformerGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(SampleSourceGeneratorTests), [CSharpSyntaxTree.ParseText("""
                                                                                                                  using System;
                                                                                                                  
                                                                                                                  public class Examples
                                                                                                                  {
                                                                                                                      [Export("moss_extension_unregister")]
                                                                                                                      public static void Unregister(){
                                                                                                                  
                                                                                                                      }
                                                                                                                  
                                                                                                                  }
                                                                                                                  """
                                                                                                                  )], [
                                                                                                                      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                                                                                                                      MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
                                                                                                                  ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _);

        // Retrieve all files in the compilation.
        var generatedFiles = newCompilation.SyntaxTrees
            .Select(t => Path.GetFileName(t.FilePath))
            .ToArray();
    }
}