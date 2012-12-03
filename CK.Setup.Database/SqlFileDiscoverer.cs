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
        /// <param name="curContext">Current context identifier. It will be used as the default. Null if no current context exist.</param>
        /// <param name="curLoc">Current location identifier. It will be used as the default location. Null if no current location exist.</param>
        /// <param name="directoryPath">Root path to start.</param>
        /// <returns>True on success. Any warn or error are logged in the <see cref="IActivityLogger"/> that has been provided to the constructor.</returns>
        public bool DiscoverPackages( string curContext, string curLoc, string directoryPath )
        {
            using( _logger.OpenGroup( LogLevel.Info, "Discovering *.ck package files in '{0}'.", directoryPath ) )
            {
                CheckDirectoryPath( directoryPath );
                return DoDiscoverPackages( new DirectoryInfo( directoryPath ), curContext, curLoc );
            }
        }

        bool DoDiscoverPackages( DirectoryInfo d, string curContext, string curLoc )
        {
            string context, loc, name;
            if( DefaultContextLocNaming.TryParse( d.Name, out context, out loc, out name ) )
            {
                if( context != null ) curContext = context;
                if( loc != null ) curLoc = loc;
            }
            bool result = true;
            foreach( var file in d.EnumerateFiles( "*.ck", SearchOption.TopDirectoryOnly ) )
            {
                if( !DoRegisterPackage( file, curContext, curLoc ) )
                {
                    _packageDiscoverErrorCount++;
                    result = false;
                }
            }
            foreach( var dir in d.EnumerateDirectories() )
            {
                result &= DoDiscoverPackages( dir, curContext, curLoc );
            }
            return result;
        }


        bool DoRegisterPackage( FileInfo file, string curContext, string curLoc )
        {
            using( _logger.OpenGroup( LogLevel.Trace, "Registering '{0}'.", file.Name ) )
            {
                XDocument doc = XDocument.Load( file.FullName );
                XElement e = doc.Root;
                if( e.Name != "SetupPackage" )
                {
                    _logger.Warn( "The root element must be 'SetupPackage'." );
                    return false;
                }
                try
                {
                    DynamicPackageItem p = ReadPackageFileFormat( e, curContext, curLoc );
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

        static public DynamicPackageItem ReadPackageFileFormat( XElement e, string curContext, string curLoc )
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

            p.FullName = DefaultContextLocNaming.Resolve( (string)e.AttributeRequired( "FullName" ), curContext, curLoc );

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

        #region Sql Files
		/// <summary>
        /// Discovers *.sql files recursively in a directory and, depending on their type, either registers them in the script <paramref name="collector"/>
        /// or consider them as <see cref="IDependentProtoItem"/> and collects them in <paramref name="itemCollector"/>.
        /// </summary>
        /// <param name="curContext">Current context identifier. It will be used as the default. Null if no current context exist.</param>
        /// <param name="curLoc">Current location identifier. It will be used as the default location. Null if no current location exist.</param>
        /// <param name="directoryPath">Root path to start.</param>
        /// <param name="itemCollector">Collector for discovered items.</param>
        /// <param name="collector">Optional scripts collector.</param>
        /// <param name="sqlFileScriptSource">The <see cref="ScriptSource.Name"/> for the <paramref name="collector"/>.
        /// It must have been registered as a source in a <see cref="ScriptTypeHandler"/>, itself registered in the <see cref="ScriptTypeManager"/> associated to the collector.
        /// </param>
        /// <returns>True on success. Any warn or error are logged in the <see cref="IActivityLogger"/> that has been provided to the constructor.</returns>
        public bool DiscoverSqlFiles( string curContext, string curLoc, string directoryPath, DependentProtoItemCollector itemCollector, ScriptCollector collector = null, string sqlFileScriptSource = DefaultSourceName )
        {
            using( _logger.OpenGroup( LogLevel.Info, "Discovering Sql files in '{0}' for source '{1}'.", directoryPath, sqlFileScriptSource ) )
            {
                CheckDirectoryPath( directoryPath );
                if( String.IsNullOrWhiteSpace( sqlFileScriptSource ) ) throw new ArgumentException( "Must not be null, empty or white space.", "sqlFileScriptSource" );
                return DoDiscoverSqlFiles( new DirectoryInfo( directoryPath ), curContext, curLoc, itemCollector, collector, sqlFileScriptSource );
            }
        }

        bool DoDiscoverSqlFiles( DirectoryInfo d, string curContext, string curLoc, DependentProtoItemCollector itemCollector, ScriptCollector collector, string sqlFileScriptSource )
        {
            string context, loc, name;
            if( DefaultContextLocNaming.TryParse( d.Name, out context, out loc, out name ) )
            {
                if( context != null ) curContext = context;
                if( loc != null ) curLoc = loc;
            }
            bool result = true;
            foreach( var file in d.EnumerateFiles( "*.sql", SearchOption.TopDirectoryOnly ) )
            {
                ParsedFileName f;
                if( !ParsedFileName.TryParse( curContext, curLoc, file.Name, file.DirectoryName, true, out f )
                    || !DoRegisterSql( f, itemCollector, collector, () => File.ReadAllText( file.FullName ), () => new FileSetupScript( f, sqlFileScriptSource ) ) )
                {
                    _sqlFileDiscoverErrorCount++;
                    result = false;
                }
            }
            foreach( var dir in d.EnumerateDirectories() )
            {
                result &= DoDiscoverSqlFiles( dir, curContext, curLoc, itemCollector, collector, sqlFileScriptSource );
            }
            return result;
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
                    var item = _sqlObjectParser.Create( _logger, f, readContent() );
                    if( item != null )
                    {
                        // Checking FullName is not perfect here: if the object content defines a Context or a Location, its FullName may
                        // differ from the external one of the ParsedFileName.
                        // It is best to compare only Name part of the two names.
                        if( item.FullName != f.FullName )
                        {
                            _logger.Error( "File '{0}' in '{1}': content indicates '{2}'. Names must match.", f.FileName, f.ExtraPath, item.FullName );
                            return false;
                        }
                        if( !itemCollector.Add( item ) )
                        {
                            _logger.Error( "File '{0}' in '{1}': object '{2}' is already defined.", f.FileName, f.ExtraPath, item.FullName );
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

	    #endregion Sql Files
        
        private static void CheckDirectoryPath( string directoryPath )
        {
            if( directoryPath == null ) throw new ArgumentNullException( directoryPath );
            if( !Path.IsPathRooted( directoryPath ) ) throw new ArgumentException( "Path must be rooted.", directoryPath );
        }


    }
}
