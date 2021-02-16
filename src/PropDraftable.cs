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

namespace Germinate.Generator
{
  public static class PropDraftable
  {

    public static void Emit(EmitPhase phase, RecordProperty prop, StringBuilder output)
    {
      if (prop.Nullable == Microsoft.CodeAnalysis.NullableAnnotation.NotAnnotated)
      {
        EmitNonNullable(phase, prop, output);
      }
      else
      {
        EmitNullable(phase, prop, output);
      }
    }

    private static void EmitNonNullable(EmitPhase phase, RecordProperty prop, StringBuilder output)
    {
      var propRecord = prop.TypeIsDraftable;
      var draftPropName = Names.PropPrefix + "draft_" + prop.PropertyName;
      var origPropName = Names.PropPrefix + "orig_" + prop.PropertyName;
      switch (phase)
      {
        case EmitPhase.Interface:
          output.AppendLine($"  {propRecord.FullyQualifiedInterfaceName} {prop.PropertyName} {{get;}}");
          output.AppendLine($"  {propRecord.FullyQualifiedInterfaceName} Set{prop.PropertyName}({prop.FullTypeName} value);");
          break;

        case EmitPhase.PropImplementation:
          output.AppendLine($"    protected {propRecord.FullyQualifiedRecordName} {origPropName};");
          output.AppendLine($"    protected {propRecord.FullyQualifiedDraftInstanceClassName}? {draftPropName};");
          output.AppendLine($"    public {propRecord.FullyQualifiedInterfaceName} {prop.PropertyName}");
          output.AppendLine("    {");
          output.AppendLine($"      get {{");
          output.AppendLine($"        if ({draftPropName} == null) {{");
          output.AppendLine($"          {draftPropName} = new {propRecord.FullyQualifiedDraftInstanceClassName}({origPropName}, this);");
          output.AppendLine("        }"); // close if checking not created
          output.AppendLine($"        return {draftPropName};");
          output.AppendLine("      }"); // close get
          output.AppendLine("    }");
          output.AppendLine($"    public {propRecord.FullyQualifiedInterfaceName} Set{prop.PropertyName}({prop.FullTypeName} value)");
          output.AppendLine("    {");
          output.AppendLine($"      {Names.SetDirtyMethod}();");
          output.AppendLine($"      {draftPropName} = new {propRecord.FullyQualifiedDraftInstanceClassName}(value, this);");
          output.AppendLine($"      return {draftPropName};");
          output.AppendLine("    }");

          break;

        case EmitPhase.Init:
          output.AppendLine($"      {origPropName} = value.{prop.PropertyName};");
          output.AppendLine($"      {draftPropName} = null;");
          break;

        case EmitPhase.Finish:
          output.AppendLine($"          {prop.PropertyName} = {draftPropName} != null ? {draftPropName}.{Names.FinishMethod}() : {origPropName},");
          break;
      }
    }

    private static void EmitNullable(EmitPhase phase, RecordProperty prop, StringBuilder output)
    {
      var propRecord = prop.TypeIsDraftable;
      var draftPropName = Names.PropPrefix + "draft_" + prop.PropertyName;
      var origPropName = Names.PropPrefix + "orig_" + prop.PropertyName;
      switch (phase)
      {
        case EmitPhase.Interface:
          output.AppendLine($"  {propRecord.FullyQualifiedInterfaceName}? {prop.PropertyName} {{get;}}");
          output.AppendLine($"  {propRecord.FullyQualifiedInterfaceName}? Set{prop.PropertyName}({prop.FullTypeName}? value);");
          break;

        case EmitPhase.PropImplementation:
          output.AppendLine($"    protected {propRecord.FullyQualifiedRecordName}? {origPropName};");
          output.AppendLine($"    protected {propRecord.FullyQualifiedDraftInstanceClassName}? {draftPropName};");
          output.AppendLine($"    public {propRecord.FullyQualifiedInterfaceName}? {prop.PropertyName}");
          output.AppendLine("    {");
          output.AppendLine($"      get {{");
          output.AppendLine($"        if ({origPropName} != null && {draftPropName} == null) {{");
          output.AppendLine($"          {draftPropName} = new {propRecord.FullyQualifiedDraftInstanceClassName}({origPropName}, this);");
          output.AppendLine("        }"); // close if checking not created
          output.AppendLine($"        return {draftPropName};");
          output.AppendLine("      }"); // close get
          output.AppendLine("    }");
          output.AppendLine($"    public {propRecord.FullyQualifiedInterfaceName}? Set{prop.PropertyName}({prop.FullTypeName}? value)");
          output.AppendLine("    {");
          output.AppendLine($"      {Names.SetDirtyMethod}();");
          output.AppendLine($"      {origPropName} = value;");
          output.AppendLine($"      {draftPropName} = null;");
          output.AppendLine($"      return {draftPropName};");
          output.AppendLine("    }");

          break;

        case EmitPhase.Init:
          output.AppendLine($"      {origPropName} = value.{prop.PropertyName};");
          output.AppendLine($"      {draftPropName} = null;");
          break;

        case EmitPhase.Finish:
          output.AppendLine($"          {prop.PropertyName} = {draftPropName} != null ? {draftPropName}.{Names.FinishMethod}() : {origPropName},");
          break;
      }
    }
  }
}