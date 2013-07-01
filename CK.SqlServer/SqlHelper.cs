using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CK.SqlServer
{
    /// <summary>
    /// Offers utility methods to deal with Sql Server objects and data.
    /// </summary>
    public class SqlHelper
    {
        /// <summary>
        /// Standard name of the return value. Applied to functions and stored procedures.
        /// </summary>
        public const string ReturnParameterName = "RETURN_VALUE";

        static Regex _rGo = new Regex( @"^GO(?:\s|$)+", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled );

        static public TextWriter CommandAsText( TextWriter w, SqlCommand cmd )
        {
            if( cmd.CommandType == System.Data.CommandType.StoredProcedure )
            {
                w.Write( "exec {0} <= ", cmd.CommandText );
                WriteCallParameters( w, cmd.Parameters );
            }
            else
            {
                WriteCallParameters( w, cmd.Parameters );
                w.Write( " => " );
                w.Write( cmd.CommandText );
            }
            return w;
        }

        static public string CommandAsText( SqlCommand cmd )
        {
            StringWriter w = new StringWriter();
            CommandAsText( w, cmd );
            return w.GetStringBuilder().ToString();
        }

        /// <summary>
        /// Writes to the given <see cref="TextWriter"/> the parameters separated by commas.
        /// </summary>
        /// <param name="b">The target <see cref="TextWriter"/>.</param>
        /// <param name="c"><see cref="SqlParameterCollection"/> to write.</param
        static public TextWriter WriteCallParameters( TextWriter w, SqlParameterCollection c )
        {
            bool atLeastOne = false;
            foreach( SqlParameter p in c )
            {
                if( p.Direction != ParameterDirection.ReturnValue )
                {
                    if( atLeastOne ) w.Write( ", " );
                    else atLeastOne = true;
                    w.Write( p.ParameterName );
                    w.Write( '=' );
                    w.Write( SqlValue( p.Value, p.SqlDbType ) );
                    if( p.Direction != ParameterDirection.Input ) w.Write( " output" );
                }
            }
            return w;
        }

        static public string SqlValue( object v, SqlDbType dbType )
        {
            if( v == null || v == DBNull.Value ) return "null";
            switch( dbType )
            {
                case SqlDbType.NVarChar: return String.Format( "N'{0}'", SqlEncode( Convert.ToString( v, CultureInfo.InvariantCulture ) ) );
                case SqlDbType.Int: return Convert.ToString( v, CultureInfo.InvariantCulture );
                case SqlDbType.Bit: return (bool)v ? "1" : "0";
                case SqlDbType.Char: goto case SqlDbType.VarChar;
                case SqlDbType.VarChar: return String.Format( "'{0}'", SqlEncode( Convert.ToString( v, CultureInfo.InvariantCulture ) ) );
                case SqlDbType.NChar: goto case SqlDbType.NVarChar;
                case SqlDbType.DateTime: return String.Format( "convert( DateTime, '{0:s}', 126 )", v );
                case SqlDbType.DateTime2: return String.Format( "'{0:O}'", v );
                case SqlDbType.TinyInt: return Convert.ToString( v, CultureInfo.InvariantCulture );
                case SqlDbType.UniqueIdentifier: return ((Guid)v).ToString( "B" );
                case SqlDbType.SmallInt: return Convert.ToString( v, CultureInfo.InvariantCulture );
                case SqlDbType.SmallDateTime: return String.Format( "convert( SmallDateTime, '{0:s}', 126 )", v );
                case SqlDbType.BigInt: return Convert.ToString( v, CultureInfo.InvariantCulture );
                case SqlDbType.NText: goto case SqlDbType.NVarChar;
                case SqlDbType.Float: return Convert.ToString( v, CultureInfo.InvariantCulture );
                case SqlDbType.Real: return Convert.ToString( v, CultureInfo.InvariantCulture );
                case SqlDbType.Money: return Convert.ToString( v, CultureInfo.InvariantCulture );
                case SqlDbType.Xml: return String.Format( "cast( '{0}' as xml )", SqlEncode( Convert.ToString( v, CultureInfo.InvariantCulture ) ) );
                case SqlDbType.Structured: return Convert.ToString( v, CultureInfo.InvariantCulture );

                default: throw new Exception( "No sql string representation for:" + dbType.ToString() );
            }
        }

        /// <summary>
        /// Determines wether a <see cref="SqlDbType"/> usually requires a length. 
        /// </summary>
        /// <param name="dbType">The type to challenge.</param>
        /// <returns>True for <see cref="SqlDbType.NVarChar"/>, <see cref="SqlDbType.NChar"/>, 
        /// <see cref="SqlDbType.VarChar"/>, <see cref="SqlDbType.Char"/>, <see cref="SqlDbType.Binary"/>
        /// and <see cref="SqlDbType.VarBinary"/>. False otherwise.</returns>
        /// <remarks>
        /// Theoretically these types can be used with a default length of 1, 
        /// but we choose to consider them to always be used as 'variable length array' types.
        /// </remarks>
        static public bool IsArrayType( SqlDbType dbType )
        {
            switch( dbType )
            {
                case SqlDbType.NVarChar: return true;
                case SqlDbType.VarChar: return true;
                case SqlDbType.Char: return true;
                case SqlDbType.NChar: return true;
                case SqlDbType.Binary: return true;
                case SqlDbType.VarBinary: return true;
                default: return false;
            }
        }

        /// <summary>
        /// Transforms a <see cref="IDataReader"/> into a <see cref="DataTable"/>.
        /// The reader is forwarded while <see cref="IDataReader.Read"/> returns true. 
        /// It is left opened since a next result set may exist.
        /// </summary>
        /// <param name="r">The <see cref="IDataReader"/> to transform. Can be null and in this case null is returned.</param>
        /// <returns>A <see cref="DataTable"/> populated with the content of <paramref name="r"/>, or null if the data reader was null.</returns>
        static public DataTable ToDataTable( IDataReader r )
        {
            if( r == null ) return null;
            DataTable t = new DataTable();
            FillDataTable( r, t );
            return t;
        }

        /// <summary>
        /// Populates an empty <see cref="DataTable"/> with the content of a <see cref="IDataReader"/>.
        /// The reader is forwarded while <see cref="IDataReader.Read"/> returns true.
        /// It is left opened since another result set may exist.
        /// </summary>
        /// <param name="r">The <see cref="IDataReader"/> to process. Can be null and in this case nothing is done.</param>
        /// <param name="target">An empty <see cref="DataTable"/>. If columns exist, an exception is thrown.</param>
        static public void FillDataTable( IDataReader r, DataTable target )
        {
            if( target.Columns.Count > 0 )
                throw new ApplicationException( "An empty DataTable must be provided (no columns must exist)." );
            if( r != null )
            {
                DataTable schema = r.GetSchemaTable();

                Debug.Assert( schema.Columns[0].ColumnName == "ColumnName" );
                Debug.Assert( schema.Columns[12].ColumnName == "DataType" );

                foreach( DataRow rSchema in schema.Rows )
                {
                    target.Columns.Add( new DataColumn( rSchema[0].ToString(), rSchema[12] as Type ) );
                }
                object[] row = new object[r.FieldCount];
                target.BeginLoadData();
                while( r.Read() )
                {
                    r.GetValues( row );
                    target.LoadDataRow( row, true );
                }
                target.EndLoadData();
            }
        }

        /// <summary>
        /// Provides a correct string content by replacing ' with ''.
        /// </summary>
        /// <param name="s">The starting string.</param>
        /// <returns>An encoded string.</returns>
        static public string SqlEncode( string s )
        {
            if( s == null ) return String.Empty;
            s = s.Replace( "'", "''" );
            return s;
        }

        /// <summary>
        /// Protects pattern meta character of Sql Server: <c>[</c>, <c>_</c> and <c>%</c> by 
        /// appropriates encoding. Then, if <paramref name="expandStarsAndMaks"/> is true, 
        /// expands <c>*</c> and <c>?</c> by appropriate pattern markers.
        /// </summary>
        /// <param name="s">The starting string.</param>
        /// <param name="expandStdWildCards">True if the pattern contains * and ? that must be expanded.. See remarks.</param>
        /// <param name="innerPattern">True to ensure that the pattern starts and ends with a %. See remarks.</param>
        /// <returns>An encoded string.</returns>
        /// <remarks>
        /// When <paramref name="expandStdWildCards"/> is true, use \* for a real *, \? for a 
        /// real ?. \ can be used directly except when directly followed by *, ? or another \: it must then be duplicated.<br/>
        /// When <paramref name="innerPattern"/> is true, an empty or null string is returned as '%'.
        /// </remarks>
        static public string SqlEncodePattern( string s, bool expandStdWildCards, bool innerPattern )
        {
            if( s == null || s.Length == 0 ) return innerPattern ? "%" : String.Empty;
            StringBuilder b = new StringBuilder( s );
            b.Replace( "'", "''" );
            b.Replace( "[", "[[]" );
            b.Replace( "_", "[_]" );
            b.Replace( "%", "[%]" );
            if( expandStdWildCards )
            {
                b.Replace( @"\\", "\x0" );
                b.Replace( @"\*", "\x1" );
                b.Replace( @"\?", "\x2" );
                b.Replace( '*', '%' );
                b.Replace( '?', '_' );
                b.Replace( '\x0', '\\' );
                b.Replace( '\x1', '*' );
                b.Replace( '\x2', '?' );
            }
            if( innerPattern )
            {
                if( b[0] != '%' ) b.Insert( 0, '%' );
                if( b.Length > 1 && b[b.Length - 1] != '%' ) b.Append( '%' );
            }
            return b.ToString();
        }

        /// <summary>
        /// Splits a script that may contain 'GO' separators (must be on the first column).
        /// </summary>
        /// <param name="script">Script to split.</param>
        /// <returns>Zero (if the <paramref name="script"/> was null empty) or more scripts.</returns>
        /// <remarks>
        /// The 'GO' may be lowercase but must always be on the first colum of the line (i.e. 
        /// there must be no white space nor tabs before it).
        /// </remarks>
        static public IEnumerable<string> SplitGoSeparator( string script )
        {
            if( !String.IsNullOrWhiteSpace( script ) )
            {
                int curBeg = 0;
                for( Match goDelim = _rGo.Match( script ); goDelim.Success; goDelim = goDelim.NextMatch() )
                {
                    int lenScript = goDelim.Index - curBeg;
                    if( lenScript > 0 )
                    {
                        yield return script.Substring( curBeg, lenScript );
                    }
                    curBeg = goDelim.Index + goDelim.Length;
                }
                if( script.Length > curBeg )
                {
                    yield return script.Substring( curBeg ).TrimEnd();
                }
            }
        }
    }
}
