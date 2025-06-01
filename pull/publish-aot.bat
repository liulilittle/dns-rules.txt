@echo off
rmdir /s /q .\bin
rmdir /s /q .\obj
dotnet publish -r win-x64 -c Release
copy /y .\bin\Release\net8.0\win-x64\native\pull.exe ..\
@echo on