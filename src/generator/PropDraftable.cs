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
    public const string PropPrefix = "__germinate_dprop__";

    public static void InterfaceProps(RecordToDraft record, RecordProperty prop, RecordToDraft propRecord, StringBuilder output)
    {
      output.AppendLine($"  {propRecord.InterfaceName} {prop.PropertyName} {{get;}}");
      output.AppendLine($"  {record.InterfaceName} SetAndDraft{prop.PropertyName}({prop.FullPropertyTypeName} value);");
    }

    public static void ImplementationProps(RecordToDraft record, RecordProperty prop, RecordToDraft propRecord, StringBuilder output)
    {
      output.AppendLine($"    private {propRecord.DraftName} {PropPrefix}{prop.PropertyName};");
      output.AppendLine($"    public {propRecord.InterfaceName} {prop.PropertyName}");
      output.AppendLine("    {");
      output.AppendLine($"      get => {PropPrefix}{prop.PropertyName};");
      output.AppendLine("    }");
      output.AppendLine($"    public {record.InterfaceName} SetAndDraft{prop.PropertyName}({propRecord.FullClassName} value)");
      output.AppendLine("    {");
      output.AppendLine("      base.SetDirty();");
      output.AppendLine($"      {PropPrefix}{prop.PropertyName}.SetParent((DraftableBase)null);");
      output.AppendLine($"      {PropPrefix}{prop.PropertyName} = new {propRecord.DraftName}(value, this);");
      output.AppendLine("      return this;");
      output.AppendLine("    }");
    }

    public static void ImplementationConstructor(RecordProperty prop, RecordToDraft propRecord, StringBuilder output)
    {
      output.AppendLine($"      {PropPrefix}{prop.PropertyName} = new {propRecord.DraftName}(value.{prop.PropertyName}, this);");
    }

    public static void Finish(RecordProperty prop, StringBuilder output)
    {
      output.AppendLine($"          {prop.PropertyName} = this.{PropPrefix}{prop.PropertyName}.{DraftableGenerator.FinishMethod}(),");
    }
  }
}