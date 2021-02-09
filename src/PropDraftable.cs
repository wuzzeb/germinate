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

    public static void Emit(EmitPhase phase, RecordProperty prop, RecordToDraft propRecord, StringBuilder output)
    {
      if (prop.Nullable == Microsoft.CodeAnalysis.NullableAnnotation.NotAnnotated)
      {
        EmitNonNullable(phase, prop, propRecord, output);
      }
      else
      {
        EmitNullable(phase, prop, propRecord, output);
      }
    }

    private static void EmitNonNullable(EmitPhase phase, RecordProperty prop, RecordToDraft propRecord, StringBuilder output)
    {
      var draftPropName = Names.PropPrefix + "draft_" + prop.PropertyName;
      switch (phase)
      {
        case EmitPhase.Interface:
          output.AppendLine($"  {propRecord.InterfaceName} {prop.PropertyName} {{get;}}");
          output.AppendLine($"  {propRecord.InterfaceName} Set{prop.PropertyName}({prop.FullTypeName} value);");
          break;

        case EmitPhase.PropImplementation:
          output.AppendLine($"    private {propRecord.DraftName}? {draftPropName} = null;");
          output.AppendLine($"    public {propRecord.InterfaceName} {prop.PropertyName}");
          output.AppendLine("    {");
          output.AppendLine($"      get {{");
          output.AppendLine($"        if ({draftPropName} == null) {{");
          output.AppendLine($"          {draftPropName} = new {propRecord.DraftName}({Names.OriginalProp}.{prop.PropertyName}, this);");
          output.AppendLine("        }"); // close if checking not created
          output.AppendLine($"        return {draftPropName};");
          output.AppendLine("      }"); // close get
          output.AppendLine("    }");
          output.AppendLine($"    public {propRecord.InterfaceName} Set{prop.PropertyName}({propRecord.FullClassName} value)");
          output.AppendLine("    {");
          output.AppendLine($"      base.{Names.SetDirtyMethod}();");
          output.AppendLine($"      {draftPropName} = new {propRecord.DraftName}(value, this);");
          output.AppendLine($"      return {draftPropName};");
          output.AppendLine("    }");

          break;

        case EmitPhase.Constructor:
          // nothing needed here, not initialized until the first get
          break;

        case EmitPhase.Finish:
          output.AppendLine($"          {prop.PropertyName} = {draftPropName} != null ? {draftPropName}.{Names.FinishMethod}() : {Names.OriginalProp}.{prop.PropertyName},");
          break;
      }
    }

    private static void EmitNullable(EmitPhase phase, RecordProperty prop, RecordToDraft propRecord, StringBuilder output)
    {
      var createdPropName = Names.PropPrefix + "creat_" + prop.PropertyName;
      var draftPropName = Names.PropPrefix + "draft_" + prop.PropertyName;
      switch (phase)
      {
        case EmitPhase.Interface:
          output.AppendLine($"  {propRecord.InterfaceName}? {prop.PropertyName} {{get;}}");
          output.AppendLine($"  {propRecord.InterfaceName}? Set{prop.PropertyName}({prop.FullTypeName}? value);");
          break;

        case EmitPhase.PropImplementation:
          output.AppendLine($"    private bool {createdPropName} = false;");
          output.AppendLine($"    private {propRecord.DraftName}? {draftPropName} = null;");
          output.AppendLine($"    public {propRecord.InterfaceName}? {prop.PropertyName}");
          output.AppendLine("    {");
          output.AppendLine($"      get {{");
          output.AppendLine($"        if (!{createdPropName}) {{");
          output.AppendLine($"          {createdPropName} = true;");
          output.AppendLine($"          if ({Names.OriginalProp}.{prop.PropertyName} != null) {{");
          output.AppendLine($"            {draftPropName} = new {propRecord.DraftName}({Names.OriginalProp}.{prop.PropertyName}, this);");
          output.AppendLine("          }"); // close if checking original prop not null
          output.AppendLine("        }"); // close if checking not created
          output.AppendLine($"        return {draftPropName};");
          output.AppendLine("      }"); // close get
          output.AppendLine("    }");
          output.AppendLine($"    public {propRecord.InterfaceName}? Set{prop.PropertyName}({propRecord.FullClassName}? value)");
          output.AppendLine("    {");
          output.AppendLine($"      base.{Names.SetDirtyMethod}();");
          output.AppendLine($"      {createdPropName} = true;");
          output.AppendLine($"      {draftPropName} = value == null ? null : new {propRecord.DraftName}(value, this);");
          output.AppendLine($"      return {draftPropName};");
          output.AppendLine("    }");

          break;

        case EmitPhase.Constructor:
          // nothing needed here, not initialized until the first get
          break;

        case EmitPhase.Finish:
          output.AppendLine($"          {prop.PropertyName} = {createdPropName} ? {draftPropName}?.{Names.FinishMethod}() : {Names.OriginalProp}.{prop.PropertyName},");
          break;
      }
    }
  }
}