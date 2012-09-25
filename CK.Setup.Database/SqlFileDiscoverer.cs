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
        /// <summary>
        /// This is the default <see cref="ScriptSource.Name"/> used by <see cref="DiscoverSqlFiles"/>.
        /// </summary>
        public const string DefaultSourceName = "file-sql";

        ISqlObjectBuilder _sqlObjectBuilder;
        IActivityLogger _logger;
        List<DynamicPackageItem> _packages;
        List<IVersionedItem> _sqlObjects;

        int _packageDiscoverErrorCount;
        int _sqlFileDiscoverErrorCount;

        public SqlFileDiscoverer( ISqlObjectBuilder sqlObjectBuilder, IActivityLogger logger )
        {
            if( sqlObjectBuilder == null ) throw new ArgumentNullException( "sqlObjectBuilder" );
            if( logger == null ) throw new ArgumentNullException( "logger" );
 
            _sqlObjectBuilder = sqlObjectBuilder;
            _logger = logger;

            _packages = new List<DynamicPackageItem>();
            _sqlObjects = new List<IVersionedItem>();
        }

        public int PackageDiscoverErrorCount
        {
            get { return _packageDiscoverErrorCount; }
        }

        public int SqlFileDiscoverErrorCount
        {
            get { return _sqlFileDiscoverErrorCount; }
        }

        /// <summary>
        /// Discovers *.ck files recursively in a directory and collects them internally as <see cref="DynamicPackageItem"/>.
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
        /// or consider them as <see cref="IVersionedItem"/> and collects them internally.
        /// </summary>
        /// <param name="directoryPath">Root path to start.</param>
        /// <param name="collector">Optional scripts collector.</param>
        /// <param name="sqlFileScriptSource">The <see cref="ScriptSource.Name"/> for the <paramref name="collector"/>.
        /// It must have been registered as a source in a <see cref="ScriptTypeHandler"/>, itself registered in the <see cref="ScriptTypeManager"/> associated to the collector.
        /// </param>
        /// <returns>True on success. Any warn or error are logged in the <see cref="IActivityLogger"/> that has been provided to the constructor.</returns>
        public bool DiscoverSqlFiles( string directoryPath, ScriptCollector collector = null, string sqlFileScriptSource = DefaultSourceName )
        {
            using( _logger.OpenGroup( LogLevel.Info, "Discovering Sql files in '{0}' for source '{1}'.", directoryPath, sqlFileScriptSource ) )
            {
                CheckDirectoryPath( directoryPath );
                if( String.IsNullOrWhiteSpace( sqlFileScriptSource ) ) throw new ArgumentException( "Must not be null, empty or white space.", "sqlFileScriptSource" );

                bool result = true;
                foreach( var path in Directory.EnumerateFiles( directoryPath, "*.sql", SearchOption.AllDirectories ) )
                {
                    if( !RegisterFileSql( path, collector, sqlFileScriptSource ) )
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

        bool RegisterFileSql( string path, ScriptCollector collector, string sqlFileScriptSource )
        {
            ParsedFileName f;
            if( !ParsedFileName.TryParse( Path.GetFileName( path ), Path.GetDirectoryName( path ), true, out f ) ) return false;
            return DoRegisterSql( f, collector, () => File.ReadAllText( path ), () => new FileSetupScript( f, sqlFileScriptSource ) );
        }

        bool DoRegisterSql( ParsedFileName f, ScriptCollector collector, Func<string> readContent, Func<ISetupScript> createSetupScript )
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
                    IVersionedItem item = _sqlObjectBuilder.Create( _logger, text );
                    if( item != null )
                    {
                        if( item.FullName != f.FullNameWithoutContext )
                        {
                            _logger.Error( "Name from the file is '{0}' whereas content indicates '{1}'. Names must match.", f.FullName, item.FullName );
                            return false;
                        }
                        _logger.CloseGroup( item.FullName );
                        _sqlObjects.Add( item );
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
