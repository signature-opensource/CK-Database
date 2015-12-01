#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Core\CKDataReader.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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

        public void Dispose()
        {
            _innerReader.Dispose();
            _connection.Dispose();
        }

        static public explicit operator SqlDataReader( CKDataReader ckDataReader )
        {
            if( ckDataReader == null ) return null;
            return ckDataReader._innerReader;
        }

        #region IDataReader

        public void Close()
        {
            _innerReader.Close();
        }

        public int Depth
        {
            get { return _innerReader.Depth; }
        }

        public DataTable GetSchemaTable()
        {
            return _innerReader.GetSchemaTable();
        }

        public bool IsClosed
        {
            get { return _innerReader.IsClosed; }
        }

        public bool NextResult()
        {
            return _innerReader.NextResult();
        }

        public bool Read()
        {
            return _innerReader.Read();
        }

        public int RecordsAffected
        {
            get { return _innerReader.RecordsAffected; }
        }

        #endregion

        #region IDataRecord Members

        public int FieldCount
        {
            get { return _innerReader.FieldCount; }
        }

        public bool GetBoolean( int i )
        {
            return _innerReader.GetBoolean( i );
        }

        public byte GetByte( int i )
        {
            return _innerReader.GetByte( i );
        }

        public long GetBytes( int i, long fieldOffset, byte[] buffer, int bufferoffset, int length )
        {
            return _innerReader.GetBytes( i, fieldOffset, buffer, bufferoffset, length );
        }

        public char GetChar( int i )
        {
            return _innerReader.GetChar( i );
        }

        public long GetChars( int i, long fieldoffset, char[] buffer, int bufferoffset, int length )
        {
            return _innerReader.GetChars( i, fieldoffset, buffer, bufferoffset, length );
        }

        public IDataReader GetData( int i )
        {
            return _innerReader.GetData( i );
        }

        public string GetDataTypeName( int i )
        {
            return _innerReader.GetDataTypeName( i );
        }

        public DateTime GetDateTime( int i )
        {
            return _innerReader.GetDateTime( i );
        }

        public decimal GetDecimal( int i )
        {
            return _innerReader.GetDecimal( i );
        }

        public double GetDouble( int i )
        {
            return _innerReader.GetDouble( i );
        }

        public Type GetFieldType( int i )
        {
            return _innerReader.GetFieldType( i );
        }

        public float GetFloat( int i )
        {
            return _innerReader.GetFloat( i );
        }

        public Guid GetGuid( int i )
        {
            return _innerReader.GetGuid( i );
        }

        public short GetInt16( int i )
        {
            return _innerReader.GetInt16( i );
        }

        public int GetInt32( int i )
        {
            return _innerReader.GetInt32( i );
        }

        public long GetInt64( int i )
        {
            return _innerReader.GetInt64( i );
        }

        public string GetName( int i )
        {
            return _innerReader.GetName( i );
        }

        public int GetOrdinal( string name )
        {
            return _innerReader.GetOrdinal( name );
        }

        public string GetString( int i )
        {
            return _innerReader.GetString( i );
        }

        public object GetValue( int i )
        {
            return _innerReader.GetValue( i );
        }

        public int GetValues( object[] values )
        {
            return _innerReader.GetValues( values );
        }

        public bool IsDBNull( int i )
        {
            return _innerReader.IsDBNull( i );
        }

        public object this[string name]
        {
            get { return _innerReader[name]; }
        }

        public object this[int i]
        {
            get { return _innerReader[i]; }
        }

        #endregion
    }
}
