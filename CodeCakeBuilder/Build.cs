using System;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.MSBuild;
using Cake.Common.Tools.SignTool;
using Cake.Common.Tools.NUnit;
using Cake.Core;
using Cake.Core.Diagnostics;
using Code.Cake;
using SimpleGitVersion;
using Cake.Common.Tools.NuGet.Pack;
using System.Collections.Generic;
using Cake.Common.Tools.NuGet.Push;
using Cake.Common.Solution;
using System.Linq;
using System.IO;
using Cake.Core.IO;

namespace CodeCakeBuilder
{
    [AddPath( "%LOCALAPPDATA%/NuGet" )]
    [AddPath( "packages/**/tools*" )]
    public class Build : CodeCakeHost
    {
        public Build()
        {
            var configuration = Cake.Argument( "configuration", "Release" );
            var securePath = Cake.Argument( "securePath", "../../_Secure" );
            var secureDir = Cake.Directory( securePath );

            var nugetOutputDir = Cake.Directory( "CodeCakeBuilder/Release" );
            SimpleRepositoryInfo gitInfo = null;
            SignToolSignSettings signSettingsForRelease = null;

            var allProjects = Cake.ParseSolution( "CK-Database.sln" )
                                    .Projects
                                    .Where( p => p.Name != "CodeCakeBuilder" );

            var topProjects = allProjects.Where( d => d.Name == "CK.DB.Tests.NUnit" || d.Path.Segments.Length == Cake.Environment.WorkingDirectory.Segments.Length + 2 );

            var topProjectAssemblies = topProjects
                                           .Select( p => p.Path.GetDirectory() + "/bin/Release/" + p.Name )
                                           .SelectMany( p => new[] { p + ".dll", p + ".exe" } )
                                           .Where( p => Cake.FileExists( p ) );

            Task( "Clean" )
                .Does( () =>
                {
                    Cake.CleanDirectory( nugetOutputDir );
                    Cake.DeleteFiles( "CodeCakeBuilder/NuSpec/*.temp.nuspec" );
                } );

            Task( "Restore-NuGet-Packages" )
                .IsDependentOn( "Clean" )
                .Does( () =>
                {
                    Cake.NuGetRestore( "CK-Database.sln" );
                } );

            Task( "Build" )
                .IsDependentOn( "Check-Publish" )
                .IsDependentOn( "Restore-NuGet-Packages" )
                .Does( () =>
                {
                    using( var sln = Cake.CreateTemporarySolutionFile( "CK-Database.sln" ) )
                    {
                        sln.ExcludeProjectsFromBuild( "CodeCakeBuilder" );
                        Cake.MSBuild( sln.FullPath, new MSBuildSettings()
                            .UseToolVersion( MSBuildToolVersion.NET45 )
                            .SetVerbosity( Verbosity.Normal )
                            .SetConfiguration( configuration ) );
                    }
                } );

            Task( "Check-Publish" )
                .Does( () =>
                {
                    gitInfo = Cake.GetSimpleRepositoryInfo();
                    if( !gitInfo.IsValid ) throw new Exception( "SimpleGitVersionInfo: This solution is not ready for publishing." );
                    else if( !Cake.DirectoryExists( secureDir ) ) throw new Exception( String.Format( "SecurePath '{0}' not found.", secureDir ) );
                    else
                    {
                        // If the release is a not a CI build, we must sign the artifacts before packaging.
                        if( gitInfo.IsValidRelease )
                        {
                            if( configuration != "Release" ) throw new Exception( "A release version must be published in 'Release' configuration!" );
                            signSettingsForRelease = new SignToolSignSettings()
                            {
                                TimeStampUri = new Uri( "http://timestamp.verisign.com/scripts/timstamp.dll" ),
                                CertPath = secureDir + Cake.File( "Invenietis-Authenticode.pfx" ),
                                Password = System.IO.File.ReadAllText( secureDir + Cake.File( "Invenietis-Authenticode.p.txt" ) )
                            };
                        }
                        Cake.Log.Information( "Packages in version '{0}' can be published.", gitInfo.NuGetVersion );
                    }
                } );

            Task( "Sign-Authenticode" )
                .IsDependentOn( "Build" )
                .WithCriteria( () => signSettingsForRelease != null )
                .Does( () =>
                {
                    Cake.Sign( topProjectAssemblies, signSettingsForRelease );
                } );

            Task( "Unit-Testing" )
                .IsDependentOn( "Build" )
                .Does( () =>
                {
                    Cake.NUnit( "Tests/**/bin/" + configuration + "/*.Tests.dll" );
                } );

            Task( "Create-NuGet-Package" )
                .IsDependentOn( "Unit-Testing" )
                .IsDependentOn( "Check-Publish" )
                .IsDependentOn( "Sign-Authenticode" )
                .Does( () =>
                {
                    Cake.CreateDirectory( nugetOutputDir );
                    var settings = new NuGetPackSettings()
                    {
                        Version = gitInfo.NuGetVersion,
                        BasePath = Cake.Environment.WorkingDirectory,
                        OutputDirectory = nugetOutputDir
                    };
                    foreach( var nuspec in Cake.GetFiles( "CodeCakeBuilder/NuSpec/*.nuspec" ) )
                    {
                        Cake.NuGetPack( nuspec, settings );
                    }
                } );

            Task( "Publish-NuGet-Package" )
                .IsDependentOn( "Create-NuGet-Package" )
                .Does( () =>
                {
                    var settings = new NuGetPushSettings()
                    {
                        ApiKey = System.IO.File.ReadAllText( secureDir + Cake.File( "NuGet-Push-ApiKey.txt" ) ),
                        Verbosity = NuGetVerbosity.Detailed,
                        Source = "http://proget.app.invenietis.net/nuget/Default"
                    };
                    foreach( var f in Cake.GetFiles( nugetOutputDir.Path.FullPath + "/*.nupkg" ) )
                    {
                        Cake.NuGetPush( f, settings );
                    }
                } );

            Task( "Default" ).IsDependentOn( "Publish-NuGet-Package" );
        }
    }
}
