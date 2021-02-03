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
  public static class PropListOfDraftable
  {
    public const string PropPrefix = "__germinate_ldprop__";

    private static string ListType(RecordToDraft elementRecord)
    {
      return $"Germinate.Collections.ListOfDraft<{elementRecord.FullClassName}, {elementRecord.DraftName}, {elementRecord.InterfaceName}>";
    }

    private static string InterfaceType(RecordToDraft elementRecord)
    {
      return $"Germinate.Collections.IListOfDraft<{elementRecord.FullClassName}, {elementRecord.InterfaceName}>";
    }

    public static void InterfaceProps(RecordProperty prop, RecordToDraft elementRecord, StringBuilder output)
    {
      output.AppendLine($"  {InterfaceType(elementRecord)} {prop.PropertyName} {{get;}}");
    }

    public static void ImplementationProps(RecordProperty prop, RecordToDraft elementRecord, StringBuilder output)
    {
      output.AppendLine($"    private {ListType(elementRecord)} {PropPrefix}{prop.PropertyName};");
      output.AppendLine($"    public {InterfaceType(elementRecord)} {prop.PropertyName}");
      output.AppendLine("    {");
      output.AppendLine($"      get => {PropPrefix}{prop.PropertyName};");
      output.AppendLine("    }");
    }

    public static void ImplementationConstructor(RecordProperty prop, RecordToDraft elementRecord, StringBuilder output)
    {
      output.AppendLine($"      {PropPrefix}{prop.PropertyName} = new {ListType(elementRecord)}(");
      output.AppendLine($"        value.{prop.PropertyName},");
      output.AppendLine($"        base.{DraftableGenerator.SetDirtyMethod},");
      output.AppendLine($"        (x, s) => new {elementRecord.DraftName}(x, null, s),");
      output.AppendLine($"        d => d.{DraftableGenerator.ClearParentMethod}(),");
      output.AppendLine($"        d => d.{DraftableGenerator.FinishMethod}()");
      output.AppendLine($"      );");
    }

    public static void Finish(RecordProperty prop, StringBuilder output)
    {
      output.AppendLine($"          {prop.PropertyName} = this.{PropPrefix}{prop.PropertyName}.Finish(),");
    }
  }
}