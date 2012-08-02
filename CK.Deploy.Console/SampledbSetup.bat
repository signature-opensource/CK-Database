echo ---------------- %date%, %time% ------------------ > dbSetup.txt

"D:\Workspaces\Dev4\ck-database\CK.Deploy.Console\bin\Debug\CK.Deploy.Console.exe" "D:\Workspaces\Dev4\ck-authentication" "Server=.;Database=CKAuthentication;Integrated Security=SSPI;" >> dbSetup.txt 
type dbSetup.txt
pause