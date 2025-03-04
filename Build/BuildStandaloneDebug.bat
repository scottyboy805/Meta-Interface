@echo off
msbuild "../Unity - Meta-Interface/Meta-Interface.csproj" -t:Rebuild -property:OutDir="../Build/Debug/" -property:DefineConstants="UNITY;DEBUG" 

"../.tools/pdb2mdb.exe" "Debug/Meta-Interface.dll"
