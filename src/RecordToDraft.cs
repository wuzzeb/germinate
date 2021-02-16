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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Germinate.Generator
{
  public class RecordProperty
  {
    public string PropertyName { get; set; }
    public string FullTypeName { get; set; }
    public NullableAnnotation Nullable { get; set; }
    public bool IsValueType { get; set; }
    public DraftableRecord TypeIsDraftable { get; set; }
  }


  public class DraftableRecord
  {
    public bool Emit { get; set; }
    public string Namespace { get; set; }
    public string RecordName { get; set; }
    public string FullyQualifiedRecordName { get; set; }
    public string InterfaceName { get; set; }
    public string FullyQualifiedInterfaceName { get; set; }
    public string DraftInstanceClassName { get; set; }
    public string FullyQualifiedDraftInstanceClassName { get; set; }
    public DraftableRecord BaseRecord { get; set; }
    public IReadOnlyList<RecordProperty> Properties { get; set; }
  }

  public static class BuildRecords
  {
    public static IReadOnlyDictionary<string, DraftableRecord> RecordsToDraft(Compilation comp, IEnumerable<RecordDeclarationSyntax> rdss)
    {
      var records = new Dictionary<string, DraftableRecord>();

      foreach (var rds in rdss)
      {
        var model = comp.GetSemanticModel(rds.SyntaxTree);
        var recordSymbol = model.GetDeclaredSymbol(rds) as INamedTypeSymbol;

        var record = AnalyzeRecord(recordSymbol, records);
        record.Emit = true;
      }

      return records;
    }

    private static DraftableRecord AnalyzeRecord(INamedTypeSymbol recordSymbol, IDictionary<string, DraftableRecord> allRecords)
    {
      var fullQualName = recordSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      DraftableRecord record;
      if (allRecords.TryGetValue(fullQualName, out record))
      {
        return record;
      }

      var nsp = recordSymbol.ContainingNamespace?.ToDisplayString();
      var recordName = recordSymbol.Name;

      DraftableRecord baseRecord = null;
      if (recordSymbol.BaseType != null && recordSymbol.BaseType.SpecialType == SpecialType.None)
      {
        baseRecord = AnalyzeRecord(recordSymbol.BaseType, allRecords);
      }

      record = new DraftableRecord()
      {
        Emit = false,
        RecordName = recordName,
        Namespace = nsp,
        FullyQualifiedRecordName = fullQualName,
        InterfaceName = "I" + recordName + "Draft",
        FullyQualifiedInterfaceName = "global::" + (string.IsNullOrEmpty(nsp) ? "" : nsp + ".") + "I" + recordName + "Draft",
        DraftInstanceClassName = recordName + "Draft",
        FullyQualifiedDraftInstanceClassName = "global::Germinate.Internal" + (string.IsNullOrEmpty(nsp) ? "" : "." + nsp) + "." + recordName + "Draft",
        BaseRecord = baseRecord,
        Properties = recordSymbol.GetMembers()
          .OfType<IPropertySymbol>()
          .Where(p => p.DeclaredAccessibility == Accessibility.Public &&
                      p.GetMethod != null &&
                      p.GetMethod.DeclaredAccessibility == Accessibility.Public &&
                      p.SetMethod != null &&
                      p.SetMethod.DeclaredAccessibility == Accessibility.Public
                )
          .Select(p =>
          {
            DraftableRecord typeIsDraftable = null;
            if (p.Type.GetAttributes().Any(IsDraftableAttribute))
            {
              typeIsDraftable = AnalyzeRecord(p.Type as INamedTypeSymbol, allRecords);
            }
            return new RecordProperty()
            {
              PropertyName = p.Name,
              Nullable = p.NullableAnnotation,
              FullTypeName = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
              IsValueType = p.Type.IsValueType,
              TypeIsDraftable = typeIsDraftable,
            };
          })
          .ToList(),
      };

      allRecords.Add(fullQualName, record);
      return record;
    }

    private static bool IsDraftableAttribute(AttributeData a)
    {
      var fullQualClass = a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      // is Draftable if the attribute hasn't been analyzed yet because it is being emitted by the generator in this assembly
      // is global::Germinate.DraftableAttribute if the attribute is defined in a dependent assembly
      return fullQualClass == "Draftable" || fullQualClass == "global::Germinate.DraftableAttribute";
    }
  }
}
