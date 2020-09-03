using Cake.Common.IO;
using Cake.Core;
using Cake.Core.Diagnostics;
using SimpleGitVersion;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCake
{
    [AddPath( "%UserProfile%/.nuget/packages/**/tools*" )]
    public partial class Build : CodeCakeHost
    {

        public Build()
        {
            Cake.Log.Verbosity = Verbosity.Diagnostic;

            StandardGlobalInfo globalInfo = CreateStandardGlobalInfo()
                                                .AddDotnet()
                                                .SetCIBuildTag();

            Task( "Check-Repository" )
                .Does( () =>
                {
                    globalInfo.TerminateIfShouldStop();
                } );

            Task( "Clean" )
                .IsDependentOn( "Check-Repository" )
                .Does( () =>
                 {
                     globalInfo.GetDotnetSolution().Clean();
                     Cake.CleanDirectories( globalInfo.ReleasesFolder );
                     Cake.CleanDirectory( "Tests/LocalTestHelper/LocalTestStore" );
                    
                 } );

            Task( "Build" )
                .IsDependentOn( "Check-Repository" )
                .IsDependentOn( "Clean" )
                .Does( () =>
                 {
                    globalInfo.GetDotnetSolution().Build();
                 } );

            Task( "Unit-Testing" )
                .IsDependentOn( "Build" )
                .WithCriteria( () => Cake.InteractiveMode() == InteractiveMode.NoInteraction
                                     || Cake.ReadInteractiveOption( "RunUnitTests", "Run Unit Tests?", 'Y', 'N' ) == 'Y' )
                .Does( () =>
                 {

                     while( true )
                     {
                         CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                         System.Threading.Tasks.Task.Delay( 60 * 5 * 1000, cancellationTokenSource.Token )
                         .ContinueWith( s =>
                          {
                              if( s.IsCanceled ) return;
                              if( !s.IsCompleted ) return;
                              Debugger.Break();
                          } );
                         globalInfo.GetDotnetSolution().Test();
                         cancellationTokenSource.Cancel();
                     }
                 } );

            Task( "Create-NuGet-Packages" )
                .WithCriteria( () => globalInfo.IsValid )
                .IsDependentOn( "Unit-Testing" )
                .Does( () =>
                 {
                    globalInfo.GetDotnetSolution().Pack();
                 } );

            Task( "Push-Runtimes-and-Engines" )
                .IsDependentOn( "Unit-Testing" )
                .WithCriteria( () => globalInfo.IsValid )
                .Does( () =>
                {
                    StandardPushCKSetupComponents( globalInfo );
                } );

            Task( "Push-NuGet-Packages" )
                .IsDependentOn( "Create-NuGet-Packages" )
                .WithCriteria( () => globalInfo.IsValid )
                .Does( () =>
                 {
                    globalInfo.PushArtifacts();
                 } );

            // The Default task for this script can be set here.
            Task( "Default" )
                .IsDependentOn( "Push-Runtimes-and-Engines" )
                .IsDependentOn( "Push-NuGet-Packages" );
        }

    }
}
