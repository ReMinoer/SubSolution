# SubSolution extension

- Create/open the .subsln file associated to the current solution from the Solution Explorer context menu.
- When saving .subsln files, a preview of the resulting solution is shown so you can decide if you want to applied it or not.
- Visual Studio automatically check if the solution is up-to-date at solution opening.

# SubSolution configuration files

SubSolution use a XML file format with extension `.subsln` to describe the content of Visual Studio solutions in a more user-friendly syntax than .sln format.

```xml
<Subsln xmlns="http://subsln.github.io">
    <Root>
        <Folder Name="Tools">
            <Files Path="tools/*.bat" />
        </Folder>
        <Folder Name="Tests">
            <Projects Path="**/*.Tests.csproj" />
        </Folder>
        <Projects Path="src/">
    </Root>
</Subsln>
```

More details on our [GitHub page](https://github.com/ReMinoer/SubSolution).