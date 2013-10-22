@echo off
echo Merging Kelp command line utility into a single executable....
.nuget\packages\ILRepack.1.22.2\tools\ILRepack.exe /ndebug /out:Kelp.App\bin\Kelp.exe Kelp.App\bin\Debug\Kelp.App.exe Kelp.App\bin\Debug\AForge.dll Kelp.App\bin\Debug\AForge.Imaging.dll Kelp.App\bin\Debug\AForge.Math.dll Kelp.App\bin\Debug\AjaxMin.dll Kelp.App\bin\Debug\dotless.Core.dll Kelp.App\bin\Debug\Kelp.dll Kelp.App\bin\Debug\log4net.dll Kelp.App\bin\Debug\Newtonsoft.Json.dll
echo Done.
