@echo off

rem Win32 batch file
rem
rem Shell script to run PDF Glown for .NET samples on MS Windows.

rem Deploy dependencies on local (private) path!
xcopy ..\PdfGlown\build\package\*.dll .\build\package /D /Y
xcopy ..\PdfGlown\lib\*.dll .\build\package /D /Y
copy PdfGlownCLISamples.exe.config .\build\package

rem Execute the samples!
.\build\package\PdfGlownCLISamples.exe
