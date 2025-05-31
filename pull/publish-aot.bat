@echo off
rm -r -force .\bin\Release\
dotnet publish -r win-x64 -c Release
cp -force .\bin\Release\net8.0\win-x64\native\pull.exe ..\
@echo on