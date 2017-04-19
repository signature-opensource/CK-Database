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
using SimpleGitVersion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeCake
{
    public static class DotNetCoreRestoreSettingsExtension
    {
        public static T AddVersionArguments<T>(this T @this, SimpleRepositoryInfo info, Action<T> conf = null) where T : DotNetCoreSettings
        {
            var prev = @this.ArgumentCustomization;
            @this.ArgumentCustomization = args => (prev?.Invoke(args) ?? args)
                    .Append($@"/p:CakeBuild=""true""");

           if (info.IsValid)
            {
                var prev2 = @this.ArgumentCustomization;
                @this.ArgumentCustomization = args => (prev2?.Invoke(args) ?? args)
                        .Append($@"/p:Version=""{info.NuGetVersion}""")
                        .Append($@"/p:AssemblyVersion=""{info.MajorMinor}.0""")
                        .Append($@"/p:FileVersion=""{info.FileVersion}""")
                        .Append($@"/p:InformationalVersion=""{info.SemVer} ({info.NuGetVersion}) - SHA1: {info.CommitSha} - CommitDate: {info.CommitDateUtc.ToString("u")}""");
            }
            conf?.Invoke(@this);
            return @this;
        }
        public static MSBuildSettings AddVersionArguments(this MSBuildSettings @this, SimpleRepositoryInfo info, Action<MSBuildSettings> conf = null)
        {
            var prev = @this.ArgumentCustomization;
            @this.ArgumentCustomization = args => (prev?.Invoke(args) ?? args)
                    .Append($@"/p:CakeBuild=""true""");

            if (info.IsValid)
            {
                var prev2 = @this.ArgumentCustomization;
                @this.ArgumentCustomization = args => (prev2?.Invoke(args) ?? args)
                        .Append($@"/p:Version=""{info.NuGetVersion}""")
                        .Append($@"/p:AssemblyVersion=""{info.MajorMinor}.0""")
                        .Append($@"/p:FileVersion=""{info.FileVersion}""")
                        .Append($@"/p:InformationalVersion=""{info.SemVer} ({info.NuGetVersion}) - SHA1: {info.CommitSha} - CommitDate: {info.CommitDateUtc.ToString("u")}""");
            }
            conf?.Invoke(@this);
            return @this;
        }
    }

    /// <summary>
    /// Standard build "script".
    /// </summary>
    [AddPath("CodeCakeBuilder/Tools")]
    [AddPath("packages/**/tools*")]
    public class Build : CodeCakeHost
    {
        public Build()
        {
            Cake.Log.Verbosity = Verbosity.Diagnostic;

            const string solutionName = "CK-Database";
            const string solutionFileName = solutionName + ".sln";

            var releasesDir = Cake.Directory("CodeCakeBuilder/Releases");

            var projects = Cake.ParseSolution(solutionFileName)
                                       .Projects
                                       .Where(p => !(p is SolutionFolder)
                                                   && p.Name != "CodeCakeBuilder");

            // We do not publish .Tests projects for this solution.
            var projectsToPublish = projects
                                        .Where(p => !p.Path.Segments.Contains("Tests"));

            SimpleRepositoryInfo gitInfo = Cake.GetSimpleRepositoryInfo();

            // Configuration is either "Debug" or "Release".
            string configuration = null;
            bool buildDone = false;

            Teardown( c =>
            {
                if( buildDone ) c.CleanDirectories(projects.Select(p => p.Path.GetDirectory().Combine("bin")));
            });

            Task("Check-Repository")
                .Does(() =>
                {
                    if (!gitInfo.IsValid)
                    {
                        if (Cake.IsInteractiveMode()
                            && Cake.ReadInteractiveOption("Repository is not ready to be published. Proceed anyway?", 'Y', 'N') == 'Y')
                        {
                            Cake.Warning("GitInfo is not valid, but you choose to continue...");
                        }
                        else throw new Exception("Repository is not ready to be published.");
                    }

                    configuration = gitInfo.IsValidRelease
                                    && (gitInfo.PreReleaseName.Length == 0 || gitInfo.PreReleaseName == "rc")
                                    ? "Release"
                                    : "Debug";

                    Cake.Information("Publishing {0} projects with version={1} and configuration={2}: {3}",
                        projectsToPublish.Count(),
                        gitInfo.SemVer,
                        configuration,
                        string.Join(", ", projectsToPublish.Select(p => p.Name)));
                });

            Task("Clean")
                .IsDependentOn("Check-Repository")
                .Does(() =>
                {
                    Cake.CleanDirectories(projects.Select(p => p.Path.GetDirectory().Combine("bin")));
                    Cake.CleanDirectories(releasesDir);
                    Cake.DeleteFiles("Tests/**/TestResult*.xml");
                });

            Task("Restore-NuGet-Packages")
                .IsDependentOn("Check-Repository")
                .IsDependentOn("Clean")
                .Does(() =>
                {
                    // https://docs.microsoft.com/en-us/nuget/schema/msbuild-targets
                    Cake.DotNetCoreRestore(new DotNetCoreRestoreSettings().AddVersionArguments(gitInfo));
                    Cake.NuGetRestore("CKDBSetup/packages.config", new NuGetRestoreSettings()
                    {
                        PackagesDirectory = "packages"
                    });
                    //string txt = System.IO.File.ReadAllText(@"CodeCakeBuilder\Releases\obj\SqlCallDemo.Tests\SqlCallDemo.Tests.csproj.nuget.g.props");
                    //Console.WriteLine("------------nuget.g.props------------------");
                    //Console.WriteLine(txt);
                    //Console.WriteLine("-------------------------------------------");
                });

            Task("Build")
                .IsDependentOn("Check-Repository")
                .IsDependentOn("Clean")
                .IsDependentOn("Restore-NuGet-Packages")
                .Does(() =>
                {
                    buildDone = true;
                    Cake.DotNetCoreBuild("CodeCakeBuilder/CoreBuild.proj",
                        new DotNetCoreBuildSettings().AddVersionArguments(gitInfo, s =>
                        {
                            s.Configuration = configuration;
                            // WHY? It works on local, but not on Appveyor :(
                            // The NuGetPackageFolders is not set (from the obj/g.props).
                            if ( Cake.AppVeyor().IsRunningOnAppVeyor )
                            {
                                var prev = s.ArgumentCustomization;
                                s.ArgumentCustomization = c =>
                                {
                                    prev?.Invoke(c);
                                    c.Append(@"/p:NuGetPackageFolders=""C:\Users\appveyor\.nuget\packages\""");
                                    return c;
                                };
                            }
                        }));

                    Cake.MSBuild("CKDBSetup/CKDBSetup.csproj", new MSBuildSettings().AddVersionArguments(gitInfo, s =>
                        {
                            s.Configuration = configuration;
                            s.ToolVersion = MSBuildToolVersion.VS2015;
                        }));
                });

            Task("Unit-Testing")
                .IsDependentOn("Build")
                .Does(() =>
                {
                    Cake.CreateDirectory(releasesDir);
                    var testDlls = projects.Where(p => p.Name.EndsWith(".Tests")).Select(p =>
                            new
                            {
                                ProjectPath = p.Path.GetDirectory(),
                                NetCoreAppDll = p.Path.GetDirectory().CombineWithFilePath("bin/" + configuration + "/netcoreapp1.0/" + p.Name + ".dll"),
                                Net451Dll = p.Path.GetDirectory().CombineWithFilePath("bin/" + configuration + "/net451/" + p.Name + ".dll"),
                            });

                    foreach (var test in testDlls)
                    {
                        using (Cake.Environment.SetWorkingDirectory(test.ProjectPath))
                        {
                            Cake.Information("Testing: {0}", test.Net451Dll);
                            Cake.NUnit(test.Net451Dll.FullPath, new NUnitSettings()
                            {
                                Framework = "v4.5",
                                ResultsFile = test.ProjectPath.CombineWithFilePath("TestResult.Net451.xml")
                            });
                            if(System.IO.File.Exists( test.NetCoreAppDll.FullPath ) )
                            {
                                Cake.Information("Testing: {0}", test.NetCoreAppDll);
                                Cake.DotNetCoreExecute(test.NetCoreAppDll);
                            }
                        }
                    }
                });

            Task("Create-NuGet-Package-For-CKDBSetup")
                .WithCriteria(() => gitInfo.IsValid)
                .IsDependentOn("Unit-Testing")
                .Does(() =>
                {
                    Cake.CreateDirectory(releasesDir);
                    var settings = new NuGetPackSettings()
                    {
                        Version = gitInfo.NuGetVersion,
                        BasePath = Cake.Environment.WorkingDirectory,
                        OutputDirectory = releasesDir
                    };
                    var tempNuspec = releasesDir.Path.CombineWithFilePath("CKDBSetup.nuspec");
                    Cake.CopyFile("CodeCakeBuilder/NuSpec/CKDBSetup.nuspec", tempNuspec);
                    Cake.TransformTextFile(tempNuspec, "{{", "}}")
                            .WithToken("configuration", configuration)
                            .WithToken("NuGetVersion", gitInfo.NuGetVersion)
                            .WithToken("CSemVer", gitInfo.SemVer)
                            .Save(tempNuspec);
                    Cake.NuGetPack(tempNuspec, settings);
                    Cake.DeleteFile(tempNuspec);
                });

            Task("Create-All-NuGet-Packages")
                .WithCriteria(() => gitInfo.IsValid)
                .IsDependentOn("Unit-Testing")
                .IsDependentOn("Create-NuGet-Package-For-CKDBSetup")
                .Does(() =>
                {
                    Cake.CreateDirectory(releasesDir);
                    var settings = new DotNetCorePackSettings();
                    settings.ArgumentCustomization = args => args.Append("--include-symbols");
                    settings.NoBuild = true;
                    settings.Configuration = configuration;
                    settings.OutputDirectory = releasesDir;
                    settings.AddVersionArguments(gitInfo);
                    Cake.DotNetCorePack("CodeCakeBuilder/CoreBuild.proj", settings);
                });


            Task("Push-NuGet-Packages")
                .IsDependentOn("Create-All-NuGet-Packages")
                .WithCriteria(() => gitInfo.IsValid)
                .Does(() =>
                {
                    IEnumerable<FilePath> nugetPackages = Cake.GetFiles(releasesDir.Path + "/*.nupkg");
                    if (Cake.IsInteractiveMode())
                    {
                        var localFeed = Cake.FindDirectoryAbove("LocalFeed");
                        if (localFeed != null)
                        {
                            Cake.Information("LocalFeed directory found: {0}", localFeed);
                            if (Cake.ReadInteractiveOption("Do you want to publish to LocalFeed?", 'Y', 'N') == 'Y')
                            {
                                Cake.CopyFiles(nugetPackages, localFeed);
                            }
                        }
                    }
                    if (gitInfo.IsValidRelease)
                    {
                        if (gitInfo.PreReleaseName == ""
                            || gitInfo.PreReleaseName == "prerelease"
                            || gitInfo.PreReleaseName == "rc")
                        {
                            PushNuGetPackages("NUGET_API_KEY", "https://www.nuget.org/api/v2/package", nugetPackages);
                        }
                        else
                        {
                            // An alpha, beta, delta, epsilon, gamma, kappa goes to invenietis-preview.
                            PushNuGetPackages("MYGET_PREVIEW_API_KEY", "https://www.myget.org/F/invenietis-preview/api/v2/package", nugetPackages);
                        }
                    }
                    else
                    {
                        Debug.Assert(gitInfo.IsValidCIBuild);
                        PushNuGetPackages("MYGET_CI_API_KEY", "https://www.myget.org/F/invenietis-ci/api/v2/package", nugetPackages);
                    }
                    if (Cake.AppVeyor().IsRunningOnAppVeyor)
                    {
                        Cake.AppVeyor().UpdateBuildVersion(gitInfo.SemVer);
                    }
                });

            // The Default task for this script can be set here.
            Task("Default")
                .IsDependentOn("Push-NuGet-Packages");

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
    }
}