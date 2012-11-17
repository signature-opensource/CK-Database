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
    public class SqlFileDiscoverer
    {
        /// <summary>
        /// The default <see cref="ScriptSource.Name"/> used by <see cref="DiscoverSqlFiles"/> is "file-sql".
        /// </summary>
        public const string DefaultSourceName = "file-sql";

        ISqlObjectParser _sqlObjectParser;
        IActivityLogger _logger;
        List<DynamicPackageItem> _packages;
        IReadOnlyList<DynamicPackageItem> _packagesEx;

        int _packageDiscoverErrorCount;
        int _sqlFileDiscoverErrorCount;

        public SqlFileDiscoverer( ISqlObjectParser sqlObjectParser, IActivityLogger logger )
        {
            if( sqlObjectParser == null ) throw new ArgumentNullException( "sqlObjectBuilder" );
            if( logger == null ) throw new ArgumentNullException( "logger" );
 
            _sqlObjectParser = sqlObjectParser;
            _logger = logger;

            _packages = new List<DynamicPackageItem>();
            _packagesEx = new ReadOnlyListOnIList<DynamicPackageItem>( _packages );
        }

        public int PackageDiscoverErrorCount
        {
            get { return _packageDiscoverErrorCount; }
        }

        public int SqlFileDiscoverErrorCount
        {
            get { return _sqlFileDiscoverErrorCount; }
        }

        public IReadOnlyList<DynamicPackageItem> DiscoveredPackages
        {
            get { return _packagesEx; }
        }

        /// <summary>
        /// Discovers *.ck files recursively in a directory and collects them as <see cref="DynamicPackageItem"/> exposed by <see cref="DiscoveredPackages"/> property.
        /// </summary>
        /// <param name="directoryPath">Root path to start.</param>
        /// <returns>True on success. Any warn or error are logged in the <see cref="IActivityLogger"/> that has been provided to the constructor.</returns>
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

        /// <summary>
        /// Discovers *.sql files recursively in a directory and, depending on their type, either registers them in the script <paramref name="collector"/>
        /// or consider them as <see cref="IDependentProtoItem"/> and collects them in <paramref name="itemCollector"/>.
        /// </summary>
        /// <param name="directoryPath">Root path to start.</param>
        /// <param name="itemCollector">Collector for discovered items.</param>
        /// <param name="collector">Optional scripts collector.</param>
        /// <param name="sqlFileScriptSource">The <see cref="ScriptSource.Name"/> for the <paramref name="collector"/>.
        /// It must have been registered as a source in a <see cref="ScriptTypeHandler"/>, itself registered in the <see cref="ScriptTypeManager"/> associated to the collector.
        /// </param>
        /// <returns>True on success. Any warn or error are logged in the <see cref="IActivityLogger"/> that has been provided to the constructor.</returns>
        public bool DiscoverSqlFiles( string directoryPath, DependentProtoItemCollector itemCollector, ScriptCollector collector = null, string sqlFileScriptSource = DefaultSourceName )
        {
            using( _logger.OpenGroup( LogLevel.Info, "Discovering Sql files in '{0}' for source '{1}'.", directoryPath, sqlFileScriptSource ) )
            {
                CheckDirectoryPath( directoryPath );
                if( String.IsNullOrWhiteSpace( sqlFileScriptSource ) ) throw new ArgumentException( "Must not be null, empty or white space.", "sqlFileScriptSource" );

                bool result = true;
                foreach( var path in Directory.EnumerateFiles( directoryPath, "*.sql", SearchOption.AllDirectories ) )
                {
                    if( !RegisterFileSql( path, itemCollector, collector, sqlFileScriptSource ) )
                    {
                        _sqlFileDiscoverErrorCount++;
                        result = false;
                    }
                }
                return result;
            }
        }

        bool RegisterFileSql( string path, DependentProtoItemCollector itemCollector, ScriptCollector collector, string sqlFileScriptSource )
        {
            ParsedFileName f;
            if( !ParsedFileName.TryParse( Path.GetFileName( path ), Path.GetDirectoryName( path ), true, out f ) ) return false;
            return DoRegisterSql( f, itemCollector, collector, () => File.ReadAllText( path ), () => new FileSetupScript( f, sqlFileScriptSource ) );
        }

        bool DoRegisterSql( ParsedFileName f, DependentProtoItemCollector itemCollector, ScriptCollector collector, Func<string> readContent, Func<ISetupScript> createSetupScript )
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
                    var item = _sqlObjectParser.Create( _logger, text );
                    if( item != null )
                    {
                        if( item.FullName != f.FullNameWithoutContext )
                        {
                            _logger.Error( "File '{0}' in '{1}': content indicates '{2}'. Names must match.", f.FileName, f.ExtraPath, item.FullName );
                            return false;
                        }
                        if( !itemCollector.Add( item ) )
                        {
                            _logger.Error( "File '{0}' in '{1}' contains an already defined object.", f.FileName, f.ExtraPath );
                            return false;
                        }
                        _logger.CloseGroup( item.FullName );
                        return true;
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
                    DynamicPackageItem p = ReadPackageFileFormat( e );
                    _packages.Add( p );
                    _logger.CloseGroup( String.Format( "SetupPackage '{0}' found.", p.FullName ) );
                    return true;
                }
                catch( Exception ex )
                {
                    _logger.Error( ex );
                }
                return false;
            }
        }

        static public DynamicPackageItem ReadPackageFileFormat( XElement e )
        {
            DynamicPackageItem p;
            XElement model = e.Elements( "Model" ).SingleOrDefault();
            if( model != null )
            {
                p = new DynamicPackageItem( "SetupPWithModel" );
                p.EnsureModel().Requires.Clear();
                foreach( var a in model.Elements( "Requirements" ).Attributes( "Requires" ) ) p.Model.Requires.AddCommaSeparatedString( (string)a );
                p.Model.RequiredBy.Clear();
                foreach( var a in e.Elements( "Requirements" ).Attributes( "RequiredBy" ) ) p.Model.RequiredBy.AddCommaSeparatedString( (string)a );
            }
            else p = new DynamicPackageItem( "SetupP" );
            p.FullName = (string)e.AttributeRequired( "FullName" );
            p.SetVersionsString( (string)e.AttributeRequired( "Versions" ) );
            p.Requires.Clear();
            foreach( var a in e.Elements( "Requirements" ).Attributes( "Requires" ) ) p.Requires.AddCommaSeparatedString( (string)a );
            p.RequiredBy.Clear();
            foreach( var a in e.Elements( "Requirements" ).Attributes( "RequiredBy" ) ) p.RequiredBy.AddCommaSeparatedString( (string)a );

            p.Children.Clear();
            XElement content = e.Elements( "Content" ).SingleOrDefault();
            if( content != null )
            {
                foreach( var add in content.Elements( "Add" ) )
                {
                    p.Children.Add( new NamedDependentItemRef( (string)add.AttributeRequired( "FullName" ) ) );
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
