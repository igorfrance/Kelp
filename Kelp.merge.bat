@echo off
echo Merging Kelp command line utility into a single executable....

set prog=.nuget\packages\ILRepack.1.22.2\tools\ILRepack.exe
if not exist %prog% set prog=..\.nuget\packages\ILRepack.1.22.2\tools\ILRepack.exe
if not exist %prog% goto notfound

%prog% /ndebug /out:Kelp.App\bin\Kelp.exe Kelp.App\bin\Debug\Kelp.App.exe Kelp.App\bin\Debug\AForge.dll Kelp.App\bin\Debug\AForge.Imaging.dll Kelp.App\bin\Debug\AForge.Math.dll Kelp.App\bin\Debug\AjaxMin.dll Kelp.App\bin\Debug\dotless.Core.dll Kelp.App\bin\Debug\Kelp.dll Kelp.App\bin\Debug\log4net.dll Kelp.App\bin\Debug\Newtonsoft.Json.dll

:done
echo Done.
echo Result is Kelp.App\bin\Kelp.exe
goto end

:notfound
echo The ILRepack program could not be found.
goto end

:end
pause