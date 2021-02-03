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
  public interface IListOfDraft<T, TDraft>
    : System.Collections.IEnumerable,
      IEnumerable<TDraft>,
      IReadOnlyCollection<TDraft>,
      IReadOnlyList<TDraft>
  {
    void Add(T value);
    void SetAndDraft(int index, T value);
    void Insert(int index, T item);
    void Clear();
    void RemoveAt(int index);
  }

  public class ListOfDraft<T, TDraft, ITDraft> :
    DraftableCollectionBase,
    System.Collections.IEnumerable,
    IEnumerable<ITDraft>,
    IReadOnlyCollection<ITDraft>,
    IReadOnlyList<ITDraft>,
    IListOfDraft<T, ITDraft>
    where TDraft : ITDraft
  {
    private readonly IReadOnlyList<T> _original;
    private readonly List<TDraft> _copy;
    private Func<T, Action, TDraft> _draft;
    private Action<TDraft> _clearParent;
    private Func<TDraft, T> _finalize;
    public ListOfDraft(IReadOnlyList<T> l, Action setParentDirty, Func<T, Action, TDraft> draft, Action<TDraft> clearParent, Func<TDraft, T> finalize) : base(setParentDirty)
    {
      _original = l;
      _draft = draft;
      _finalize = finalize;
      _clearParent = clearParent;
      _copy = new List<TDraft>(l.Count);
      foreach (var t in l)
      {
        _copy.Add(draft(t, SetDirty));
      }
    }

    public IReadOnlyList<T> Finalize()
    {
      if (base.IsDirty)
      {
        var ret = new List<T>(_copy.Count);
        foreach (var t in _copy)
        {
          ret.Add(_finalize(t));
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

    public IEnumerator<ITDraft> GetEnumerator() => ((IEnumerable<ITDraft>)_copy).GetEnumerator();

    public int Count => _copy.Count;
    public void Add(T t)
    {
      SetDirty();
      _copy.Add(_draft(t, SetDirty));
    }
    public void Clear()
    {
      SetDirty();
      foreach (var t in _copy)
      {
        _clearParent(t);
      }
      _copy.Clear();
    }

    public ITDraft this[int index]
    {
      get => _copy[index];
    }
    public void SetAndDraft(int index, T value)
    {
      SetDirty();
      _copy[index] = _draft(value, SetDirty);
    }
    public void Insert(int index, T item)
    {
      SetDirty();
      _copy.Insert(index, _draft(item, SetDirty));
    }
    public void RemoveAt(int index)
    {
      SetDirty();
      if (index >= 0 && index < _copy.Count)
      {
        _clearParent(_copy[index]);
        _copy.RemoveAt(index);
      }
      else
      {
        throw new ArgumentOutOfRangeException();
      }
    }
  }

}