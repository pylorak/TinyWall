using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace pylorak.Utilities
{
    [DebuggerTypeProxy(typeof(CircularBuffer<>.CircularBufferDebugView))]
    public sealed class CircularBuffer<T> : IEnumerable<T>, System.Collections.ICollection
    {
        private readonly object _syncRoot = new();
        private readonly T[] _array;
        private int _size = 0;  // current # of elements
        private int _head = 0;  // first element
        private int _tail = 0;  // last element

        public CircularBuffer(int capacity)
        {
            _array = new T[capacity];
        }

        public int Count
        {
            get { return _size; }
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        public void CopyTo(Array array, int dstIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), "Argument cannot be null.");
            if (array.Rank != 1)
                throw new ArgumentException("Destination array must have a rank of 1.");
            if (dstIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(dstIndex), "Argument cannot be negative.");

            int arrayLen = array.Length;
            if (arrayLen - dstIndex < _size)
                throw new ArgumentException("Not enough space in destination array.");

            int numToCopy = _size;
            if (numToCopy == 0)
                return;
            int firstPart = (_array.Length - _head < numToCopy) ? _array.Length - _head : numToCopy;
            Array.Copy(_array, _head, array, dstIndex, firstPart);
            numToCopy -= firstPart;
            if (numToCopy > 0)
                Array.Copy(_array, 0, array, dstIndex + _array.Length - _head, numToCopy);
        }

        public void Enqueue(T obj)
        {
            _array[_tail] = obj;
            _tail = (_tail + 1) % _array.Length;

            if (_size == _array.Length)
                _head = (_head + 1) % _array.Length;
            else
                _size++;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _size; ++i)
                yield return _array[(_head + i) % _array.Length];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T Dequeue()
        {
            if (_size == 0)
                throw new InvalidOperationException("The collection is already empty.");

            T ret = _array[_head];
            _array[_head] = default;
            _head = (_head + 1) % _array.Length;
            _size--;
            return ret;
        }

        public T Peek()
        {
            if (_size == 0)
                throw new InvalidOperationException("The collection is empty.");

            return _array[_head];
        }

        public bool Contains(T obj)
        {
            int index = _head;
            int count = _size;

            if (typeof(T).IsClass && (obj == null))
            {
                while (count-- > 0)
                {
                    if (_array[index] == null)
                        return true;

                    index = (index + 1) % _array.Length;
                }
            }
            else
            {
                while (count-- > 0)
                {
                    var item = _array[index];
                    if ((item is not null) && item.Equals(obj))
                        return true;

                    index = (index + 1) % _array.Length;
                }
            }

            return false;
        }

        public T this[int i]
        {
            get
            {
                if (i >= _size)
                    throw new IndexOutOfRangeException();

                return _array[(_head + i) % _array.Length];
            }

            set
            {
                if (i >= _size)
                    throw new IndexOutOfRangeException();

                _array[(_head + i) % _array.Length] = value;
            }
        }

        public T[] ToArray()
        {
            T[] arr = new T[_size];
            if (_size == 0)
                return arr;

            if (_head < _tail)
            {
                Array.Copy(_array, _head, arr, 0, _size);
            }
            else
            {
                Array.Copy(_array, _head, arr, 0, _array.Length - _head);
                Array.Copy(_array, 0, arr, _array.Length - _head, _tail);
            }

            return arr;
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get
            {
                return _syncRoot;
            }
        }

        internal class CircularBufferDebugView
        {
            private readonly CircularBuffer<T> buffer;

            public CircularBufferDebugView(CircularBuffer<T> buffer)
            {
                this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            }

            public int Count
            {
                get { return buffer.Count; }
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items
            {
                get
                {
                    return buffer.ToArray();
                }
            }
        }
    }
}
