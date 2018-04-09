using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CK.Core;
using CK.Testing.StObjSetup;
using CK.Text;
using CKSetup;

namespace CK.Testing
{
    /// <summary>
    /// Exposes standard implementation of <see cref="IStObjSetupTestHelperCore"/>.
    /// </summary>
    public class StObjSetupTestHelper : IStObjSetupTestHelperCore
    {
        readonly ICKSetupTestHelper _ckSetup;
        readonly IStObjMapTestHelper _stObjMap;
        EventHandler<StObjSetupRunningEventArgs> _stObjSetupRunning;
        bool _generateSourceFiles;
        bool _revertOrderingNames;
        bool _traceGraphOrdering;

        internal StObjSetupTestHelper( ITestHelperConfiguration config, ICKSetupTestHelper ckSetup, IStObjMapTestHelper stObjMap )
        {
            _ckSetup = ckSetup;
            _stObjMap = stObjMap;
            stObjMap.StObjMapLoading += OnStObjMapLoading;

            var oldConfig = config.GetConfigValue( "DBSetup/GenerateSourceFiles" );
            if( oldConfig.HasValue ) throw new Exception( $"Configuration DBSetup/GenerateSourceFiles entry in '{oldConfig.Value.BasePath}' must be updated to StObjSetup/StObjGenerateSourceFiles."  );

            _generateSourceFiles = config.GetBoolean( "StObjSetup/StObjGenerateSourceFiles" ) ?? true;
            _revertOrderingNames = config.GetBoolean( "StObjSetup/StObjRevertOrderingNames" ) ?? false;
            _traceGraphOrdering = config.GetBoolean( "StObjSetup/StObjTraceGraphOrdering" ) ?? false;
        }

        void OnStObjMapLoading( object sender, EventArgs e )
        {
            var file = _stObjMap.BinFolder.AppendPart( _stObjMap.GeneratedAssemblyName + ".dll" );
            if( !System.IO.File.Exists( file ) )
            {
                _stObjMap.Monitor.Info( $"File '{file}' does not exist. Running StObjSetup to create it." );

                bool forceSetup = _ckSetup.CKSetup.DefaultForceSetup
                                    || _ckSetup.CKSetup.FinalDefaultBinPaths
                                            .Select( p => p.AppendPart( _stObjMap.GeneratedAssemblyName + ".dll" ) )
                                            .Any( p => !File.Exists( p ) );

                var stObjConf = new StObjEngineConfiguration();
                stObjConf.GenerateSourceFiles = _generateSourceFiles;
                stObjConf.RevertOrderingNames = _revertOrderingNames;
                stObjConf.TraceDependencySorterInput = _traceGraphOrdering;
                stObjConf.TraceDependencySorterOutput = _traceGraphOrdering;
                stObjConf.GeneratedAssemblyName = _stObjMap.GeneratedAssemblyName;
                stObjConf.AppContextAssemblyGeneratedDirectoryTarget = _stObjMap.BinFolder;
                DoRunStObjSetup( stObjConf, forceSetup );
            }
        }

        CKSetupRunResult DoRunStObjSetup( StObjEngineConfiguration stObjConf, bool forceSetup )
        {
            if( stObjConf == null ) throw new ArgumentNullException( nameof( stObjConf ) );
            using( _ckSetup.Monitor.OpenInfo( $"Running StObjSetup." ) )
            {
                try
                {
                    var ev = new StObjSetupRunningEventArgs( stObjConf, forceSetup );
                    _stObjSetupRunning?.Invoke( this, ev );

                    var ckSetupConf = new SetupConfiguration();
                    ckSetupConf.EngineAssemblyQualifiedName = "CK.Setup.StObjEngine, CK.StObj.Engine";
                    stObjConf.SerializeXml( ckSetupConf.Configuration );
                    var result = _ckSetup.CKSetup.Run( ckSetupConf, forceSetup: ev.ForceSetup );
                    if( result != CKSetupRunResult.Failed )
                    {
                        string genDllName = _stObjMap.GeneratedAssemblyName + ".dll";
                        var firstGen = new NormalizedPath( ckSetupConf.BinPaths[0] ).AppendPart( genDllName );
                        if( firstGen != _stObjMap.BinFolder.AppendPart( genDllName ) && File.Exists( firstGen ) )
                        {
                            if( ckSetupConf.BinPaths.Count > 1 && !stObjConf.ForceAppContextAssemblyGeneration )
                            {
                                _stObjMap.Monitor.Warn( $"ForceAppContextAssemblyGeneration is false but setup is based on multiple bin paths. Copying the first generated '{genDllName}' from BinPaths ({ckSetupConf.BinPaths[0]}) to bin folder. This may not work." );
                            }
                            else _stObjMap.Monitor.Info( $"Copying generated '{genDllName}' from first BinPath ({ckSetupConf.BinPaths[0]}) to bin folder." );
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

        bool IStObjSetupTestHelperCore.StObjGenerateSourceFiles { get => _generateSourceFiles; set => _generateSourceFiles = value; }

        bool IStObjSetupTestHelperCore.StObjRevertOrderingNames { get => _revertOrderingNames; set => _revertOrderingNames = value; }

        bool IStObjSetupTestHelperCore.StObjTraceGraphOrdering { get => _traceGraphOrdering; set => _traceGraphOrdering = value; }

        event EventHandler<StObjSetupRunningEventArgs> IStObjSetupTestHelperCore.StObjSetupRunning
        {
            add => _stObjSetupRunning += value;
            remove => _stObjSetupRunning -= value;
        }

        CKSetupRunResult IStObjSetupTestHelperCore.RunStObjSetup( StObjEngineConfiguration configuration, bool forceSetup ) => DoRunStObjSetup( configuration, forceSetup );

        /// <summary>
        /// Gets the <see cref="IStObjSetupTestHelperCore"/> default implementation.
        /// </summary>
        public static IStObjSetupTestHelperCore TestHelper => TestHelperResolver.Default.Resolve<IStObjSetupTestHelperCore>();

    }
}
