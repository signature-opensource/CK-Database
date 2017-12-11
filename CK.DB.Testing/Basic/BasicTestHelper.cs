using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CK.Core;
using CK.Text;

namespace CK.Testing
{

    /// <summary>
    /// Provides default implementation of <see cref="IBasicTestHelper"/>.
    /// </summary>
    public class BasicTestHelper : IBasicTestHelper
    {
        static readonly string[] _allowedConfigurations = new[] { "Debug", "Release" };
        NormalizedPath _binFolder;
        string _buildConfiguration;
        NormalizedPath _testProjectFolder;
        NormalizedPath _testProjectName;
        NormalizedPath _repositoryFolder;
        NormalizedPath _solutionFolder;
        NormalizedPath _logFolder;

        public string BuildConfiguration => Initalize();

        public NormalizedPath RepositoryFolder => InitalizePaths( ref _repositoryFolder );

        public NormalizedPath SolutionFolder => InitalizePaths( ref _solutionFolder );

        public NormalizedPath LogFolder => InitalizePaths( ref _logFolder );

        public NormalizedPath TestProjectFolder => InitalizePaths( ref _testProjectFolder );

        public NormalizedPath TestProjectName => InitalizePaths( ref _testProjectName );

        public NormalizedPath BinFolder => InitalizePaths( ref _binFolder );

        NormalizedPath InitalizePaths( ref NormalizedPath varPath )
        {
            Initalize();
            return varPath;
        }

        string Initalize()
        {
            if( _buildConfiguration != null ) return _buildConfiguration;
            string p = _binFolder = AppContext.BaseDirectory;
            string buildConfDir = null;
            foreach( var config in _allowedConfigurations )
            {
                buildConfDir = FindAbove( p, config );
                if( buildConfDir != null )
                {
                    _buildConfiguration = config;
                    break;
                }
            }
            if( _buildConfiguration == null )
            {
                throw new InvalidOperationException( $"Unable to find parent folder named '{_allowedConfigurations.Concatenate("' or '")}' above '{_binFolder}'." );
            }
            p = Path.GetDirectoryName( buildConfDir );
            if( Path.GetFileName( p ) != "bin" )
            {
                throw new InvalidOperationException( $"Folder '{_buildConfiguration}' MUST be in 'bin' folder (above '{_binFolder}')." );
            }
            _testProjectFolder = p = Path.GetDirectoryName( p );
            _testProjectName = Path.GetFileName( p );
            Assembly entry = Assembly.GetEntryAssembly();
            if( entry != null )
            {
                string assemblyName = entry.GetName().Name;
                if( _testProjectName != assemblyName )
                {
                    throw new InvalidOperationException( $"Current test project assembly is '{assemblyName}' but folder is '{_testProjectName}' (above '{_buildConfiguration}' in '{_binFolder}')." );
                }
            }
            p = Path.GetDirectoryName( p );

            string testsFolder = null;
            bool hasGit = false;
            while( p != null && !(hasGit = Directory.Exists( Path.Combine( p, ".git" ) )) )
            {
                if( Path.GetFileName( p ) == "Tests" ) testsFolder = p;
                p = Path.GetDirectoryName( p );
            }
            if( !hasGit ) throw new InvalidOperationException( $"The project must be in a git repository (above '{_binFolder}')." );
            _repositoryFolder = p;
            if( testsFolder == null )
            {
                throw new InvalidOperationException( $"A parent 'Tests' folder must exist above '{_testProjectFolder}'." );
            }
            _solutionFolder = Path.GetDirectoryName( testsFolder );
            _logFolder = Path.Combine( testsFolder, "Logs" );
            return _buildConfiguration;
        }

        static string FindAbove( string path, string folderName )
        {
            while( path != null && Path.GetFileName( path ) != folderName )
            {
                path = Path.GetDirectoryName( path );
            }
            return path;
        }

        /// <summary>
        /// Gets the <see cref="IBasicTestHelper"/> default implementation.
        /// </summary>
        public static IBasicTestHelper TestHelper { get; } = TestHelperResolver.Default.Resolve<IBasicTestHelper>();
    }
}
