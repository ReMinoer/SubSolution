# SubSolution

SubSolution use XML files with the extension `.subsln` to describe the content of Visual Studio solutions in a user-friendly syntax.

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

The Visual Studio extension includes the following features:

- You can create/open the .subsln file associated to the current solution from the Solution Explorer context menu.

  ![Command](https://raw.githubusercontent.com/ReMinoer/SubSolution/master/Images/vs_command.png)

- When saving a .subsln file, you can see a preview of the updated solution and decide if you want to modify the solution or not.

  ![Save](https://raw.githubusercontent.com/ReMinoer/SubSolution/master/Images/vs_save.png)

- When opening a solution with an associated .subsln file, it automatically checks if your solution is up-to-date.

  ![Load](https://raw.githubusercontent.com/ReMinoer/SubSolution/master/Images/vs_load.png)

