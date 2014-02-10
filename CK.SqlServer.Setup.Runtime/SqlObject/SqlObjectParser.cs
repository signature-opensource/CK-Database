using System;
using System.Linq;
using System.Text.RegularExpressions;
using CK.Core;
using CK.Setup;

namespace CK.SqlServer.Setup
{
    public class SqlObjectParser : ISqlObjectParser
    {
        static Regex _rSqlObject = new Regex( @"(create|alter)\s+(?<1>proc(?:edure)?|function|view)\s+(\[?(?<2>\w+)]?\.)?(\[?(?<3>\w+)]?\.)?\[?(?<4>\w+)]?",
                                            RegexOptions.CultureInvariant
                                            | RegexOptions.IgnoreCase
                                            | RegexOptions.ExplicitCapture );

        static Regex _rHeader = new Regex( @"^\s*--\s*Version\s*=\s*(?<1>\d+(\.\d+)*|\*)\s*(,\s*(Package\s*=\s*(?<2>(\w|\.|-)+)|Requires\s*=\s*{\s*(?<3>\??(\w+|-|\^|\[|]|\.)+)\s*(,\s*(?<3>\??(\w+|-|\^|\[|]|\.)+)\s*)*}|Groups\s*=\s*{\s*(?<4>(\w+|-|\^|\[|]|\.)+)\s*(,\s*(?<4>(\w+|-|\^|\[|]|\.)+)\s*)*}|RequiredBy\s*=\s*{\s*(?<5>(\w+|-|\^|\[|]|\.)+)\s*(,\s*(?<5>(\w+|-|\^|\[|]|\.)+)\s*)*}|PreviousNames\s*=\s*{\s*((?<6>(\w+|-|\^|\[|]|\.)+)\s*=\s*(?<6>\d+\.\d+\.\d+(\.\d+)?))\s*(,\s*((?<6>(\w+|-|\^|\[|]|\.)+)\s*=\s*(?<6>\d+(\.\d+){1,3}))\s*)*})\s*)*",
                RegexOptions.CultureInvariant
                | RegexOptions.IgnoreCase
                | RegexOptions.ExplicitCapture );

        static Regex _rMissingDep = new Regex( @"MissingDependencyIsError\s*=\s*(?<1>\w+)",
                RegexOptions.CultureInvariant
                | RegexOptions.IgnoreCase
                | RegexOptions.ExplicitCapture );

        IDependentProtoItem ISqlObjectParser.Create( IActivityMonitor monitor, IContextLocNaming externalName, string text )
        {
            return SqlObjectParser.Create( monitor, externalName, text, null );
        }

        static public SqlObjectProtoItem Create( IActivityMonitor monitor, IContextLocNaming externalName, string text, string expectedType = null )
        {
            Match mSqlObject = _rSqlObject.Match( text );
            if( !mSqlObject.Success )
            {
                monitor.Error().Send( "Unable to detect create or alter statement for view, procedure or function (the object name must be Schema.Name)." );
                return null;
            }
            string type;
            switch( char.ToUpperInvariant( mSqlObject.Groups[1].Value[0] ) )
            {
                case 'V': type = SqlObjectProtoItem.TypeView; break;
                case 'P': type = SqlObjectProtoItem.TypeProcedure; break;
                default: type = SqlObjectProtoItem.TypeFunction; break;
            }
            if( expectedType != null && expectedType != type )
            {
                monitor.Error().Send( "Expected Sql object of type '{0}' but found a {1}.", expectedType, type );
                return null;
            }

            string header = text.Substring( 0, mSqlObject.Index );
            string textAfterName = text.Substring( mSqlObject.Index + mSqlObject.Length );

            Match mHeader = _rHeader.Match( header );
            if( !mHeader.Success )
            {
                monitor.Error().Send( "Invalid header: -- Version=X.Y.Z (with Major.Minor.Build) or Version=* must appear first in header.\r\n{0}", text );
                return null;
            }
            string packageName = null;
            string[] requires = null;
            string[] groups = null;
            string[] requiredBy = null;
            Version version = null;
            VersionedName[] previousNames = null;

            if( mHeader.Groups[2].Length > 0 ) packageName = mHeader.Groups[2].Value;
            if( mHeader.Groups[3].Captures.Count > 0 ) requires = mHeader.Groups[3].Captures.Cast<Capture>().Select( m => m.Value ).ToArray();
            if( mHeader.Groups[4].Captures.Count > 0 ) groups = mHeader.Groups[4].Captures.Cast<Capture>().Select( m => m.Value ).ToArray();
            if( mHeader.Groups[5].Captures.Count > 0 ) requiredBy = mHeader.Groups[5].Captures.Cast<Capture>().Select( m => m.Value ).ToArray();
            if( mHeader.Groups[6].Captures.Count > 0 )
            {
                var prevNames = mHeader.Groups[6].Captures.Cast<Capture>().Select( m => m.Value );
                var prevVer = mHeader.Groups[6].Captures.Cast<Capture>().Select( m => Version.Parse( m.Value ) );
                previousNames = prevNames.Zip( prevVer, ( n, v ) => new VersionedName( n, v ) ).ToArray();
            }
            if( mHeader.Groups[1].Length == 1 ) version = null;
            else if( !Version.TryParse( mHeader.Groups[1].Value, out version ) || version.Revision != -1 || version.Build == -1 )
            {
                monitor.Error().Send( "-- Version=X.Y.Z (with Major.Minor.Build) or Version=* must appear first in header." );
                return null;
            }
            bool? missingDep = null;
            Match missDep = _rMissingDep.Match( header );
            if( missDep.Success )
            {
                bool m;
                if( !bool.TryParse( missDep.Groups[1].Value, out m ) )
                {
                    monitor.Error().Send( "Invalid syntax: it should be MissingDependencyIsError = true or MissingDependencyIsError = false." );
                    return null;
                }
                missingDep = m;
            }
            string databaseOrSchema = mSqlObject.Groups[2].Value;
            string schema = mSqlObject.Groups[3].Value;
            string name = mSqlObject.Groups[4].Value;
            if( schema.Length == 0 && databaseOrSchema.Length > 0 )
            {
                string tmp = schema;
                schema = databaseOrSchema;
                databaseOrSchema = tmp;
            }
            return new SqlObjectProtoItem( externalName, type, databaseOrSchema, schema, name, header, version, packageName, missingDep, requires, groups, requiredBy, previousNames, textAfterName, text );
        }

    }
}
