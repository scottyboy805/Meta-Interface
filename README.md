# C# Meta Interface
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A tool for generating a compilable public interface from an existing C# project while providing no implementation or private/internal details.
Similar to Visual Studio Metadata view when viewing decompiled 3rd party code, except the generated source is compilable in the case of Meta Interface. Designed with Unity engine in mind but can be used as standlone if required.

## Use case (Unity)
In the case of Unity 3d engine, it can be used as an interface to a public API for purposes such as modding, without requiring full source code to be disclosed. 
In the case of Unity it works very well if you also retain the original .meta file for the C# source file, as in the case of MonoBehavious you will be able to attach a generated meta script to a game object, build an asset bundle, and then load that bundle into a project where the full script file resides.
The result is that the loaded game object now correctly link back to the full version of the script so now gains functionality, it can be very powerful for modding or similar UGC where users can make use of existing game scripts to add new content to a game.

## Example
Here is a quick example of an input and output source to give a better understanding:  

![img](https://github.com/scottyboy805/Meta-Interface/blob/main/Docs/Resources/ExampleCode.png)

## How it works
Meta Interface uses Roslyn to parse C# source files and work out which members will be publicly visible and will need to remain. 
This includes public and protected members naturally, but also needs to handle other cases to ensure that compilable code is emitted, such as explicit interface implementations for example. 
All other members that are private or internal can be safely discarded, since any implementation that references them will also be removed. In most cases that just means throwing a NotImplementedException by default meaning that the output code will be compilable but should never be called.  

Meta Interface uses the parser/syntax tree only to generate the output source and there are no checks verifying the validity of the input code via a semantic model or assembly metadata.
It means that the input code could in theory be code that fails to compile, and Meta Interface will carry on as best it can and may also produce something that is not compilable. This is the only scenario where non-compilable output code should be expected, otherwise it is a bug ;).

## Getting Started
- To get started first clone or download the repo  
- For Unity users `Unity - Meta-Interface` contains a Unity project targeting version `2021.3.43 LTS`
- For standalone usage use `Build/BuildStandaloneDebug.bat` or `/BuildStandaloneRelease.bat` on windows to generate the assembly in `Build/Debug/` or `Build/Release` respectivley

## Usage
Meta Interface works on a per source file basis and processes files individually rather than as a .csproj or batch. It keeps things simple and means there is no need to read .csproj files manualy for example.
To generate a meta interface from a given source file can be done as shown:
```cs
// Load a C# source file from some location
MetaSourceFile source = MetaSourceFile.FromFile("MySource.cs");

// Generate the modified syntax tree
SyntaxNode generatedNode = source.ParseAndGenerateMeta(null);

// Now we can overwrite the original file or save to another location (Be careful with overwrite as the input source file is not backed up at any stage)
if(overwrite)
{
  // Write the generate code back to the same source file that was loaded (Overwrite and discard original contents)
  source.OverwriteSource(generatedNode);
}
else
{
  // Write to a different location
  source.WriteSource("MySource-Generated.cs", generatedNode);
}
```
Note that the calling code will need to add a dependency to `Microsoft.CodeAnalysis` as `ParseAndGenerateMeta` method returns a `Microsoft.CodeAnalysis.SyntaxNode`.

## Contributions
Any and all contributons are welcome!

## Dependencies
- Microsoft.CodeAnalysis.CSharp
- Microsoft.CodeAnalysis
- UnityEditor (Optional for asmdef support)

## Sponsors
You can sponsor this project to help it grow
[:heart: Sponsor](https://github.com/sponsors/scottyboy805)
