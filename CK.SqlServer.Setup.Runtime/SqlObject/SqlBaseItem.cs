using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using CK.Core;
using CK.Setup;
using System.Text;
using CK.SqlServer.Parser;
using System.Text.RegularExpressions;
using System.Linq;
using CK.Text;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Base class for <see cref="SqlObjectItem"/> and <see cref="SqlTransformerItem"/>.
    /// </summary>
    public abstract class SqlBaseItem : SetupObjectItemV
    {
        ISqlServerParsedText _sqlObject;
        List<SqlTransformerItem> _transformers;

        internal SqlBaseItem()
        {
        }

        internal SqlBaseItem( SqlContextLocName name, string itemType, ISqlServerParsedText parsed )
            : base( name, itemType )
        {
            _sqlObject = parsed;
        }

        public new SqlBaseItem TransformTarget => (SqlBaseItem)base.TransformTarget;

        public new SqlContextLocName ContextLocName
        {
            get { return (SqlContextLocName)base.ContextLocName; }
            set { base.ContextLocName = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="ISqlServerParsedText"/> associated object.
        /// </summary>
        public ISqlServerParsedText SqlObject
        {
            get { return _sqlObject; }
            set { _sqlObject = value; }
        }

        internal SqlBaseItem AddTransformer( IActivityMonitor monitor, SqlTransformerItem sqlTransformerItem )
        {
            if( _transformers == null )
            {
                AssumeTransformTarget( monitor );
                _transformers = new List<SqlTransformerItem>();
                _transformers.Add( sqlTransformerItem );
            }
            return TransformTarget;
        }

        public IReadOnlyList<SqlTransformerItem> Transformers => _transformers;

        static Regex _rHeader = new Regex( @"^\s*--\s*(Version\s*=\s*(?<1>\d+(\.\d+)*|\*))?\s*(,\s*(Package\s*=\s*(?<2>(\w|\.|-)+)|Requires\s*=\s*{\s*(?<3>\??(\w+|-|\^|\[|]|\.)+)\s*(,\s*(?<3>\??(\w+|-|\^|\[|]|\.)+)\s*)*}|Groups\s*=\s*{\s*(?<4>(\w+|-|\^|\[|]|\.)+)\s*(,\s*(?<4>(\w+|-|\^|\[|]|\.)+)\s*)*}|RequiredBy\s*=\s*{\s*(?<5>(\w+|-|\^|\[|]|\.)+)\s*(,\s*(?<5>(\w+|-|\^|\[|]|\.)+)\s*)*}|PreviousNames\s*=\s*{\s*((?<6>(\w+|-|\^|\[|]|\.)+)\s*=\s*(?<6>\d+\.\d+\.\d+(\.\d+)?))\s*(,\s*((?<6>(\w+|-|\^|\[|]|\.)+)\s*=\s*(?<6>\d+(\.\d+){1,3}))\s*)*})\s*)*",
                                                RegexOptions.CultureInvariant
                                                | RegexOptions.IgnoreCase
                                                | RegexOptions.ExplicitCapture );

        internal virtual bool Initialize( IActivityMonitor monitor, string fileName, IDependentItemContainer packageItem )
        {
            return SetPropertiesFromHeader( monitor );
        }

        bool SetPropertiesFromHeader( IActivityMonitor monitor )
        {
            // TODO: rewrite this to handle parts indenpently accross the header comment strings.
            string header = _sqlObject.HeaderComments.Select( h => h.ToString() ).Concatenate( string.Empty );
            Match mHeader = _rHeader.Match( header );
            if( !mHeader.Success )
            {
                monitor.Error().Send( "Invalid header: -- Version=X.Y.Z or Version=* must appear first in header." );
                return false;
            }
            string packageName = null;
            IEnumerable<string> requires = null;
            IEnumerable<string> groups = null;
            IEnumerable<string> requiredBy = null;
            Version version = null;
            IEnumerable<VersionedName> previousNames = null;

            if( mHeader.Groups[2].Length > 0 ) packageName = mHeader.Groups[2].Value;
            if( mHeader.Groups[3].Captures.Count > 0 ) requires = mHeader.Groups[3].Captures.Cast<Capture>().Select( m => m.Value );
            if( mHeader.Groups[4].Captures.Count > 0 ) groups = mHeader.Groups[4].Captures.Cast<Capture>().Select( m => m.Value );
            if( mHeader.Groups[5].Captures.Count > 0 ) requiredBy = mHeader.Groups[5].Captures.Cast<Capture>().Select( m => m.Value );
            if( mHeader.Groups[6].Captures.Count > 0 )
            {
                var prevNames = mHeader.Groups[6].Captures.Cast<Capture>().Select( m => m.Value );
                var prevVer = mHeader.Groups[6].Captures.Cast<Capture>().Select( m => Version.Parse( m.Value ) );
                previousNames = prevNames.Zip( prevVer, ( n, v ) => new VersionedName( n, v ) );
            }
            if( mHeader.Groups[1].Length <= 1 ) version = null;
            else if( !Version.TryParse( mHeader.Groups[1].Value, out version ) || version.Revision != -1 || version.Build == -1 )
            {
                monitor.Error().Send( "-- Version=X.Y.Z or Version=* must appear first in header." );
                return false;
            }
            if( version != null ) Version = version;
            if( packageName != null ) Container = new NamedDependentItemContainerRef( packageName );
            if( requires != null ) Requires.Add( requires );
            if( requiredBy != null ) RequiredBy.Add( requiredBy );
            if( groups != null ) Groups.Add( groups );
            if( previousNames != null ) PreviousNames.AddRange( previousNames );
            return true;
        }

        /// <summary>
        /// Centralized parsing function. Returns null on error.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="name">Name of the item.</param>
        /// <param name="parser">The parser to use.</param>
        /// <param name="text">The test to parse.</param>
        /// <param name="fileName">The file name to use in traces.</param>
        /// <param name="packageItem">Optional package that defines this item if known.</param>
        /// <param name="expectedItemTypes">Optional restrictions of expected item types.</param>
        /// <returns>A new initialized <see cref="SqlBaseItem"/> or null on error.</returns>
        public static SqlBaseItem Parse(
                    IActivityMonitor monitor,
                    SqlContextLocName name,
                    ISqlServerParser parser,
                    string text,
                    string fileName,
                    IDependentItemContainer packageItem,
                    IEnumerable<string> expectedItemTypes )
        {
            try
            {
                var r = parser.Parse( text );
                if( r.IsError )
                {
                    r.LogOnError( monitor );
                    return null;
                }
                ISqlServerParsedText o = r.Result;
                SqlBaseItem result = null;
                if( o is ISqlServerObject )
                {
                    if( o is ISqlServerCallableObject )
                    {
                        if( o is ISqlServerStoredProcedure )
                        {
                            result = new SqlProcedureItem( name, (ISqlServerStoredProcedure)o );
                        }
                        else if( o is ISqlServerFunctionScalar )
                        {
                            result = new SqlFunctionScalarItem( name, (ISqlServerFunctionScalar)o );
                        }
                        else if( o is ISqlServerFunctionInlineTable )
                        {
                            result = new SqlFunctionInlineTableItem( name, (ISqlServerFunctionInlineTable)o );
                        }
                        else if( o is ISqlServerFunctionTable )
                        {
                            result = new SqlFunctionTableItem( name, (ISqlServerFunctionTable)o );
                        }
                    }
                    else if( o is ISqlServerView )
                    {
                        result = new SqlViewObjectItem( name, (ISqlServerView)o );
                    }
                }
                else if( o is ISqlServerTransformer )
                {
                    result = new SqlTransformerItem( name, (ISqlServerTransformer)o );
                }

                if( result == null )
                {
                    throw new NotSupportedException( "Unhandled type of object: " + o.ToString() );
                }
                if( expectedItemTypes != null && !expectedItemTypes.Contains( result.ItemType ) )
                {
                    monitor.Error().Send( $"Resource '{fileName}' of '{packageItem?.FullName}' is a '{result.ItemType}' whereas '{expectedItemTypes.Concatenate( "' or '" )}' is expected." );
                    return null;
                }
                return result.Initialize( monitor, fileName, packageItem ) ? result : null;
            }
            catch( Exception ex )
            {
                using( monitor.OpenError().Send( ex, $"While parsing '{fileName}'." ) )
                {
                    monitor.Info().Send( text );
                }
                return null;
            }
        }


    }
}
