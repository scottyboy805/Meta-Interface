~echo off
msbuild "Unity - Meta-Interface/Meta-Interface.csproj" -t:Rebuild -property:OutDir="../Build - Meta-Interface-Standalone" -property:DefineConstants="UNITY;DEBUG" 

".tools/pdb2mdb.exe" "Build - Meta-Interface-Standalone/Meta-Interface.dll"
