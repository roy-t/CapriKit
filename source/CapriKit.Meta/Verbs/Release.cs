using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapriKit.Meta.Verbs;

internal class Release
{
    // TODO:
    // - dotnet format    
    // - dotnet build --configuration Release
    // - dotnet test --no-build
    // - dotnet solution list
    // foreach...
    // -  dotnet pack .\source\CapriKit.CommandLine\CapriKit.CommandLine.csproj --configuration Release --no-build -o .\.pack
    // end
    // - dotnet nuget push *.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate --api-key ${{secrets.NUGET_API_KEY}}
}
