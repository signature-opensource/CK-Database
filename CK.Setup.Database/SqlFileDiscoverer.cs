using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CK.Core;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace CK.Setup.Database
{
    public class SqlFileDiscoverer : IDependentItemDiscoverer
    {
        ISqlObjectBuilder _sqlObjectBuilder;
        IActivityLogger _logger;
        List<DynamicPackage> _packages;
        List<ISetupableItem> _sqlObjects;

        int _packageDiscoverErrorCount;
        int _sqlFileDiscoverErrorCount;

        public SqlFileDiscoverer( ISqlObjectBuilder sqlObjectBuilder, IActivityLogger logger )
        {
            if( sqlObjectBuilder == null ) throw new ArgumentNullException( "sqlObjectBuilder" );
            if( logger == null ) throw new ArgumentNullException( "logger" );
 
            _sqlObjectBuilder = sqlObjectBuilder;
            _logger = logger;

            _packages = new List<DynamicPackage>();
            _sqlObjects = new List<ISetupableItem>();
        }

        public int PackageDiscoverErrorCount
        {
            get { return _packageDiscoverErrorCount; }
        }

        public int SqlFileDiscoverErrorCount
        {
            get { return _sqlFileDiscoverErrorCount; }
        }

        public bool DiscoverPackages( string directoryPath )
        {
            using( _logger.OpenGroup( LogLevel.Info, "Discovering *.ck package files in '{0}'.", directoryPath ) )
            {
                CheckDirectoryPath( directoryPath );
                bool result = true;
                foreach( var path in Directory.EnumerateFiles( directoryPath, "*.ck", SearchOption.AllDirectories ) )
                {
                    if( !RegisterPackage( path ) )
                    {
                        _packageDiscoverErrorCount++;
                        result = false;
                    }
                }
                return result;
            }
        }

        public bool DiscoverSqlFiles( string directoryPath, PackageScriptCollector collector, string sqlFileScriptType = "file-sql" )
        {
            using( _logger.OpenGroup( LogLevel.Info, "Discovering Sql files in '{0}' for type '{1}'.", directoryPath, sqlFileScriptType ) )
            {
                CheckDirectoryPath( directoryPath );
                if( String.IsNullOrWhiteSpace( sqlFileScriptType ) ) throw new ArgumentException( "Must not be null, empty or white space.", "sqlFileScriptType" );

                bool result = true;
                foreach( var path in Directory.EnumerateFiles( directoryPath, "*.sql", SearchOption.AllDirectories ) )
                {
                    if( !RegisterFileSql( path, collector, sqlFileScriptType ) )
                    {
                        _sqlFileDiscoverErrorCount++;
                        result = false;
                    }
                }
                return result;
            }
        }

        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetOtherItemsToRegister()
        {
            return _packages.Concat( _sqlObjects );
        }

        static Regex _rHeader = new Regex( @"^\s*--\s*Version\s*=\s*(?<1>\d+(\.\d+)*|\*)(\s*,?\s*((Package\s*=\s*(?<2>(\w|\.|-)+))|(Requires\s*=\s*{\s*((?<3>\??(\w+|-|\.)+)\s*,?\s*)*})|((RequiredBy\s*=\s*{\s*((?<4>(\w+|-|\.)+)\s*,?\s*)*}))|(PreviousNames\s*=\s*{\s*(((?<5>(\w|\.|-)+)\s*=\s*(?<6>\d+\.\d+\.\d+))\s*,?\s*)*})))*",
            RegexOptions.CultureInvariant
            | RegexOptions.IgnoreCase
            | RegexOptions.ExplicitCapture );

        bool RegisterFileSql( string path, PackageScriptCollector collector, string sqlFileScriptType )
        {
            ParsedFileName f;
            if( !ParsedFileName.TryParse( Path.GetFileName( path ), Path.GetDirectoryName( path ), true, out f ) ) return false;
            return DoRegisterSql( f, collector, () => File.ReadAllText( path ), () => new FileSetupScript( f, sqlFileScriptType ) );
        }

        bool DoRegisterSql( ParsedFileName f, PackageScriptCollector collector, Func<string> readContent, Func<ISetupScript> createSetupScript )
        {
            if( f.SetupStep != SetupStep.None )
            {
                // It is a script related to a package: if a collector exists, register a SetupScript.
                if( collector == null ) return true;
                if( collector.Add( createSetupScript(), _logger ) != null )
                {
                    _logger.Trace( "File '{0}' discovered.", f.FileName );
                    return true;
                }
                return false;
            }
            using( _logger.OpenGroup( LogLevel.Trace, "Discovering '{0}'.", f.FileName ) )
            {
                try
                {
                    // There is no SetupStep suffix: it should be a SqlObject.
                    string text = readContent();
                    SqlObjectPreParse pre = _sqlObjectBuilder.PreParse( _logger, text );
                    if( pre != null )
                    {
                        Match mHeader = _rHeader.Match( pre.Header );
                        if( !mHeader.Success )
                        {
                            _logger.Warn( "Unable to read header: {0}", text.Substring( 0, Math.Max( text.Length, 80 ) ) );
                            return false;
                        }
                        SetupableItemData data = new SetupableItemData();
                        data.FullName = f.ContainerFullName;
                        if( mHeader.Groups[1].Length == 1 ) data.Version = null;
                        else if( !Version.TryParse( mHeader.Groups[1].Value, out data.Version ) || data.Version.Revision != -1 || data.Version.Build == -1 )
                        {
                            _logger.Error( "-- Version=X.Y.Z (with Major.Minor.Build) must appear first in header." );
                            return false;
                        }
                        if( mHeader.Groups[2].Length > 0 ) data.Container = new DependentItemContainerRef( mHeader.Groups[2].Value );
                        if( mHeader.Groups[3].Captures.Count > 0 ) data.Requires = mHeader.Groups[3].Captures.Cast<Group>().Select( m => m.Value ).ToArray();
                        if( mHeader.Groups[4].Captures.Count > 0 ) data.RequiredBy = mHeader.Groups[4].Captures.Cast<Group>().Select( m => m.Value ).ToArray();
                        if( mHeader.Groups[5].Captures.Count > 0 )
                        {
                            var prevNames = mHeader.Groups[5].Captures.Cast<Group>().Select( m => m.Value );
                            var prevVer = mHeader.Groups[5].Captures.Cast<Group>().Select( m => Version.Parse( m.Value ) );
                            data.PreviousNames = prevNames.Zip( prevVer, ( n, v ) => new VersionedName( n, v ) ).ToArray();
                        }
                        ISetupableItem item = _sqlObjectBuilder.Create( _logger, pre, data );
                        if( item != null )
                        {
                            _logger.CloseGroup( item.FullName );
                            _sqlObjects.Add( item );
                            return true;
                        }
                    }
                }
                catch( Exception ex )
                {
                    _logger.Error( ex );
                }
            }
            return false;
        }

        private bool RegisterPackage( string path )
        {
            using( _logger.OpenGroup( LogLevel.Trace, "Registering '{0}'.", Path.GetFileName( path ) ) )
            {
                XDocument doc = XDocument.Load( path );
                XElement e = doc.Root;
                if( e.Name != "SetupPackage" )
                {
                    _logger.Warn( "The root element must be 'SetupPackage'.", path );
                    return false;
                }
                try
                {
                    DynamicPackage p = ReadPackageFileFormat( e );
                    _packages.Add( p );
                    return true;
                }
                catch( Exception ex )
                {
                    _logger.Error( ex );
                }
                return false;
            }
        }

        static public DynamicPackage ReadPackageFileFormat( XElement e )
        {
            DynamicPackage p = new DynamicPackage();
            p.FullName = (string)e.AttributeRequired( "FullName" );
            p.SetVersionsString( (string)e.AttributeRequired( "Versions" ) );
            p.Requires.Clear();
            foreach( var a in e.Elements( "Requirements" ).Attributes( "Requires" ) ) p.AddRequiresString( (string)a );
            p.RequiredBy.Clear();
            foreach( var a in e.Elements( "Requirements" ).Attributes( "RequiredBy" ) ) p.AddRequiredByString( (string)a );

            XElement model = e.Elements( "Model" ).SingleOrDefault();
            if( model != null )
            {
                p.EnsureModel().Requires.Clear();
                foreach( var a in model.Elements( "Requirements" ).Attributes( "Requires" ) ) p.Model.AddRequiresString( (string)a );
                p.Model.RequiredBy.Clear();
                foreach( var a in e.Elements( "Requirements" ).Attributes( "RequiredBy" ) ) p.Model.AddRequiredByString( (string)a );
            }
            else p.SupressModel();
            p.Children.Clear();
            XElement content = e.Elements( "Content" ).SingleOrDefault();
            if( content != null )
            {
                foreach( var add in content.Elements( "Add" ) )
                {
                    p.Children.Add( new DependentItemRef( (string)add.AttributeRequired( "FullName" ) ) );
                }
            }
            return p;
        }

        private static void CheckDirectoryPath( string directoryPath )
        {
            if( directoryPath == null ) throw new ArgumentNullException( directoryPath );
            if( !Path.IsPathRooted( directoryPath ) ) throw new ArgumentException( "Path must be rooted.", directoryPath );
        }


    }
}
