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

      var records = BuildRecords.RecordsToDraft(context.Compilation, attrReceiver.Records);

      foreach (var rds in records.Values)
      {
        /*
        foreach (var a in classSymbol.GetAttributes())
        {
          log.WriteLine(a.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }
        */

        var output = new StringBuilder();
        output.AppendLine($"namespace {Names.Namespace} {{");

        // Interface
        output.AppendLine($"public interface {rds.InterfaceName} {{");
        EmitProperties(EmitPhase.Interface, rds, output, records);
        output.AppendLine("}"); // close interface

        output.AppendLine();
        output.AppendLine("public static partial class Producer {");

        output.AppendLine($"  private class {rds.DraftName} : {Names.DraftableBase}, {rds.InterfaceName} {{");

        EmitProperties(EmitPhase.PropImplementation, rds, output, records);

        // constructor
        output.AppendLine($"    private readonly {rds.FullClassName} {Names.OriginalProp};");
        output.AppendLine($"    public {rds.DraftName}({rds.FullClassName} value, {Names.DraftableBase} parent, {Names.CheckDirtyStruct}? checkDirty = null) : base(parent, checkDirty)");
        output.AppendLine("    {");
        output.AppendLine($"      {Names.OriginalProp} = value;");
        EmitProperties(EmitPhase.Constructor, rds, output, records);
        output.AppendLine("    }"); // close constructor

        // finish
        output.AppendLine($"    public {rds.FullClassName} {Names.FinishMethod}()");
        output.AppendLine("    {");
        output.AppendLine($"      if (base.{Names.IsDirtyProp})");
        output.AppendLine("      {");
        output.AppendLine($"        return new {rds.FullClassName}() {{");
        EmitProperties(EmitPhase.Finish, rds, output, records);
        output.AppendLine("        };"); // close initializer
        output.AppendLine("      } else {");
        output.AppendLine($"        return {Names.OriginalProp};");
        output.AppendLine("      }"); // close else
        output.AppendLine("    }"); // close finish method

        output.AppendLine("  }"); // close class

        // Producer
        output.AppendLine($"  public static {rds.FullClassName} Produce(this {rds.FullClassName} value, System.Action<{rds.InterfaceName}> f)");
        output.AppendLine("  {");
        output.AppendLine($"    var check = new {Names.CheckDirtyStruct}() {{ Checks = new System.Collections.Generic.List<System.Action>() }};");
        output.AppendLine($"    var draft = new {rds.DraftName}(value, null, check);");
        output.AppendLine("    f(draft);");
        output.AppendLine("    foreach (var a in check.Checks) a();");
        output.AppendLine($"    return draft.{Names.FinishMethod}();");
        output.AppendLine("  }");

        output.AppendLine("}}"); // close Producer and namespace

        log.WriteLine(output.ToString());
        log.WriteLine("-------------------------------");
        context.AddSource(rds.ClassName + ".Draftable.cs", output.ToString());
      }
    }

    private static IReadOnlyList<string> _immutableCollections =
      new[] {
        "global::System.Collections.Immutable.ImmutableArray",
        "global::System.Collections.Immutable.ImmutableDictionary",
        "global::System.Collections.Immutable.ImmutableHashSet",
        "global::System.Collections.Immutable.ImmutableList",
        "global::System.Collections.Immutable.ImmutableQueue",
        "global::System.Collections.Immutable.ImmutableSortedDictionary",
        "global::System.Collections.Immutable.ImmutableSortedSet",
        "global::System.Collections.Immutable.ImmutableStack"
      };

    private void EmitProperties(EmitPhase phase, RecordToDraft rds, StringBuilder output, IReadOnlyDictionary<string, RecordToDraft> allRecords)
    {
      foreach (var prop in rds.Properties)
      {
        if (allRecords.TryGetValue(prop.FullPropertyTypeName, out var propRecord))
        {
          PropDraftable.Emit(phase, rds, prop, propRecord, output);
        }
        else if (_immutableCollections.Any(t => prop.FullPropertyTypeName.StartsWith(t)))
        {
          PropImmutableCollection.Emit(phase, prop, output);
        }
        else
        {
          PropNonDraftable.Emit(phase, prop, output);
        }
      }
    }

    private string DraftableBase()
    {
      return $@"namespace {Names.Namespace} {{
[System.AttributeUsage(System.AttributeTargets.Class)]
public class DraftableAttribute : System.Attribute {{ }}

public static partial class Producer {{
  private struct {Names.CheckDirtyStruct}
  {{
    public System.Collections.Generic.List<System.Action> Checks;
  }}


  private abstract class {Names.DraftableBase}
  {{
    private {Names.DraftableBase} _parent;
    private {Names.CheckDirtyStruct} _checkDirty;
    private bool _dirty = false;

    protected bool {Names.IsDirtyProp} => _dirty;

    protected void {Names.SetDirtyMethod}()
    {{
      {Names.DraftableBase} b = this;
      while (b != null)
      {{
        b._dirty = true;
        b = b._parent;
      }}
    }}

    protected void {Names.AddCheckDirtyMethod}(System.Action a)
    {{
      _checkDirty.Checks.Add(a);
    }}

    protected {Names.DraftableBase}({Names.DraftableBase} parent, {Names.CheckDirtyStruct}? checkDirty)
    {{
      _parent = parent;
      _checkDirty = parent == null ? checkDirty.Value : parent._checkDirty;
    }}
  }}
}}}}";
    }
  }
}