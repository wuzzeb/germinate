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

  public class DictionaryOfPrim<Key, Value>
    : DraftableCollectionBase,
      System.Collections.IEnumerable,
      IEnumerable<KeyValuePair<Key, Value>>,
      ICollection<KeyValuePair<Key, Value>>,
      IReadOnlyCollection<KeyValuePair<Key, Value>>,
      IReadOnlyDictionary<Key, Value>,
      IDictionary<Key, Value>
  {
    private readonly IReadOnlyDictionary<Key, Value> _original;
    private Dictionary<Key, Value> _copy;

    public DictionaryOfPrim(IReadOnlyDictionary<Key, Value> original, Action setParentDirty) : base(setParentDirty)
    {
      _original = original;
      _copy = new Dictionary<Key, Value>(original);
    }

    public IReadOnlyDictionary<Key, Value> Finalize()
    {
      if (IsDirty)
      {
        return _copy;
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

    public IEnumerator<KeyValuePair<Key, Value>> GetEnumerator() => _copy.GetEnumerator();

    public int Count => _copy.Count;

    public bool ContainsKey(Key k) => _copy.ContainsKey(k);
    public bool TryGetValue(Key k, out Value value) => _copy.TryGetValue(k, out value);

    public Value this[Key k]
    {
      get => _copy[k];
      set
      {
        SetDirty();
        _copy[k] = value;
      }
    }

    IEnumerable<Key> IReadOnlyDictionary<Key, Value>.Keys => _copy.Keys;
    IEnumerable<Value> IReadOnlyDictionary<Key, Value>.Values => _copy.Values;

    public ICollection<Key> Keys => _copy.Keys;
    public ICollection<Value> Values => _copy.Values;

    public bool IsReadOnly => ((ICollection<KeyValuePair<Key, Value>>)_copy).IsReadOnly;



    public void Add(Key key, Value value)
    {
      SetDirty();
      _copy.Add(key, value);
    }

    public bool Remove(Key key)
    {
      SetDirty();
      return _copy.Remove(key);
    }

    public void Add(KeyValuePair<Key, Value> item)
    {
      SetDirty();
      ((ICollection<KeyValuePair<Key, Value>>)_copy).Add(item);
    }

    public void Clear()
    {
      SetDirty();
      _copy.Clear();
    }

    public bool Contains(KeyValuePair<Key, Value> item)
    {
      return ((ICollection<KeyValuePair<Key, Value>>)_copy).Contains(item);
    }

    public void CopyTo(KeyValuePair<Key, Value>[] array, int arrayIndex)
    {
      ((ICollection<KeyValuePair<Key, Value>>)_copy).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<Key, Value> item)
    {
      SetDirty();
      return ((ICollection<KeyValuePair<Key, Value>>)_copy).Remove(item);
    }
  }
}