/*
MIT License

Copyright (c) 2021 John Lenz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Germinate.Generator
{
  [Generator]
  public class DraftableGenerator : ISourceGenerator
  {
    public const string Namespace = "Germinate";
    public const string PropPrefix = "__germinate_prop__";
    public const string FinishMethod = "__germinate_finish";
    public const string OriginalProp = "__germinate_original";

    private class AttrSyntaxReceiver : ISyntaxReceiver
    {
      public List<RecordDeclarationSyntax> Records { get; } = new List<RecordDeclarationSyntax>();
      public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
      {
        if (syntaxNode is RecordDeclarationSyntax rds && rds.AttributeLists.SelectMany(al => al.Attributes).Any(a => a.Name.ToString().Contains("Draftable")))
        {
          Records.Add(rds);
        }
      }
    }


    public void Initialize(GeneratorInitializationContext context)
    {
      context.RegisterForSyntaxNotifications(() => new AttrSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
      var attrReceiver = (AttrSyntaxReceiver)context.SyntaxReceiver;
      using var log = new System.IO.StreamWriter(System.IO.File.OpenWrite(
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "genlog.txt")));

      context.AddSource("DraftableBase.cs", DraftableBase());

      foreach (var rds in attrReceiver.Records)
      {
        var className = rds.Identifier.ToString();
        var draftName = className + "Draft";
        var interfaceName = "I" + className + "Draft";

        var model = context.Compilation.GetSemanticModel(rds.SyntaxTree);
        var classSymbol = model.GetDeclaredSymbol(rds);
        var fullClassName = classSymbol.ToDisplayString();

        foreach (var a in classSymbol.GetAttributes())
        {
          log.WriteLine(a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        var output = new StringBuilder();
        output.AppendLine($"namespace {Namespace} {{");

        EmitInterface(rds, model, output, interfaceName);

        output.AppendLine();
        output.AppendLine("public static partial class Producer {");

        EmitImpl(rds, model, output, fullClassName, interfaceName, draftName);

        output.AppendLine($"  public static {fullClassName} Produce(this {fullClassName} value, System.Action<{interfaceName}> f)");
        output.AppendLine("  {");
        output.AppendLine($"    var draft = new {draftName}(value, null);");
        output.AppendLine("    f(draft);");
        output.AppendLine($"    return draft.{FinishMethod}();");
        output.AppendLine("  }");

        output.AppendLine("}}"); // close Producer and namespace

        log.WriteLine(output.ToString());
        log.WriteLine("-------------------------------");
        context.AddSource(className + ".Draftable.cs", output.ToString());
      }
    }

    private void EmitInterface(RecordDeclarationSyntax rds, SemanticModel model, StringBuilder output, string interfaceName)
    {
      output.AppendLine($"public interface {interfaceName} {{");

      foreach (var member in rds.Members)
      {
        if (member is PropertyDeclarationSyntax p)
        {
          var name = p.Identifier.ToString();
          var type = model.GetSymbolInfo(p.Type).Symbol as INamedTypeSymbol;
          output.AppendLine($"  {type.ToDisplayString()} {name} {{get; set;}}");
        }
      }

      output.AppendLine("}"); // close interface
    }

    private void EmitImpl(RecordDeclarationSyntax rds, SemanticModel model, StringBuilder output, string fullClassName, string interfaceName, string draftName)
    {
      output.AppendLine($"  private class {draftName} : DraftableBase, {interfaceName} {{");

      foreach (var member in rds.Members)
      {
        if (member is PropertyDeclarationSyntax p)
        {
          var name = p.Identifier.ToString();
          var type = model.GetSymbolInfo(p.Type).Symbol as INamedTypeSymbol;
          var typeName = type.ToDisplayString();
          output.AppendLine($"    private {typeName} {PropPrefix}{name};");
          output.AppendLine($"    public {typeName} {name}");
          output.AppendLine("    {");
          output.AppendLine($"      get => {PropPrefix}{name};");
          output.AppendLine("      set");
          output.AppendLine("      {");
          output.AppendLine("        base.SetDirty();");
          output.AppendLine($"        {PropPrefix}{name} = value;");
          output.AppendLine("      }");
          output.AppendLine("    }");
        }
      }

      // constructor
      output.AppendLine($"    private {fullClassName} {OriginalProp};");
      output.AppendLine($"    public {draftName}({fullClassName} value, DraftableBase parent) : base(parent)");
      output.AppendLine("    {");
      output.AppendLine($"      {OriginalProp} = value;");
      foreach (var member in rds.Members)
      {
        if (member is PropertyDeclarationSyntax p)
        {
          var name = p.Identifier.ToString();
          output.AppendLine($"      {PropPrefix}{name} = value.{name};");
        }
      }
      output.AppendLine("    }"); // close constructor

      // finalize
      output.AppendLine($"    public {fullClassName} {FinishMethod}()");
      output.AppendLine("    {");
      output.AppendLine("      if (base.IsDirty)");
      output.AppendLine("      {");
      output.AppendLine($"        return new {fullClassName}() {{");
      foreach (var member in rds.Members)
      {
        if (member is PropertyDeclarationSyntax p)
        {
          var name = p.Identifier.ToString();
          output.AppendLine($"          {name} = this.{PropPrefix}{name},");
        }
      }
      output.AppendLine("        };"); // close initializer
      output.AppendLine("      } else {"); // close if
      output.AppendLine($"        return {OriginalProp}; }}");
      output.AppendLine("    }"); // close finish method

      output.AppendLine("  }"); // close class
    }

    private string DraftableBase()
    {
      return "namespace " + Namespace + @"{
[System.AttributeUsage(System.AttributeTargets.Class)]
public class DraftableAttribute : System.Attribute { }

public static partial class Producer {
  private abstract class DraftableBase
  {
    private DraftableBase _parent;
    private System.Action _setParentDirty;
    private bool _dirty = false;
    protected bool IsDirty => _dirty;

    protected void SetDirty()
    {
      DraftableBase b = this;
      while (b != null)
      {
        b._dirty = true;
        if (b._setParentDirty != null)
        {
          b._setParentDirty();
        }
        b = b._parent;
      }
    }

    public void SetParent(DraftableBase p)
    {
      _parent = p;
      _setParentDirty = null;
    }

    public void SetParent(System.Action setParentDirty)
    {
      _parent = null;
      _setParentDirty = setParentDirty;
    }

    protected DraftableBase(DraftableBase parent)
    {
      _parent = parent;
      _setParentDirty = null;
    }

    protected DraftableBase(System.Action setParentDirty)
    {
      _parent = null;
      _setParentDirty = setParentDirty;
    }
  }
}}";
    }
  }
}