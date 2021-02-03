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

namespace Germinate.Collections
{
  public interface IDictionaryOfDraft<Key, Value, ValueDraft>
    : System.Collections.IEnumerable,
      IEnumerable<KeyValuePair<Key, ValueDraft>>,
      IReadOnlyCollection<KeyValuePair<Key, ValueDraft>>,
      IReadOnlyDictionary<Key, ValueDraft>
  {
    void Add(Key key, Value v);
    void Clear();
    bool Remove(Key key);
  }

  public class DictionaryOfDraft<Key, Value, ValueDraft, IValueDraft>
    : DraftableCollectionBase,
      System.Collections.IEnumerable,
      IEnumerable<KeyValuePair<Key, IValueDraft>>,
      IReadOnlyCollection<KeyValuePair<Key, IValueDraft>>,
      IReadOnlyDictionary<Key, IValueDraft>,
      IDictionaryOfDraft<Key, Value, IValueDraft>
      where ValueDraft : IValueDraft
  {
    private readonly IReadOnlyDictionary<Key, Value> _original;
    private readonly Dictionary<Key, ValueDraft> _copy;
    private Func<Value, Action, ValueDraft> _draft;
    private Action<ValueDraft> _clearParent;
    private Func<ValueDraft, Value> _finalize;

    public DictionaryOfDraft(IReadOnlyDictionary<Key, Value> original, Action setParentDirty, Func<Value, Action, ValueDraft> draft, Action<ValueDraft> clearParent, Func<ValueDraft, Value> finalize)
      : base(setParentDirty)
    {
      _original = original;
      _draft = draft;
      _clearParent = clearParent;
      _finalize = finalize;
      _copy = new Dictionary<Key, ValueDraft>(original.Count);
      foreach (var x in original)
      {
        _copy.Add(x.Key, draft(x.Value, SetDirty));
      }
    }

    public IReadOnlyDictionary<Key, Value> Finalize()
    {
      if (base.IsDirty)
      {
        var ret = new Dictionary<Key, Value>(_copy.Count);
        foreach (var x in _copy)
        {
          ret.Add(x.Key, _finalize(x.Value));
        }
        return ret;
      }
      else
      {
        return _original;
      }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return ((System.Collections.IEnumerable)_copy).GetEnumerator();
    }

    public IEnumerator<KeyValuePair<Key, IValueDraft>> GetEnumerator() =>
      // C# type system can't convert KeyValuePair<Key, ValueDraft> to KeyValuePair<Key, IValueDraft>
      (IEnumerator<KeyValuePair<Key, IValueDraft>>)(object)_copy.GetEnumerator();

    public int Count => _copy.Count;
    public bool ContainsKey(Key k) => _copy.ContainsKey(k);
    public bool TryGetValue(Key k, out IValueDraft value)
    {
      if (_copy.TryGetValue(k, out var val))
      {
        value = val;
        return true;
      }
      else
      {
        value = default(IValueDraft);
        return false;
      }
    }

    public IValueDraft this[Key k]
    {
      get => _copy[k];
    }

    public IEnumerable<Key> Keys => _copy.Keys;
    public IEnumerable<IValueDraft> Values => (IEnumerable<IValueDraft>)(IEnumerable<ValueDraft>)_copy.Values;


    public void Add(Key key, Value v)
    {
      SetDirty();
      _copy.Add(key, _draft(v, SetDirty));
    }

    public void Clear()
    {
      SetDirty();
      _copy.Clear();
    }

    public bool Remove(Key key)
    {
      if (_copy.TryGetValue(key, out var val))
      {
        SetDirty();
        _clearParent(val);
        _copy.Remove(key);
        return true;
      }
      else
      {
        return false;
      }
    }
  }
}