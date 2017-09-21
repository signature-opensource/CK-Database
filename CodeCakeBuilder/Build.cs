using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Text;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Pack;
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
    [AddPath("CodeCakeBuilder/Tools")]
    [AddPath("packages/**/tools*")]
    public class Build : CodeCakeHost
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

            // We do not publish .Tests projects for this solution.
            var projectsToPublish = projects
                                        .Where( p => !p.Path.Segments.Contains( "Tests" ) );

            SimpleRepositoryInfo gitInfo = Cake.GetSimpleRepositoryInfo();

            // Configuration is either "Debug" or "Release".
            string configuration = "Debug";

            bool buildDone = false;
            Teardown( c =>
            {
                var mustStop = Process.GetProcessesByName( "CKSetupRemoteStore" );
                foreach( var p in mustStop ) p.Kill();
                if( buildDone ) c.CleanDirectories( projects.Select( p => p.Path.GetDirectory().Combine( "bin" ) ) );
            } );

            Task( "Check-Repository" )
                .Does( () =>
                 {
                     if( !gitInfo.IsValid )
                     {
                         if( Cake.IsInteractiveMode()
                             && Cake.ReadInteractiveOption( "Repository is not ready to be published. Proceed anyway?", 'Y', 'N' ) == 'Y' )
                         {
                             Cake.Warning( "GitInfo is not valid, but you choose to continue..." );
                         }
                         else if( !Cake.AppVeyor().IsRunningOnAppVeyor ) throw new Exception( "Repository is not ready to be published." );
                     }

                     configuration = gitInfo.IsValidRelease
                                     && (gitInfo.PreReleaseName.Length == 0 || gitInfo.PreReleaseName == "rc")
                                     ? "Release"
                                     : "Debug";

                     Cake.Information( "Publishing {0} projects with version={1} and configuration={2}: {3}",
                         projectsToPublish.Count(),
                         gitInfo.SafeSemVersion,
                         configuration,
                         string.Join( ", ", projectsToPublish.Select( p => p.Name ) ) );
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
                     buildDone = true;
                     Cake.DotNetCoreBuild( "CodeCakeBuilder/CoreBuild.proj",
                         new DotNetCoreBuildSettings().AddVersionArguments( gitInfo, s =>
                         {
                             s.Configuration = configuration;
                         } ) );
                 } );

            Task( "Unit-Testing" )
                .IsDependentOn( "Build" )
                .WithCriteria( () => !Cake.IsInteractiveMode()
                                     || Cake.ReadInteractiveOption( "Run Unit Tests?", 'Y', 'N' ) == 'Y' )
                .Does( () =>
                 {
                     Cake.CreateDirectory( releasesDir );
                     var testDlls = projects.Where( p => p.Name.EndsWith( ".Tests" ) ).Select( p =>
                                 new
                                 {
                                     ProjectPath = p.Path.GetDirectory(),
                                     NetCoreAppDll = p.Path.GetDirectory().CombineWithFilePath( "bin/" + configuration + "/netcoreapp2.0/" + p.Name + ".dll" ),
                                     Net461Dll = p.Path.GetDirectory().CombineWithFilePath( "bin/" + configuration + "/net461/" + p.Name + ".dll" ),
                                 } );

                     foreach( var test in testDlls )
                     {
                         using( Cake.Environment.SetWorkingDirectory( test.ProjectPath ) )
                         {
                             Cake.Information( "Testing: {0}", test.Net461Dll );
                             Cake.NUnit( test.Net461Dll.FullPath, new NUnitSettings()
                             {
                                 Framework = "v4.5"
                             } );
                             if( System.IO.File.Exists( test.NetCoreAppDll.FullPath ) )
                             {
                                 Cake.Information( "Testing: {0}", test.NetCoreAppDll );
                                 Cake.DotNetCoreExecute( test.NetCoreAppDll );
                             }
                         }
                     }
                 } );

            Task( "Create-All-NuGet-Packages" )
                .WithCriteria( () => gitInfo.IsValid )
                .IsDependentOn( "Unit-Testing" )
                .Does( () =>
                 {
                     Cake.CreateDirectory( releasesDir );
                     var settings = new DotNetCorePackSettings();
                     settings.ArgumentCustomization = args => args.Append( "--include-symbols" );
                     // --no-build DOES NOT WORK :(
                     // https://github.com/dotnet/cli/issues/5331
                     //settings.NoBuild = true;
                     settings.Configuration = configuration;
                     settings.OutputDirectory = releasesDir;
                     settings.AddVersionArguments( gitInfo );
                     Cake.DotNetCorePack( "CodeCakeBuilder/CoreBuild.proj", settings );
                 } );

            Task( "Push-Runtimes-and-Engines" )
                .IsDependentOn( "Unit-Testing" )
                .WithCriteria( () => gitInfo.IsValid )
                .Does( () =>
                {
                    var apiKey = Cake.InteractiveEnvironmentVariable( "CKSETUPREMOTESTORE_PUSH_API_KEY" );
                    if( !String.IsNullOrWhiteSpace( apiKey ) )
                    {
                        string ckSetupExe = "CKSetup/bin/" + configuration + "/net461/CKSetup.exe";
                        string storePath = releasesDir.Path.Combine( "TempStore" ).FullPath;
                        var args = new ProcessArgumentBuilder()
                                    .Append( "store" )
                                    .Append( "add" );

                        args.Append( GetNet461BinFolder( "CK.StObj.Runner", configuration ) );
                        args.Append( GetNet461BinFolder( "CK.StObj.Model", configuration ) );
                        args.Append( GetNet461BinFolder( "CK.StObj.Runtime", configuration ) );
                        args.Append( GetNet461BinFolder( "CK.StObj.Engine", configuration ) );
                        args.Append( GetNet461BinFolder( "CK.Setupable.Model", configuration ) );
                        args.Append( GetNet461BinFolder( "CK.Setupable.Runtime", configuration ) );
                        args.Append( GetNet461BinFolder( "CK.Setupable.Engine", configuration ) );
                        args.Append( GetNet461BinFolder( "CK.SqlServer.Setup.Model", configuration ) );
                        args.Append( GetNet461BinFolder( "CK.SqlServer.Setup.Runtime", configuration ) );
                        args.Append( GetNet461BinFolder( "CK.SqlServer.Setup.Engine", configuration ) );

                        args.Append( EnsurePublishPath( GetNetCoreBinFolder( "CK.StObj.Runner", configuration ) ) );
                        args.Append( GetNetCoreBinFolder( "CK.StObj.Model", configuration ) );
                        args.Append( EnsurePublishPath( GetNetCoreBinFolder( "CK.StObj.Runtime", configuration ) ) );
                        args.Append( EnsurePublishPath( GetNetCoreBinFolder( "CK.StObj.Engine", configuration ) ) );
                        args.Append( GetNetCoreBinFolder( "CK.Setupable.Model", configuration ) );
                        args.Append( EnsurePublishPath( GetNetCoreBinFolder( "CK.Setupable.Runtime", configuration ) ) );
                        args.Append( EnsurePublishPath( GetNetCoreBinFolder( "CK.Setupable.Engine", configuration ) ) );
                        args.Append( GetNetCoreBinFolder( "CK.SqlServer.Setup.Model", configuration ) );
                        args.Append( EnsurePublishPath( GetNetCoreBinFolder( "CK.SqlServer.Setup.Runtime", configuration ) ) );
                        args.Append( EnsurePublishPath( GetNetCoreBinFolder( "CK.SqlServer.Setup.Engine", configuration ) ) );

                        args.AppendSwitchQuoted( "--store", storePath )
                            .AppendSwitchQuoted( "-f", "Debug" );

                        Cake.ProcessRunner.Start( ckSetupExe, new ProcessSettings()
                        {
                            Arguments = args
                        } ).WaitForExit();
                        args.Clear();
                        args.Append( "store" )
                            .Append( "push" )
                            .AppendSwitch( "-u", "http://cksetup.invenietis.net" )
                            .AppendSwitchSecret( "-k", apiKey )
                            .AppendSwitchQuoted( "--store", storePath )
                            .AppendSwitchQuoted( "-f", "Debug" );
                        Cake.ProcessRunner.Start( ckSetupExe, new ProcessSettings()
                        {
                            Arguments = args
                        } ).WaitForExit();
                    }
                    else Cake.Information( "Skipped push to http:/cksetup.invenietis.net." );
                } );

            Task( "Push-NuGet-Packages" )
                .IsDependentOn( "Create-All-NuGet-Packages" )
                .IsDependentOn( "Push-Runtimes-and-Engines" )
                .WithCriteria( () => gitInfo.IsValid )
                .Does( () =>
                 {
                     IEnumerable<FilePath> nugetPackages = Cake.GetFiles( releasesDir.Path + "/*.nupkg" );
                     if( Cake.IsInteractiveMode() )
                     {
                         var localFeed = Cake.FindDirectoryAbove( "LocalFeed" );
                         if( localFeed != null )
                         {
                             Cake.Information( "LocalFeed directory found: {0}", localFeed );
                             if( Cake.ReadInteractiveOption( "Do you want to publish to LocalFeed?", 'Y', 'N' ) == 'Y' )
                             {
                                 Cake.CopyFiles( nugetPackages, localFeed );
                             }
                         }
                     }
                     if( gitInfo.IsValidRelease )
                     {
                         if( gitInfo.PreReleaseName == ""
                             || gitInfo.PreReleaseName == "prerelease"
                             || gitInfo.PreReleaseName == "rc" )
                         {
                             PushNuGetPackages( "NUGET_API_KEY", "https://www.nuget.org/api/v2/package", nugetPackages );
                         }
                         else
                         {
                            // An alpha, beta, delta, epsilon, gamma, kappa goes to invenietis-preview.
                            PushNuGetPackages( "MYGET_PREVIEW_API_KEY", "https://www.myget.org/F/invenietis-preview/api/v2/package", nugetPackages );
                         }
                     }
                     else
                     {
                         Debug.Assert( gitInfo.IsValidCIBuild );
                         PushNuGetPackages( "MYGET_CI_API_KEY", "https://www.myget.org/F/invenietis-ci/api/v2/package", nugetPackages );
                     }
                     if( Cake.AppVeyor().IsRunningOnAppVeyor )
                     {
                         Cake.AppVeyor().UpdateBuildVersion( gitInfo.SafeNuGetVersion );
                     }
                 } );

            // This is an independent task that must be run manually:
            //
            //      -target=Build-And-Push-CKRemoteStore-WebSite
            //
            // This task does the build.
            // The appsettings.json is not included in the project.
            // (the <ExcludeFilesFromDeployment>appsettings.json</ExcludeFilesFromDeployment> in
            // pubxml seems to be ignored).
            Task( "Build-And-Push-CKRemoteStore-WebSite" )
                .WithCriteria( () => gitInfo.IsValid )
                .Does( () =>
                {
                    var publishPwd = Cake.InteractiveEnvironmentVariable( "PUBLISH_CKSetupRemoteStore_PWD" );
                    if( !String.IsNullOrWhiteSpace( publishPwd ) )
                    {
                        var conf = new MSBuildSettings();
                        conf.Configuration = configuration;
                        conf.Targets.Add( "Build" );
                        conf.Targets.Add( "Publish" );
                        conf.WithProperty( "DeployOnBuild", "true" )
                            .WithProperty( "PublishProfile", "CustomProfile" )
                            .WithProperty( "UserName", "ci@invenietis.net" )
                            .WithProperty( "Password", publishPwd );
                        conf.AddVersionArguments( gitInfo );

                        Cake.MSBuild( "CKSetupRemoteStore/CKSetupRemoteStore.csproj", conf );
                    }
                } );

            // The Default task for this script can be set here.
            Task( "Default" )
                .IsDependentOn( "Push-NuGet-Packages" );
        }

        void PushNuGetPackages(string apiKeyName, string pushUrl, IEnumerable<FilePath> nugetPackages)
        {
            // Resolves the API key.
            var apiKey = Cake.InteractiveEnvironmentVariable(apiKeyName);
            if (string.IsNullOrEmpty(apiKey))
            {
                Cake.Information("Could not resolve {0}. Push to {1} is skipped.", apiKeyName, pushUrl);
            }
            else
            {
                var settings = new NuGetPushSettings
                {
                    Source = pushUrl,
                    ApiKey = apiKey,
                    Verbosity = NuGetVerbosity.Detailed
                };

                foreach (var nupkg in nugetPackages.Where(p => !p.FullPath.EndsWith(".symbols.nupkg")))
                {
                    Cake.Information($"Pushing '{nupkg}' to '{pushUrl}'.");
                    Cake.NuGetPush(nupkg, settings);
                }
            }
        }

        string GetNet461BinFolder( string name, string configuration )
        {
            return System.IO.Path.GetFullPath( name + "/bin/" + configuration + "/net461" );
        }

        string GetNetCoreBinFolder( string name, string configuration )
        {
            string pathToFramework = System.IO.Path.GetFullPath( name + "/bin/" + configuration + "/netstandard2.0" );
            if( !Directory.Exists( pathToFramework ) )
            {
                pathToFramework = System.IO.Path.GetFullPath( name + "/bin/" + configuration + "/netcoreapp2.0" );
            }
            return pathToFramework;
        }

        string EnsurePublishPath( string pathToFramework )
        {
            var publishPath = System.IO.Path.Combine( pathToFramework, "publish" );
            if( !Directory.Exists( publishPath ) )
            {
                var framework = System.IO.Path.GetFileName( pathToFramework );
                var pathToConfiguration = System.IO.Path.GetDirectoryName( pathToFramework );
                var configuration = System.IO.Path.GetFileName( pathToConfiguration );
                var projectPath = System.IO.Path.GetDirectoryName( System.IO.Path.GetDirectoryName( pathToConfiguration ) );
                var projectName = System.IO.Path.GetFileName( projectPath );
                var pI = new ProcessStartInfo()
                {
                    WorkingDirectory = projectPath,
                    FileName = "dotnet",
                    Arguments = $"publish -c {configuration} -f {framework}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Cake.Information( $"Publishing {projectName}: dotnet {pI.Arguments}" );
                using( Process cmdProcess = new Process() )
                {
                    cmdProcess.StartInfo = pI;
                    cmdProcess.ErrorDataReceived += ( o, e ) => { if( !string.IsNullOrEmpty( e.Data ) ) Cake.Error( e.Data ); };
                    cmdProcess.OutputDataReceived += ( o, e ) => { if( e.Data != null ) Cake.Information( e.Data ); };
                    cmdProcess.Start();
                    cmdProcess.BeginErrorReadLine();
                    cmdProcess.BeginOutputReadLine();
                    cmdProcess.WaitForExit();
                    if( cmdProcess.ExitCode != 0 )
                    {
                        Cake.Error( $"Process returned ExitCode {cmdProcess.ExitCode}." );
                        return null;
                    }
                }
            }
            return publishPath;
        }

    }
}
