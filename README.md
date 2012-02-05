# Zealib.Build

A project layout specification and MSBuild tasks for .NET development.

## Design

Zealib.Build can help your writing better MSBuild script, you can more easily write for automated build script and CI build script.

* Only work on .Net Framework 4.0
* Include third-party tasks library:
  + [Closure Compiler](http://closurecompiler.codeplex.com)
  + [Microsoft Ajax Minifier](http://ajaxmin.codeplex.com)
  + [MSBuild.ExtensionPack](http://msbuildextensionpack.codeplex.com)
  + [MSBuild.Community.Tasks](http://msbuildtasks.tigris.org)
  + [YUICompressor.NET](http://yuicompressor.codeplex.com)

## MSBuild Tasks
* GetVersion, SetVersion : Get or set executable file version.
* HookProject : Execute MSBuild script on before or after the build.
* ProcessFile : Using pipeline compress or encrypt file.
* PruneSolution : Remove project or item from solution file.
* SetWebProxy : Set build process web proxy.
* More ...

## Building

Run `build\RunBuild.cmd` batch script to build.

## Usage

See the `src\Zealib.Build.Tests\Zealib.Build.Tasks` directory for usage examples.

## Authors

* [Zealic](http://www.github.com/zealic) `<zealic@gmail.com>`

##LICENSE

[BSD License](http://creativecommons.org/licenses/BSD/)
