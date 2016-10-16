using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Small helper that helps handling data read and tests against them.
    /// </summary>
    public class SimpleDataTable
    {
        readonly Header[] _headers;
        List<object[]> _rows;

        public struct Header
        {
            public readonly string Name;
            public readonly Type ColumnType;

            public Header( string name, Type columnType )
            {
                Name = name;
                ColumnType = columnType;
            }
        }

        public IReadOnlyList<Header> Headers => _headers;

        public IReadOnlyList<object[]> Rows => _rows;

        public SimpleDataTable( IDataReader r, bool readContent = true )
        {
            _headers = new Header[r.FieldCount];
            for( int i = 0; i < _headers.Length; ++i ) _headers[i] = new Header( r.GetName( i ), r.GetFieldType( i ) );
            _rows = new List<object[]>();
            if( readContent ) AddNextRows( r );
        }

        public void AddRow( IDataReader r )
        {
            object[] row = new object[_headers.Length];
            r.GetValues( row );
            _rows.Add( row );
        }

        public int AddNextRows( IDataReader r )
        {
            int count = 0;
            while( r.Read() )
            {
                AddRow( r );
                count++;
            }
            return count;
        }

        public string PrettyPrint( int maxRowCount = 100 )
        {
            int rowCount = maxRowCount < 0 ? _rows.Count : Math.Min( maxRowCount, _rows.Count );
            var sizes = new int[_headers.Length];
            var grid = new string[sizes.Length, rowCount];
            int allW = 0;
            for( int i = 0; i < sizes.Length; ++i )
            {
                int w = _headers[i].Name.Length;
                for( int j = 0; j < rowCount; ++j )
                {
                    var s = _rows[j][i]?.ToString() ?? string.Empty;
                    if( w < s.Length ) w = s.Length;
                    grid[i, j] = s;
                }
                sizes[i] = w;
                allW += w + 3;
            }
            allW -= 3;
            StringBuilder b = new StringBuilder();
            for( int i = 0; i < _headers.Length; ++i )
            {
                string name = _headers[i].Name;
                if( i > 0 ) b.Append( " | " );
                b.Append( name ).Append( ' ', sizes[i] - name.Length );
            }
            b.AppendLine().Append( '-', allW ).AppendLine();
            for( int i = 0; i < rowCount; ++i )
            {
                for( int j = 0; j < sizes.Length; ++j )
                {
                    string val = grid[j, i];
                    if( j > 0 ) b.Append( " | " );
                    b.Append( val ).Append( ' ', sizes[j] - val.Length );
                }
                b.AppendLine();
            }
            return b.ToString();
        }
    }
}
