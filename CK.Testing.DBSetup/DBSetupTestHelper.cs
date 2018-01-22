using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CK.Core;
using CK.Testing.DBSetup;
using CK.Testing.SqlServer;
using CK.Text;
using CKSetup;

namespace CK.Testing
{
    /// <summary>
    /// Exposes standard implementation of <see cref="IDBSetupTestHelperCore"/>.
    /// </summary>
    public class DBSetupTestHelper : IDBSetupTestHelperCore
    {
        readonly ICKSetupTestHelper _ckSetup;
        readonly IStObjMapTestHelper _stObjMap;
        readonly ISqlServerTestHelper _sqlServer;
        bool _generateSourceFiles;

        internal DBSetupTestHelper( ITestHelperConfiguration config, ICKSetupTestHelper ckSetup, ISqlServerTestHelper sqlServer, IStObjMapTestHelper stObjMap )
        {
            _ckSetup = ckSetup;
            _stObjMap = stObjMap;
            _sqlServer = sqlServer;
            stObjMap.StObjMapLoading += OnStObjMapLoading;
            _generateSourceFiles = config.GetBoolean( "DBSetup/GenerateSourceFiles" ) ?? true;
        }

        void OnStObjMapLoading( object sender, EventArgs e )
        {
            var file = _stObjMap.BinFolder.AppendPart( _stObjMap.GeneratedAssemblyName + ".dll" );
            if( !System.IO.File.Exists( file ) )
            {
                _stObjMap.Monitor.Info( $"File '{file}' does not exist. Running DBSetup to create it." );
                DoRunDBSetup( null, false, false, false );
            }
        }

        bool IDBSetupTestHelperCore.GenerateSourceFiles { get => _generateSourceFiles; set => _generateSourceFiles = value; }

        CKSetupRunResult IDBSetupTestHelperCore.RunDBSetup( ISqlServerDatabaseOptions db, bool traceStObjGraphOrdering, bool traceSetupGraphOrdering, bool revertNames )
        {
            return DoRunDBSetup( db, traceStObjGraphOrdering, traceSetupGraphOrdering, revertNames );
        }

        CKSetupRunResult DoRunDBSetup( ISqlServerDatabaseOptions db, bool traceStObjGraphOrdering, bool traceSetupGraphOrdering, bool revertNames )
        {
            if( db == null ) db = _sqlServer.DefaultDatabaseOptions;
            using( _ckSetup.Monitor.OpenInfo( $"Running DBSetup on {db}." ) )
            {
                try
                {
                    var conf = new SetupConfiguration();
                    bool forceSetup = _ckSetup.CKSetup.DefaultForceSetup
                                        || _sqlServer.EnsureDatabase( db )
                                        || _ckSetup.CKSetup.DefaultBinPaths
                                                    .Select( p => p.AppendPart( _stObjMap.GeneratedAssemblyName + ".dll" ) )
                                                    .Any( p => !File.Exists( p ) );
                    conf.EngineAssemblyQualifiedName = "CK.Setup.StObjEngine, CK.StObj.Engine";
                    conf.Configuration = XElement.Parse( $@"
                        <StObjEngineConfiguration>
                            <TraceDependencySorterInput>{traceStObjGraphOrdering}</TraceDependencySorterInput>
                            <TraceDependencySorterOutput>{traceStObjGraphOrdering}</TraceDependencySorterOutput>
                            <RevertOrderingNames>{revertNames}</RevertOrderingNames>
                            <GenerateAppContextAssembly>true</GenerateAppContextAssembly>
                            <GenerateSourceFiles>{_generateSourceFiles}</GenerateSourceFiles>
                            <GeneratedAssemblyName>{_stObjMap.GeneratedAssemblyName}</GeneratedAssemblyName>
                            <Aspect Type=""CK.Setup.SetupableAspectConfiguration, CK.Setupable.Model"" >
                                <TraceDependencySorterInput>{traceSetupGraphOrdering}</TraceDependencySorterInput>
                                <TraceDependencySorterOutput>{traceSetupGraphOrdering}</TraceDependencySorterOutput>
                                <RevertOrderingNames>{revertNames}</RevertOrderingNames>
                            </Aspect>
                            <Aspect Type=""CK.Setup.SqlSetupAspectConfiguration, CK.SqlServer.Setup.Model"" >
                                <DefaultDatabaseConnectionString>{_sqlServer.GetConnectionString( db.DatabaseName )}</DefaultDatabaseConnectionString>
                                <GlobalResolution>false</GlobalResolution>
                                <IgnoreMissingDependencyIsError>true</IgnoreMissingDependencyIsError>
                            </Aspect>
                        </StObjEngineConfiguration>" );
                    var result = _ckSetup.CKSetup.Run( conf, forceSetup: forceSetup );
                    if( result != CKSetupRunResult.Failed )
                    {
                        string genDllName = _stObjMap.GeneratedAssemblyName + ".dll";
                        var firstGen = new NormalizedPath( conf.BinPaths[0] ).AppendPart( genDllName );
                        if( firstGen != _stObjMap.BinFolder.AppendPart( genDllName ) && File.Exists( firstGen ) )
                        {
                            _stObjMap.Monitor.Info( $"Copying generated '{genDllName}' from first BinPath ({conf.BinPaths[0]}) to bin folder." );
                            File.Copy( firstGen, Path.Combine( AppContext.BaseDirectory, genDllName ), true );
                        }
                    }
                    return result;
                }
                catch( Exception ex )
                {
                    _ckSetup.Monitor.Error( ex );
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IDBSetupTestHelper"/> default implementation.
        /// </summary>
        public static IDBSetupTestHelper TestHelper => TestHelperResolver.Default.Resolve<IDBSetupTestHelper>();

    }
}
