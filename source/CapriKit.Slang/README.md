# CapriKit.Slang

C# Wrapper The Slang Shading Language SDK (https://github.com/shader-slang/slang). The low-level interop code was generated using SharpGenTools (https://github.com/SharpGenTools/SharpGenTools).

## TODO why doesn't it work:

If I include the entire slang.h file it crashes sharpgen. I have tried to track it down by slowly introducing more of the file. 

2230 OK
2961 OK
3781 OK

4733 WARN, errors in generated code
4875 WARN, errors in generated code 

4910 WARN, errors in generated code
4918 CRASH, The error is caused by  lines 4910-4918:

While narrowing it down further it looks like this C++ method that is not bound to a type (or namespace?)
```
SLANG_EXTERN_C SLANG_API ISlangBlob* slang_createBlob(const void* data, size_t size);
```

Crashes when I have the following mapping in `Mappings.xml`
```
<map function="slang_(.*)" group="CapriKit.Slang.FreeFunctions"/>
```

But without that mapping, SharpGen cannot place the method somewhere.

(Note, I first thought some methods define as SLANG_EXTERN_C SLANG_API were the cause of the issues, but that looks incorrect). 

## Debugging
Generate a better log using `dotnet build -bl` (make sure it does a full build by removing the .build directory first) and open it using the MSBuild Structured Log Viewer.

