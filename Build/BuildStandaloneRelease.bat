@echo off
msbuild "../Unity - Meta-Interface/Meta-Interface.csproj" -t:Rebuild -property:OutDir="../Build/Release/" -property:DefineConstants="UNITY" 

"../.tools/pdb2mdb.exe" "Release/Meta-Interface.dll"
