xcopy ..\CI-StreamLine\nuspec\ck-database\master\*.nuspec /Y

.nuget\nuget.exe pack CK-Database.Console.nuspec
.nuget\nuget.exe pack CK-Database.nuspec

move /Y *.nupkg ..\LocalFeed
del *.nuspec
pause