# CapriKit
_Roy Triesscheijn's collection of code libraries_

- GitHub: https://github.com/roy-t/
- Blog: https://roy-t.nl
- Mastodon: https://mastodon.social/@roytries (rarely used)
- LinkedIn: https://www.linkedin.com/in/roy-triesscheijn/

## Goal

I have been writing code since 2003. I started in Visual Basic 6 and I switched to C# code since 2007. Since then I have written thousands of lines of code. Most of that code was barely tested and sits in forgotten repositories.

With CapriKit I try to make a library where I gather all code that I find relevant and through documentation and testing I try to make it reusable and have lasting value. For myself, and maybe for others. Most of the code is focussed on game engine development as that is my favorite hobby.

## How to Build, Run, Test and Lint
In general you can use the standard .net commands. These examples here assume you are using `powershell`. (when using Git Bash the `/` characters in 

```pwsh
# build the solution
dotnet build

# run all tests or a single test
dotnet test     
dotnet test --treenode-filter /Assembly/Namespace/Class/Method

# formatting according to .editorconfig
dotnet format   # format code

# running an individual project
dotnet run --project <the project>
```

> [!WARNING]
> These examples assume you are using `powershell`. When using Git Bash the `/` characters are silently converted to `\`, making the commands fail. Use powershell or set `MSYS_NO_PATHCONV=1` to work around this problem.

> [!NOTE]
> This project uses non-standard output directories. See the `.build` directory in the root of this repository. This keeps all source code in the `source` folder clean.

There is a command line utility called `CapriKit.Meta` that assist in publishing in a consistent way. It also contains utilities to run benchmarks and to compare if those results significantly differ from previous versions. Build the project in DEBUG mode and run it from `.build\bin\CapriKit.Meta-Debug\CapriKit.Meta.exe`.

## TODO
Investigate making all libraries AOT Compatible `<IsAotCompatible>true</IsAotCompatible>` but if something, like a LightInject cannot be made AOT compatible it will not work. Otherwise suggest compiling with ReadyToRun on the consumer side which is mixed tier-0 compiled code with JIT. (The tier-0 code will slowly be Jitted to more optimized code, thought that can be disabled to avoid hitches).
