#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Core\SqlHelper.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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

        /// <summary>
        /// Writes the command as a text with its parameters. Handles stored procedure calls as well as simple text commands.
        /// </summary>
        /// <param name="w">The writer to use.</param>
        /// <param name="cmd">The command to write.</param>
        /// <returns>The writer.</returns>
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

        /// <summary>
        /// Returns a string the command its parameters. Handles stored procedure calls as well as simple text commands.
        /// </summary>
        /// <param name="cmd">The command to call.</param>
        /// <returns>A textual representation.</returns>
        static public string CommandAsText( SqlCommand cmd )
        {
            StringWriter w = new StringWriter();
            CommandAsText( w, cmd );
            return w.GetStringBuilder().ToString();
        }

        /// <summary>
        /// Writes to the given <see cref="TextWriter"/> the parameters separated by commas.
        /// </summary>
        /// <param name="w">The target <see cref="TextWriter"/>.</param>
        /// <param name="c"><see cref="SqlParameterCollection"/> to write.</param>
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


        static readonly Type[] _typesMap = new Type[] 
        {
            typeof(Int64), // SqlDbType.BigInt
            typeof(byte[]), // SqlDbType.Binary
            typeof(bool), // SqlDbType.Bit
            typeof(string), // SqlDbType.Char
            typeof(DateTime), // SqlDbType.DateTime
            typeof(Decimal), // SqlDbType.Decimal
            typeof(Double), // SqlDbType.Float
            typeof(byte[]), // SqlDbType.Image
            typeof(Int32), // SqlDbType.Int
            typeof(Decimal), // SqlDbType.Money
            typeof(string), // SqlDbType.NChar
            typeof(string), // SqlDbType.NText
            typeof(string), // SqlDbType.NVarChar
            typeof(Single), // SqlDbType.Real
            typeof(Guid), // SqlDbType.UniqueIdentifier
            typeof(DateTime), // SqlDbType.SmallDateTime
            typeof(Int16), // SqlDbType.SmallInt
            typeof(Decimal), // SqlDbType.SmallMoney
            typeof(string), // SqlDbType.Text
            typeof(byte[]), // SqlDbType.Timestamp
            typeof(byte), // SqlDbType.TinyInt
            typeof(byte[]), // SqlDbType.VarBinary
            typeof(string), // SqlDbType.VarChar
            typeof(object), // SqlDbType.Variant
            null,
            typeof(string), // SqlDbType.Xml
            null, null, null,
            typeof(object), // SqlDbType.Udt
            typeof(object), // SqlDbType.Structured
            typeof(DateTime), // SqlDbType.Date
            typeof(DateTime), // SqlDbType.Time
            typeof(DateTime), // SqlDbType.DateTime2
            typeof(DateTimeOffset), // SqlDbType.DateTimeOffset
        };

        /// <summary>
        /// Simple association to a Type from a Sql type.
        /// </summary>
        /// <param name="tSql">Sql type.</param>
        /// <returns>.net type to consider.</returns>
        static public Type FromSqlDbTypeToNetType( SqlDbType tSql )
        {
            Debug.Assert( _typesMap.Length == 35 );
            return _typesMap[(int)tSql];
        }

        /// <summary>
        /// Tests whether a type has a corresponding <see cref="SqlDbType"/>. 
        /// It is all the types that are mapped by <see cref="FromSqlDbTypeToNetType"/> except <see cref="Object"/> plus <see cref="Char"/>
        /// and any <see cref="Nullable{T}"/> where T is mapped.
        /// </summary>
        /// <param name="t">Type to challenge.</param>
        /// <returns>True if this type can be mapped to a basic Sql type.</returns>
        static public bool IsNetTypeMapped( Type t )
        {
            if( t == null ) throw new ArgumentNullException();
            if( t == typeof( object ) ) return false;
            var nT = Nullable.GetUnderlyingType( t );
            if( nT != null ) t = nT;
            if( t == typeof( char ) ) return true;
            return _typesMap.Any( m => m == t );
        }


        /// <summary>
        /// Express a value of a given <see cref="SqlDbType"/> into a syntaxically compatible string. 
        /// </summary>
        /// <param name="v">Object for which a string representation must be obtained.</param>
        /// <param name="dbType">Sql type.</param>
        /// <returns>A sql string that represents the value.</returns>
        static public string SqlValue( object v, SqlDbType dbType )
        {
            if( v == null || v == DBNull.Value ) return "null";
            switch( dbType )
            {
                case SqlDbType.NVarChar: return String.Format( "N'{0}'", SqlEncode( Convert.ToString( v, CultureInfo.InvariantCulture ) ) );
                case SqlDbType.Int: return Convert.ToString( v, CultureInfo.InvariantCulture );
                case SqlDbType.Bit: return Convert.ToBoolean( v ) ? "1" : "0";
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
                case SqlDbType.Decimal: return Convert.ToString( v, CultureInfo.InvariantCulture );
                case SqlDbType.Xml: return String.Format( "cast( '{0}' as xml )", SqlEncode( Convert.ToString( v, CultureInfo.InvariantCulture ) ) );
                case SqlDbType.Structured: return Convert.ToString( v, CultureInfo.InvariantCulture );

                default: throw new Exception( "No sql string representation for:" + dbType.ToString() );
            }
        }

        /// <summary>
        /// Determines whether a <see cref="SqlDbType"/> usually requires a length. 
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
        /// appropriates encoding. Then, if <paramref name="expandWildCards"/> is true, 
        /// expands <c>*</c> and <c>?</c> by appropriate pattern markers.
        /// </summary>
        /// <param name="s">The starting string.</param>
        /// <param name="expandWildCards">True if the pattern contains * and ? that must be expanded.. See remarks.</param>
        /// <param name="innerPattern">True to ensure that the pattern starts and ends with a %. See remarks.</param>
        /// <returns>An encoded string.</returns>
        /// <remarks>
        /// When <paramref name="expandWildCards"/> is true, use \* for a real *, \? for a 
        /// real ?. \ can be used directly except when directly followed by *, ? or another \: it must then be duplicated.<br/>
        /// When <paramref name="innerPattern"/> is true, an empty or null string is returned as '%'.
        /// </remarks>
        static public string SqlEncodePattern( string s, bool expandWildCards, bool innerPattern )
        {
            if( s == null || s.Length == 0 ) return innerPattern ? "%" : String.Empty;
            StringBuilder b = new StringBuilder( s );
            b.Replace( "'", "''" );
            b.Replace( "[", "[[]" );
            b.Replace( "_", "[_]" );
            b.Replace( "%", "[%]" );
            if( expandWildCards )
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
        /// The 'GO' may be lowercase but must always be on the first column of the line (i.e. 
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
