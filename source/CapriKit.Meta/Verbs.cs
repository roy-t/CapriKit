using CapriKit.CommandLine.Types;

namespace CapriKit.Meta;


/// <verb>Lalalalal</verb>
/// <summary>
/// Bumps the package version, in line with semantic versioning 2.0
/// </summary>
[Verb("bump")]
public partial class Bump
{
    ///// <summary>
    ///// Increase the major version of the package
    ///// </summary>
    //[Flag("--major")]
    //public partial bool Major { get; }

    ///// <summary>
    ///// Increase the minor version of the package
    ///// </summary>
    //[Flag("--minor")]
    //public partial bool Minor { get; }

    ///// <summary>
    ///// Increase the patch version of the package
    ///// </summary>
    //[Flag("--patch")]
    //public partial bool Patch { get; }

    ///// <summary>
    ///// Set the prerelease information
    ///// </summary>
    //[Flag("--prerelease")]
    //public partial string Prerelease { get; }

    ///// <summary>
    ///// Set the build meta data
    ///// </summary>
    //[Flag("--build-meta-data")]
    //public partial string BuildMetaData { get; }
}



// TODO: generate something like this:

//public partial class Bump
//{
//    private bool major;
//    public partial bool Major { get => this.major; }
//}
