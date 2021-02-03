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
  public class ListOfPrim<T> :
    DraftableCollectionBase,
    System.Collections.IEnumerable,
    IEnumerable<T>,
    IReadOnlyCollection<T>,
    IReadOnlyList<T>,
    ICollection<T>,
    IList<T>
  {
    private readonly IReadOnlyList<T> _original;
    private readonly List<T> _copy;
    public ListOfPrim(IReadOnlyList<T> o, Action setParentDirty) : base(setParentDirty)
    {
      _original = o;
      _copy = new List<T>(_original);
    }

    public IReadOnlyList<T> Finish()
    {
      if (base.IsDirty)
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

    public IEnumerator<T> GetEnumerator() => _copy.GetEnumerator();

    public int Count => _copy.Count;
    public bool IsReadOnly => false;

    public void Add(T t)
    {
      SetDirty();
      _copy.Add(t);
    }
    public void Clear()
    {
      SetDirty();
      _copy.Clear();
    }
    public T this[int index]
    {
      get => _copy[index];
      set
      {
        SetDirty();
        _copy[index] = value;
      }
    }
    public bool Contains(T item) => _copy.Contains(item);
    public void CopyTo(T[] items, int index) => _copy.CopyTo(items, index);
    public bool Remove(T item)
    {
      SetDirty();
      return _copy.Remove(item);
    }

    public int IndexOf(T item) => _copy.IndexOf(item);
    public void RemoveAt(int index)
    {
      SetDirty();
      _copy.RemoveAt(index);
    }
    public void Insert(int index, T item)
    {
      SetDirty();
      _copy.Insert(index, item);
    }
  }
}