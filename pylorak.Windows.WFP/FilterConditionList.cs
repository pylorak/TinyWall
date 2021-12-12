using System;
using System.Collections;
using System.Collections.Generic;

namespace pylorak.Windows.WFP
{
    public class FilterConditionList : ICollection<FilterCondition>, IList<FilterCondition>, IDisposable
    {
        private readonly List<FilterCondition> _list;
        private bool _disposed;

        public FilterConditionList()
        {
            _list = new List<FilterCondition>();
        }

        public FilterConditionList(int capacity)
        {
            _list = new List<FilterCondition>(capacity);
        }

        public FilterCondition this[int index] { get => _list[index]; set => _list[index] = value; }

        public int Count => _list.Count;

        public int Capacity { get => _list.Capacity; set => _list.Capacity = value; }

        public bool IsReadOnly => false;

        public object SyncRoot => _list;

        public bool IsSynchronized => false;

        public bool IsDisposed => _disposed;

        public void Add(FilterCondition item)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FilterConditionList));

            item.AddRef();
            _list.Add(item);
        }

        public void Clear()
        {
            foreach (var item in _list)
                item.RemoveRef();
            _list.Clear();
        }

        public bool Contains(FilterCondition item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(FilterCondition[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<FilterCondition> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(FilterCondition item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, FilterCondition item)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FilterConditionList));

            item.AddRef();
            _list.Insert(index, item);
        }

        public bool Remove(FilterCondition item)
        {
            var success = _list.Remove(item);
            if (success)
                item.RemoveRef();
            return success;
        }

        public void RemoveAt(int index)
        {
            _list[index].RemoveRef();
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (_list as IEnumerable).GetEnumerator();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Clear();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
