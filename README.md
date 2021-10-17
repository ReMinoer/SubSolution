# SubSolution

[![Nuget](https://img.shields.io/nuget/v/SubSolution?label=SubSolution&color=004880&logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/SubSolution)
[![Nuget](https://img.shields.io/nuget/v/subsln?label=subsln&color=004880&logo=windowsterminal&style=for-the-badge)](https://www.nuget.org/packages/subsln)

SubSolution is a tool giving you control on your Visual Studio solutions.

It came in two forms:

- __SubSolution .NET libraries__ to manipulate read/edit/write solutions as you want.
- __"subsln" command line tool__ to generate/update .sln files from .subsln files.

# .NET libraries

[![Change](https://img.shields.io/github/workflow/status/ReMinoer/SubSolution/Change?color=forestgreen&label=Change&logo=github&style=for-the-badge)](https://github.com/ReMinoer/SubSolution/actions/workflows/change.yml)
[![Release](https://img.shields.io/github/workflow/status/ReMinoer/SubSolution/Release?color=forestgreen&label=Release&logo=github&style=for-the-badge)](https://github.com/ReMinoer/SubSolution/actions/workflows/release.yml)

You can use SubSolution .NET libraries as Nuget packages:

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

> __SubSolution is currently released as version v0.__ All core features are already implemented but it needs to be tested in more practical cases.
>
> Also be aware of the following:
>
> - Some small API breaking changes might happen until version v1.
> - Some MSBuild project types are not supported yet. ([Supported project types](https://github.com/ReMinoer/SubSolution/blob/master/Sources/SubSolution/ProjectType.cs))


# Command line tool: `subsln`

"`subsln`" is a command line tool using `.subsln` configuration files to build Visual Studio solutions.

```bash
> subsln create MySolution
> subsln generate MySolution.subsln
> subsln validate MySolution.subsln
> subsln show MySolution.sln
```

You can download a standalone version from the [Releases](https://github.com/ReMinoer/SubSolution/releases) page.

Or install it with the [.NET SDK](https://dotnet.microsoft.com/download) command line:

```bash
> dotnet tool install subsln --global 
```

Use `subsln help` or `subsln [command] --help` for more details on commands.

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

# Contribute

- Install [.NET SDK](https://dotnet.microsoft.com/download)
- Make a fork of the repo.
- Clone it locally (including submodules if you are using ReSharper in Visual Studio).
- Build the solution.
    - It will automatically install [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore) to generate C# from the XML schema.
- Make your changes.
- Submit a pull request.