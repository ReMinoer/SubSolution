# SubSolution

[![Change](https://github.com/ReMinoer/SubSolution/actions/workflows/change.yml/badge.svg)](https://github.com/ReMinoer/SubSolution/actions/workflows/change.yml)
[![Release](https://github.com/ReMinoer/SubSolution/actions/workflows/release.yml/badge.svg)](https://github.com/ReMinoer/SubSolution/actions/workflows/release.yml)
[![Nuget](https://img.shields.io/nuget/v/SubSolution?label=SubSolution&color=004880&logo=nuget)](https://www.nuget.org/packages/SubSolution)
[![Nuget](https://img.shields.io/nuget/v/subsln?label=subsln&color=004880&logo=windowsterminal)](https://www.nuget.org/packages/subsln)

SubSolution is a tool giving you control on your Visual Studio solutions.

It came in three forms:

- __Configuration files ".subsln"__ to describe the content of a Visual Studio solution.
- __Command line tool "subsln"__ to generate/update .sln files from .subsln files.
- __.NET libraries__ to manipulate read/edit/write solutions as you want.

> SubSolution is currently released as version __v0__. All core features are already implemented but it needs to be tested in more practical cases.
>
> Also be aware of the following:
>
> - Some small API breaking changes might happen until version v1.
> - Some MSBuild project types are not supported yet. ([Supported project types](https://github.com/ReMinoer/SubSolution/blob/master/Sources/SubSolution/ProjectType.cs))

# Configuration files: `.subsln`

SubSolution introduce a XML file format using extension `.subsln` to describe the content of Visual Studio solutions with a more user-friendly syntax than .sln format. You can generate solutions from it or update existing ones.

```xml
<SubSolutionConfiguration>
    <Root>
        <Folder Name="Tools">
            <Files Path="tools/*.bat" />
        </Folder>
        <Folder Name="Tests">
            <Projects Path="**/*.Tests.csproj" />
        </Folder>
        <Projects Path="src/">
    </Root>
</SubSolutionConfiguration>
```

## XML Syntax

- Describe your item hierarchy: `<Root>`
    - Create folders to organize your items: `<Folder>`
    - Add projects with glob patterns: `<Projects>`
    - Add files for quick-access: `<Files>`
    - Find project dependencies and dependents: `<Dependencies>` / `<Dependents>`
    - Include the content of existing solutions: `<Solutions>` / `<SubSolutions>`
    - Select what you want to keep from other solutions: `<KeepOnly>`
    - Apply complex filters on your item sources: `<Where>`
- Setup your solution configuration-platforms: `<Configurations>` / `<Platforms>`
    - Ignore them to auto-generate from projects.
    - Create new ones: `<SolutionConfiguration>` / `<SolutionPlatform>`
    - Match them with project configurations and platforms: `<ProjectConfiguration>` / `<ProjectPlatform>`
- And a lot more options as XML attributes !

## Why use a `.subsln` file ?

- It allows you to __express your organization rules__ ("those projects in that folder, unit tests in that one...") and __ensure they are respected__ on solution changes.
- It acts as a __substitute or edition assistant__ of .sln files, to describe the solution content with a __user-friendly structure__ similar to Visual Studio representation.
- It can also be used as a punctual tool, to __apply a one-time update__.
- It allows to __quickly iterate__ on your solution structure until it matches your needs, without even running Visual Studio.
- It can __build an entirely customized hierarchy__, or at contrary __mirror your file system structure__.
- It can __find and fill your solution with dependencies__ of your central project.
- It can describe solutions __in a modular way__ by including the content of a solution into another.
- It can __apply changes to multiple solutions__ sharing the same projects.
- It can __divide a big solution in smaller ones__ to reduce impact on Visual Studio performances.

# Command line tool: `subsln`

`subsln.exe` is a command line tool using `.subsln` configuration files to build your Visual Studio solutions.

```bash
> subsln create MySolution
> subsln generate MySolution.subsln
> subsln validate MySolution.subsln
> subsln display MySolution.sln
```

Use `subsln help` or `subsln [command] --help` for more details on commands.

## Download

You can download the last version from [Releases page](https://github.com/ReMinoer/SubSolution/releases).

Or install it as a tool with the [.NET SDK](https://dotnet.microsoft.com/download):

```bash
> dotnet tool install subsln --global 
```

# .NET libraries

If you have specific needs or need a high level of control, you can use those .NET libraries as Nuget packages:

- [SubSolution](https://www.nuget.org/packages/SubSolution): core package to edit solutions using minimum dependencies.
- [SubSolution.Builders](https://www.nuget.org/packages/SubSolution.Builders): solution building features used by .subsln format.
- [SubSolution.MsBuild](https://www.nuget.org/packages/SubSolution.MsBuild): project reader implementation based on MSBuild.

The API is structured around 3 representations of solutions:

- __`Solution`__:
    - Match the Visual Studio edition experience.
    - Edit the item hierarchy as it show in the Solution Explorer.
    - Automatically fill solution configurations-platforms with your projects.
    - Use __`ManualSolution`__ to manually fill configuration-platforms.
- __`RawSolution`__:
    - Match the .sln format.
    - Dedicated to input/ouput files (but hard to edit).
    - Convert to/from __`Solution`__ with __`RawSolutionConverter`__/__`SolutionConverter`__.
- __`SubSolutionConfiguration`__:
    - Match the .subsln format.
    - Dedicated to build patterns & modular usages.
    - Build __`Solution`__ with __`SolutionBuilder`__.

## Contribute

- Install [.NET SDK](https://dotnet.microsoft.com/download)
- Make a fork of the repo.
- Clone it locally (including submodules if you are using ReSharper in Visual Studio).
- Build the solution.
    - It will automatically download [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore) to generate C# from the XML schema.
- Make your changes.
- Submit a pull request.