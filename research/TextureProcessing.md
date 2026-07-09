I am building a game engine in C#, my next project is building the asset pipeline. For texture handling I want to generate mipmaps, process the files for compression and efficient loading and then unpack and upload them to the GPU (using DirectX11) in an efficient format for the GPU to work with them.

I've settled on using basis_universal project, see `external/basis_universal` for the basis_universal repository (git submodule) which also contains the documentation and usage examples. basis_universal is a c++ project but I want to access the functionality of this library from C#. 

For a much older version of basis_unversal I created two small projects, one in C++/CLR to integrate with the basis_universal library, and one plain C#/.NET t expose this functionality better to other C# projects. The code for that is in `C:\projects\csharp\SuperCompressed`, but I am not certain if this is the best approach. Do not blindly make another C++/CLR project. But consider all options you have for integrating with C++ code. 

Use the (currently empty) C# project in C:\projects\csharp\CapriKit\source\CapriKit.SuperCompressed\ to place your C# code wrapper in. If you need an extra project to deal with the C++ stuff feel free to create it. But ideally this is all we need.

Considerations
- I want to make it easy to update to newer versions of basis_universal
- I want to minimize custom code and duplication or basis_universal functionality
- I want to access the following functionality of the basis_universal library:
    - Loading image data from various image files (jpg, png, etc..)
    - Generating MipMaps using any of the supported algorithms
    - Encoding and compressing images into the .basis file format
    - Loading and transcoding images into a graphics device native format
- I want the C# classes/enums/records feel as a very lightweight wrapper for the C++ code, from C# you should not have to work with raw pointers. Though working with things like a `Span<>` or `ReadOnlySpan<>` is fine. 


## Follow-up information after Claude read the intial instructions:

Note: I don't need cross-platform capabilities, I'm already tying myself to windows due to the use of DirectX11.
Questions:
- Is it easier to cmake that correctly builds basis_universal and the dlls we need than creating a build.ps1 file?


Answers:
- Image file loading: Yes let's use StbImageSharp again
- Mipmap filter selection: Yes, let's keep it simple for now and do not allow algorithm selection and use the default kaiser filter
- Yes, going for ktx2 only is fine. 