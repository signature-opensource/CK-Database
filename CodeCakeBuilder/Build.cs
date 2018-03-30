using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Text;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Common.Tools.DotNetCore.Restore;
using Cake.Common.Tools.MSBuild;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Common.Tools.NuGet.Push;
using Cake.Common.Tools.NuGet.Restore;
using Cake.Common.Tools.NUnit;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using CK.Text;
using Code.Cake;
using SimpleGitVersion;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CodeCake
{

    class ComponentProjects
    {
        public ComponentProjects( string configuration )
        {
            ComponentProjectPaths = new []
            {
                GetNet461BinFolder( "CK.StObj.Model", configuration ),
                GetNet461BinFolder( "CK.StObj.Runtime", configuration ),
                GetNet461BinFolder( "CK.StObj.Engine", configuration ),
                GetNet461BinFolder( "CK.Setupable.Model", configuration ),
                GetNet461BinFolder( "CK.Setupable.Runtime", configuration ),
                GetNet461BinFolder( "CK.Setupable.Engine", configuration ),
                GetNet461BinFolder( "CK.SqlServer.Setup.Model", configuration ),
                GetNet461BinFolder( "CK.SqlServer.Setup.Runtime", configuration ),
                GetNet461BinFolder( "CK.SqlServer.Setup.Engine", configuration ),

                GetNetCoreBinFolder( "CK.StObj.Model", configuration ),
                GetNetCoreBinFolder( "CK.StObj.Runtime", configuration ),
                GetNetCoreBinFolder( "CK.StObj.Engine", configuration ) ,
                GetNetCoreBinFolder( "CK.Setupable.Model", configuration ),
                GetNetCoreBinFolder( "CK.Setupable.Runtime", configuration ),
                GetNetCoreBinFolder( "CK.Setupable.Engine", configuration ),
                GetNetCoreBinFolder( "CK.SqlServer.Setup.Model", configuration ),
                GetNetCoreBinFolder( "CK.SqlServer.Setup.Runtime", configuration ),
                GetNetCoreBinFolder( "CK.SqlServer.Setup.Engine", configuration )
            };
        }

        public IReadOnlyList<NormalizedPath> ComponentProjectPaths { get; }

        static NormalizedPath GetNet461BinFolder( string name, string configuration )
        {
            return System.IO.Path.GetFullPath( name + "/bin/" + configuration + "/net461" );
        }

        static NormalizedPath GetNetCoreBinFolder( string name, string configuration )
        {
            string pathToFramework = System.IO.Path.GetFullPath( name + "/bin/" + configuration + "/netstandard2.0" );
            if( !Directory.Exists( pathToFramework ) )
            {
                pathToFramework = System.IO.Path.GetFullPath( name + "/bin/" + configuration + "/netcoreapp2.0" );
            }
            return pathToFramework;
        }
    }


    [AddPath( "%UserProfile%/.nuget/packages/**/tools*" )]
    public partial class Build : CodeCakeHost
    {

        public Build()
        {
            Cake.Log.Verbosity = Verbosity.Diagnostic;

            const string solutionName = "CK-Database";
            const string solutionFileName = solutionName + ".sln";

            var releasesDir = Cake.Directory( "CodeCakeBuilder/Releases" );
            var projects = Cake.ParseSolution( solutionFileName )
                                       .Projects
                                       .Where( p => !(p is SolutionFolder)
                                                    && p.Name != "CodeCakeBuilder" );

            // We do not generate NuGet packages for .Tests projects for this solution.
            var projectsToPublish = projects
                                        .Where( p => !p.Path.Segments.Contains( "Tests" ) );

            // Initialized by Build: the netstandard2.0/netcoreapp2.0 directory must exist
            // since we rely on them to find the target...
            ComponentProjects componentProjects = null;

            SimpleRepositoryInfo gitInfo = Cake.GetSimpleRepositoryInfo();

            // Configuration is either "Debug" or "Release".
            string configuration = "Debug";

            Task( "Check-Repository" )
                .Does( () =>
                 {
                     configuration = StandardCheckRepository( projectsToPublish, gitInfo );
                 } );

            Task( "Clean" )
                .IsDependentOn( "Check-Repository" )
                .Does( () =>
                 {
                     Cake.CleanDirectories( projects.Select( p => p.Path.GetDirectory().Combine( "bin" ) ) );
                     Cake.CleanDirectories( releasesDir );
                     Cake.DeleteFiles( "Tests/**/TestResult*.xml" );
                 } );

            Task( "Build" )
                .IsDependentOn( "Check-Repository" )
                .IsDependentOn( "Clean" )
                .Does( () =>
                 {
                     StandardSolutionBuild( solutionFileName, gitInfo, configuration );
                     componentProjects = new ComponentProjects( configuration );
                     foreach( var pub in componentProjects.ComponentProjectPaths.Where( p => p.EndsWith( "netcoreapp2.0" ) ) )
                     {
                         Cake.DotNetCorePublish( pub.RemoveLastPart().RemoveLastPart().RemoveLastPart(),
                            new DotNetCorePublishSettings().AddVersionArguments( gitInfo, s =>
                            {
                                s.Framework = "netcoreapp2.0";
                                s.Configuration = configuration;
                            } ) );
                     }
                 } );

            Task( "Unit-Testing" )
                .IsDependentOn( "Build" )
                .WithCriteria( () => Cake.InteractiveMode() == InteractiveMode.NoInteraction
                                     || Cake.ReadInteractiveOption( "Run Unit Tests?", 'Y', 'N' ) == 'Y' )
                .Does( () =>
                 {
                     StandardUnitTests( configuration, projects.Where( p => p.Name.EndsWith( ".Tests" ) ) );
                 } );

            Task( "Create-All-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .IsDependentOn( "Unit-Testing" )
                .Does( () =>
                 {
                     StandardCreateNuGetPackages( releasesDir, projectsToPublish, gitInfo, configuration );
                 } );

            Task( "Push-Runtimes-and-Engines" )
                .IsDependentOn( "Unit-Testing" )
                .WithCriteria( () => gitInfo.IsValid )
                .Does( () =>
                {
                    var apiKey = Cake.InteractiveEnvironmentVariable( "CKSETUPREMOTESTORE_PUSH_API_KEY" );
                    if( !String.IsNullOrWhiteSpace( apiKey ) )
                    {
                        if( !Cake.CKSetupPublishAndAddComponentFoldersToStore( componentProjects.ComponentProjectPaths.Select( x => x.ToString() ) ) )
                        {
                            Cake.TerminateWithError( "Error while registering components." );
                        }
                        if( !Cake.CKSetupPushLocalStoreToRemote( apiKey ) )
                        {
                            Cake.TerminateWithError( "Error while pushing components to remote store." );
                        }
                    }
                    else Cake.Information( "Skipped push to http:/cksetup.invenietis.net." );
                } );

            Task( "Push-NuGet-Packages" )
                .IsDependentOn( "Create-All-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .Does( () =>
                 {
                     IEnumerable<FilePath> nugetPackages = Cake.GetFiles( releasesDir.Path + "/*.nupkg" );
                     StandardPushNuGetPackages( nugetPackages, gitInfo );
                 } );

            // The Default task for this script can be set here.
            Task( "Default" )
                .IsDependentOn( "Push-NuGet-Packages" )
                .IsDependentOn( "Push-Runtimes-and-Engines" );
        }

    }
}
