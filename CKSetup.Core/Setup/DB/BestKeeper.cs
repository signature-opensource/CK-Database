using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    internal class BestKeeper<T> : IReadOnlyList<T>
    {
        readonly T[] _items;
        IComparer<T> _comparer;
        int _count;

        class ComparerAdapter : IComparer<T>
        {
            readonly Func<T, T, int> _comparator;

            public ComparerAdapter( Func<T, T, int> comparator )
            {
                _comparator = comparator;
            }

            public int Compare( T x, T y ) => _comparator( x, y );
        }

        public BestKeeper( int maxCount, Func<T, T, int> comparator = null )
            : this( maxCount, comparator == null ? (IComparer<T>)Comparer<T>.Default : new ComparerAdapter( comparator ) )
        {
        }

        public BestKeeper( int maxCount, IComparer<T> comparer )
        {
            if( maxCount < 0 ) throw new ArgumentException( "Must be positive.", nameof( maxCount ) );
            if( comparer == null ) throw new ArgumentNullException( nameof( comparer) );
            _comparer = comparer;
            _items = new T[maxCount];
        }

        public bool Add( T candidate )
        {
            int idx = Array.BinarySearch( _items, 0, _count, candidate, _comparer );
            if( idx < 0 ) idx = ~idx;

            if( idx >= _count )
            {
                if( _count == _items.Length ) return false;
                _items[idx] = candidate;
                _count++;
                return true;
            }
            int toCopy = _count - idx;
            if( _count == _items.Length ) --toCopy;
            Array.Copy( _items, idx, _items, idx + 1, toCopy );
            _items[idx] = candidate;
            if( _count < _items.Length ) _count++;
            return true;
        }

        public T this[int index]
        {
            get
            {
                if( index >= _count ) throw new IndexOutOfRangeException();
                return _items[index];
            }
        }

        public int Count => _count;

        public IEnumerator<T> GetEnumerator() => _items.Take( _count ).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    }


}
