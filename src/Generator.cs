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
      var assemblyName = context.Compilation.AssemblyName;

      // Check if Draftable attribute defined in a dependency of this assembly
      if (context.Compilation.GetTypeByMetadataName("Germinate.DraftableAttribute") == null)
      {
        context.AddSource("DraftableBase.cs", DraftableBase);
      }

      foreach (var rds in records.Values.Where(r => r.Emit))
      {
        var output = new StringBuilder();
        output.AppendLine("#nullable enable");

        // Interface
        if (!string.IsNullOrEmpty(rds.Namespace))
        {
          output.AppendLine($"namespace {rds.Namespace} {{");
        }
        output.AppendLine($"public interface {rds.InterfaceName} {(rds.BaseRecord != null ? " : " + rds.BaseRecord.FullyQualifiedInterfaceName : "")} {{");
        EmitProperties(EmitPhase.Interface, rds, output);
        output.AppendLine("}"); // close interface
        if (!string.IsNullOrEmpty(rds.Namespace))
        {
          output.AppendLine("}");
        }

        output.AppendLine();

        output.AppendLine($"namespace Germinate.Internal{(string.IsNullOrEmpty(rds.Namespace) ? "" : "." + rds.Namespace)} {{");
        output.AppendLine($"  public class {rds.DraftInstanceClassName} : {rds.BaseRecord?.FullyQualifiedDraftInstanceClassName ?? Names.FullyQualifiedDraftableBase}, {rds.FullyQualifiedInterfaceName} {{");

        EmitProperties(EmitPhase.PropImplementation, rds, output);

        // constructor
        output.AppendLine($"    private readonly {rds.FullyQualifiedRecordName} {Names.OriginalProp};");
        output.AppendLine($"    public {rds.DraftInstanceClassName}({rds.FullyQualifiedRecordName} value, {Names.FullyQualifiedDraftableBase}? parent, {Names.FullyQualifiedCheckDirty}? checkDirty = null) : base(value, parent, checkDirty)");
        output.AppendLine("    {");
        output.AppendLine($"      {Names.OriginalProp} = value;");
        EmitProperties(EmitPhase.Constructor, rds, output);
        output.AppendLine("    }"); // close constructor

        // finish
        output.AppendLine($"    public override {rds.FullyQualifiedRecordName} {Names.FinishMethod}()");
        output.AppendLine("    {");
        output.AppendLine($"      if ({Names.IsDirtyProp})");
        output.AppendLine("      {");
        output.AppendLine($"        return new {rds.FullyQualifiedRecordName}() {{");
        {
          DraftableRecord r = rds;
          while (r != null)
          {
            EmitProperties(EmitPhase.Finish, r, output);
            r = r.BaseRecord;
          }
        }
        output.AppendLine("        };"); // close initializer
        output.AppendLine("      } else {");
        output.AppendLine($"        return {Names.OriginalProp};");
        output.AppendLine("      }"); // close else
        output.AppendLine("    }"); // close finish method

        output.AppendLine("  }"); // close class
        output.AppendLine("}"); // close Internal namespace

        // Producer
        output.AppendLine("namespace Germinate {");
        output.AppendLine($"public static partial class Producer_{assemblyName?.Replace(".", "_").Replace("-", "_")} {{");
        output.AppendLine($"  public static {rds.FullyQualifiedRecordName} Produce(this {rds.FullyQualifiedRecordName} value, System.Action<{rds.FullyQualifiedInterfaceName}> f)");
        output.AppendLine("  {");
        output.AppendLine($"    var check = new {Names.FullyQualifiedCheckDirty}() {{ Checks = new System.Collections.Generic.List<System.Action>() }};");
        output.AppendLine($"    var draft = new {rds.FullyQualifiedDraftInstanceClassName}(value, null, check);");
        output.AppendLine("    f(draft);");
        output.AppendLine("    foreach (var a in check.Checks) a();");
        output.AppendLine($"    return draft.{Names.FinishMethod}();");
        output.AppendLine("  }");
        output.AppendLine("}}"); // close Producer and namespace

        context.AddSource(rds.RecordName + ".Draftable.cs", output.ToString());
      }
    }

    private static IReadOnlyList<string> _immutableCollections =
      new[] {
        "global::System.Collections.Immutable.ImmutableArray",
        "global::System.Collections.Immutable.ImmutableDictionary",
        "global::System.Collections.Immutable.ImmutableHashSet",
        "global::System.Collections.Immutable.ImmutableList",
        "global::System.Collections.Immutable.ImmutableSortedDictionary",
        "global::System.Collections.Immutable.ImmutableSortedSet",
      };

    private void EmitProperties(EmitPhase phase, DraftableRecord rds, StringBuilder output)
    {
      foreach (var prop in rds.Properties)
      {
        if (prop.TypeIsDraftable != null)
        {
          PropDraftable.Emit(phase, prop, output);
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

    private static readonly string DraftableBase =
$@"#nullable enable
namespace Germinate {{
[global::System.AttributeUsage(global::System.AttributeTargets.Class)]
public class DraftableAttribute : global::System.Attribute {{ }}

namespace Internal {{
  public struct {Names.CheckDirtyStructName}
  {{
    public global::System.Collections.Generic.List<global::System.Action> Checks;
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

    protected void {Names.AddCheckDirtyMethod}(global::System.Action a)
    {{
      _checkDirty.Checks.Add(a);
    }}

    public abstract object {Names.FinishMethod}();

    protected {Names.DraftableBaseClassName}(object value, {Names.DraftableBaseClassName}? parent, {Names.CheckDirtyStructName}? checkDirty)
    {{
      _parent = parent;
      _checkDirty = parent != null ? parent._checkDirty : (checkDirty ?? new {Names.CheckDirtyStructName}() {{Checks = new global::System.Collections.Generic.List<global::System.Action>() }});
    }}
  }}
}}}}";
  }
}