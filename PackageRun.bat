xcopy ..\CI-StreamLine\nuspec\ck-database\develop\*.nuspec /Y

.nuget\nuget.exe pack CK.StObj.Model.nuspec
.nuget\nuget.exe pack CK.Setupable.Model.nuspec
.nuget\nuget.exe pack CK.SqlServer.Setup.Model.nuspec

.nuget\nuget.exe pack CK.SqlServer.nuspec
.nuget\nuget.exe pack CK.Setup.Dependency.nuspec

.nuget\nuget.exe pack CK.StObj.Runtime.nuspec
.nuget\nuget.exe pack CK.Setupable.Runtime.nuspec
.nuget\nuget.exe pack CK.SqlServer.Setup.Runtime.nuspec

.nuget\nuget.exe pack CK.StObj.Engine.nuspec
.nuget\nuget.exe pack CK.Setupable.Engine.nuspec
.nuget\nuget.exe pack CK.SqlServer.Setup.Engine.nuspec

move /Y *.nupkg ..\LocalFeed
del *.nuspec
pause