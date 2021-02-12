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
      var records = BuildRecords.RecordsToDraft(context.Compilation, attrReceiver.Records);

      context.AddSource("DraftableBase.cs", DraftableBase());

      //var log = new System.IO.StreamWriter(System.IO.File.OpenWrite(
      //  System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "genlog.txt")));

      foreach (var rds in records.Values)
      {
        var output = new StringBuilder();
        output.AppendLine("#nullable enable");

        // Interface
        if (!string.IsNullOrEmpty(rds.Namespace))
        {
          output.AppendLine($"namespace {rds.Namespace} {{");
        }
        output.AppendLine($"public interface {rds.InterfaceName} {{");
        EmitProperties(EmitPhase.Interface, rds, output, records);
        output.AppendLine("}"); // close interface
        if (!string.IsNullOrEmpty(rds.Namespace))
        {
          output.AppendLine("}");
        }

        output.AppendLine();

        output.AppendLine($"namespace Germinate.Internal{(string.IsNullOrEmpty(rds.Namespace) ? "" : "." + rds.Namespace)} {{");
        output.AppendLine($"  public class {rds.DraftInstanceClassName} : {Names.FullyQualifiedDraftableBase}, {rds.FullyQualifiedInterfaceName} {{");

        EmitProperties(EmitPhase.PropImplementation, rds, output, records);

        // constructor
        output.AppendLine($"    private readonly {rds.FullyQualifiedClassName} {Names.OriginalProp};");
        output.AppendLine($"    public {rds.DraftInstanceClassName}({rds.FullyQualifiedClassName} value, {Names.FullyQualifiedDraftableBase}? parent, {Names.FullyQualifiedCheckDirty}? checkDirty = null) : base(parent, checkDirty)");
        output.AppendLine("    {");
        output.AppendLine($"      {Names.OriginalProp} = value;");
        EmitProperties(EmitPhase.Constructor, rds, output, records);
        output.AppendLine("    }"); // close constructor

        // finish
        output.AppendLine($"    public {rds.FullyQualifiedClassName} {Names.FinishMethod}()");
        output.AppendLine("    {");
        output.AppendLine($"      if (base.{Names.IsDirtyProp})");
        output.AppendLine("      {");
        output.AppendLine($"        return new {rds.FullyQualifiedClassName}() {{");
        EmitProperties(EmitPhase.Finish, rds, output, records);
        output.AppendLine("        };"); // close initializer
        output.AppendLine("      } else {");
        output.AppendLine($"        return {Names.OriginalProp};");
        output.AppendLine("      }"); // close else
        output.AppendLine("    }"); // close finish method

        output.AppendLine("  }"); // close class
        output.AppendLine("}"); // close Internal namespace

        // Producer
        output.AppendLine("namespace Germinate {");
        output.AppendLine($"public static partial class Producer {{");
        output.AppendLine($"  public static {rds.FullyQualifiedClassName} Produce(this {rds.FullyQualifiedClassName} value, System.Action<{rds.FullyQualifiedInterfaceName}> f)");
        output.AppendLine("  {");
        output.AppendLine($"    var check = new {Names.FullyQualifiedCheckDirty}() {{ Checks = new System.Collections.Generic.List<System.Action>() }};");
        output.AppendLine($"    var draft = new {rds.FullyQualifiedDraftInstanceClassName}(value, null, check);");
        output.AppendLine("    f(draft);");
        output.AppendLine("    foreach (var a in check.Checks) a();");
        output.AppendLine($"    return draft.{Names.FinishMethod}();");
        output.AppendLine("  }");
        output.AppendLine("}}"); // close Producer and namespace

        //log.WriteLine(output.ToString());
        //log.WriteLine("########################################");
        context.AddSource(rds.ClassName + ".Draftable.cs", output.ToString());
      }

      //log.Close();
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
        if (allRecords.TryGetValue(prop.FullTypeName, out var propRecord))
        {
          PropDraftable.Emit(phase, prop, propRecord, output);
        }
        else if (_immutableCollections.Any(t => prop.FullTypeName.StartsWith(t)))
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
      return $@"#nullable enable
namespace Germinate {{
[System.AttributeUsage(System.AttributeTargets.Class)]
public class DraftableAttribute : System.Attribute {{ }}

namespace Internal {{
  public struct {Names.CheckDirtyStructName}
  {{
    public System.Collections.Generic.List<System.Action> Checks;
  }}


  public abstract class {Names.DraftableBaseClassName}
  {{
    private {Names.DraftableBaseClassName}? _parent;
    private {Names.CheckDirtyStructName} _checkDirty;
    private bool _dirty = false;

    protected bool {Names.IsDirtyProp} => _dirty;

    protected void {Names.SetDirtyMethod}()
    {{
      {Names.DraftableBaseClassName}? b = this;
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

    protected {Names.DraftableBaseClassName}({Names.DraftableBaseClassName}? parent, {Names.CheckDirtyStructName}? checkDirty)
    {{
      _parent = parent;
      _checkDirty = parent != null ? parent._checkDirty : (checkDirty ?? new {Names.CheckDirtyStructName}() {{Checks = new System.Collections.Generic.List<System.Action>() }});
    }}
  }}
}}}}";
    }
  }
}