# New class file generator from Razor template file

[NuGet project page](https://www.nuget.org/packages/Itn.DotNet.NewClassGenerator/)

# Prerequisits

* [dotnet core runtime 2.1](https://www.microsoft.com/net/download)

## Build requisits

* [dotnet core sdk 2.1](https://www.microsoft.com/net/download)

# Installation

1. execute `dotnet tool install -g Itn.DotNet.NewClassGenerator`
2. ensure `$HOME/.dotnet/tools` in your PATH environment variable

then you can execute `dotnet newclass`.

# Uninstallation

do `dotnet tool uninstall -g Itn.DotNet.NewClassGenerator`.

# Usage

you can get the available options by `dotnet newclass --help`

## Basic

`dotnet newclass -n <NAMESPACE> -o <OUTPUTFILENAME> <CLASSNAME>`

## Using razor template file

`dotnet newclass -n <NAMESPACE> -o <OUTPUTFILENAME> -t <TEMPLATEFILEPATH> <CLASSNAME>`

You can using the following properties in Model

* `@Model.ClassName`
    * specified by last argument
* `@Model.Namespace`
    * means namespace
    * specified by `-n` option
* `@Model.Properties[<NAME>]`
    * specified by `-a` option
    * you can multiple key-value pair(`-a A=B -a C=D`)

# Development

you can build app by `dotnet pack -c Release`,
and then you will get nupkg in `Itn.DotNet.NewClassGenerator/bin/Release/Itn.DotNet.NewClassGenerator.[version].nupkg`.
you can install own build with `dotnet tool install -g --add-source Itn.DotNet.NewClassGenerator/bin/Release Itn.DotNet.NewClassGenerator`

about dotnet global tool, see [dotnet global tool's official document](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)