using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.SqlServer;

namespace CK.SqlServer
{
    /// <summary>
    /// Wraps a <see cref="SqlDataReader"/>.
    /// </summary>
    public sealed class CKDataReader : IDataReader
    {
        readonly SqlDataReader _innerReader;
        readonly IDisposable _connection;

        internal CKDataReader( SqlDataReader innerReader, IDisposable connection )
        {
            Debug.Assert( innerReader != null );
            Debug.Assert( connection != null );

            _innerReader = innerReader;
            _connection = connection;
        }

        /// <summary>
        /// Dipsoses the inner reader and inner connection.
        /// </summary>
        public void Dispose()
        {
            _innerReader.Dispose();
            _connection.Dispose();
        }

        /// <summary>
        /// A <see cref="CKDataReader"/> can be explicitly cast into its inner <see cref="SqlDataReader"/>.
        /// </summary>
        /// <param name="ckDataReader"></param>
        static public explicit operator SqlDataReader( CKDataReader ckDataReader )
        {
            if( ckDataReader == null ) return null;
            return ckDataReader._innerReader;
        }

        #region IDataReader

        /// <summary>
        /// Calls <see cref="Dispose"/>.
        /// </summary>
        void IDataReader.Close()
        {
            Dispose();
        }

        public int Depth => _innerReader.Depth; 

        public DataTable GetSchemaTable() => _innerReader.GetSchemaTable();

        public bool IsClosed => _innerReader.IsClosed; 

        public bool NextResult() => _innerReader.NextResult();

        public bool Read() => _innerReader.Read();

        public int RecordsAffected => _innerReader.RecordsAffected; 

        #endregion

        #region IDataRecord Members

        public int FieldCount => _innerReader.FieldCount; 

        public bool GetBoolean( int i ) => _innerReader.GetBoolean( i );

        public byte GetByte( int i ) => _innerReader.GetByte( i );

        public long GetBytes( int i, long fieldOffset, byte[] buffer, int bufferoffset, int length )
        {
            return _innerReader.GetBytes( i, fieldOffset, buffer, bufferoffset, length );
        }

        public char GetChar( int i ) => _innerReader.GetChar( i );

        public long GetChars( int i, long fieldoffset, char[] buffer, int bufferoffset, int length )
        {
            return _innerReader.GetChars( i, fieldoffset, buffer, bufferoffset, length );
        }

        public IDataReader GetData( int i ) => _innerReader.GetData( i );

        public string GetDataTypeName( int i ) => _innerReader.GetDataTypeName( i );

        public DateTime GetDateTime( int i ) => _innerReader.GetDateTime( i );

        public decimal GetDecimal( int i ) => _innerReader.GetDecimal( i );

        public double GetDouble( int i ) => _innerReader.GetDouble( i );

        public Type GetFieldType( int i ) => _innerReader.GetFieldType( i );

        public float GetFloat( int i ) => _innerReader.GetFloat( i );

        public Guid GetGuid( int i ) => _innerReader.GetGuid( i );

        public short GetInt16( int i ) => _innerReader.GetInt16( i );

        public int GetInt32( int i ) => _innerReader.GetInt32( i );

        public long GetInt64( int i ) => _innerReader.GetInt64( i );

        public string GetName( int i ) =>_innerReader.GetName( i );

        public int GetOrdinal( string name ) => _innerReader.GetOrdinal( name );

        public string GetString( int i ) => _innerReader.GetString( i );

        public object GetValue( int i ) => _innerReader.GetValue( i );

        public int GetValues( object[] values ) => _innerReader.GetValues( values );

        public bool IsDBNull( int i ) => _innerReader.IsDBNull( i );

        public object this[string name] => _innerReader[name]; 

        public object this[int i] => _innerReader[i];

        #endregion
    }
}
