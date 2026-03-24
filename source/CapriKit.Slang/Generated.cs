using ClangSharp.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static CapriKit.Slang.LayoutRules;
using static CapriKit.Slang.ParameterCategory;
using static CapriKit.Slang.SlangBindingType;
using static CapriKit.Slang.SlangDeclKind;
using static CapriKit.Slang.SlangLayoutRules;
using static CapriKit.Slang.SlangModifierID;
using static CapriKit.Slang.SlangParameterCategory;
using static CapriKit.Slang.SlangScalarType;
using static CapriKit.Slang.SlangTarget;
using static CapriKit.Slang.SlangTypeKind;
using static CapriKit.Slang.SpecializationArg.Kind;
using static CapriKit.Slang.TypeReflection.Kind;

namespace CapriKit.Slang.CA
{
    [NativeTypeName("SlangSeverityIntegral")]
    public enum SlangSeverity
    {
        SLANG_SEVERITY_DISABLED = 0,
        SLANG_SEVERITY_NOTE,
        SLANG_SEVERITY_WARNING,
        SLANG_SEVERITY_ERROR,
        SLANG_SEVERITY_FATAL,
        SLANG_SEVERITY_INTERNAL,
    }

    public enum SlangDiagnostic
    {
        SLANG_DIAGNOSTIC_FLAG_VERBOSE_PATHS = 0x01,
        SLANG_DIAGNOSTIC_FLAG_TREAT_WARNINGS_AS_ERRORS = 0x02,
    }

    [NativeTypeName("SlangBindableResourceIntegral")]
    public enum SlangBindableResourceType
    {
        SLANG_NON_BINDABLE = 0,
        SLANG_TEXTURE,
        SLANG_SAMPLER,
        SLANG_UNIFORM_BUFFER,
        SLANG_STORAGE_BUFFER,
    }

    [NativeTypeName("SlangCompileTargetIntegral")]
    public enum SlangCompileTarget
    {
        SLANG_TARGET_UNKNOWN,
        SLANG_TARGET_NONE,
        SLANG_GLSL,
        SLANG_GLSL_VULKAN_DEPRECATED,
        SLANG_GLSL_VULKAN_ONE_DESC_DEPRECATED,
        SLANG_HLSL,
        SLANG_SPIRV,
        SLANG_SPIRV_ASM,
        SLANG_DXBC,
        SLANG_DXBC_ASM,
        SLANG_DXIL,
        SLANG_DXIL_ASM,
        SLANG_C_SOURCE,
        SLANG_CPP_SOURCE,
        SLANG_HOST_EXECUTABLE,
        SLANG_SHADER_SHARED_LIBRARY,
        SLANG_SHADER_HOST_CALLABLE,
        SLANG_CUDA_SOURCE,
        SLANG_PTX,
        SLANG_CUDA_OBJECT_CODE,
        SLANG_OBJECT_CODE,
        SLANG_HOST_CPP_SOURCE,
        SLANG_HOST_HOST_CALLABLE,
        SLANG_CPP_PYTORCH_BINDING,
        SLANG_METAL,
        SLANG_METAL_LIB,
        SLANG_METAL_LIB_ASM,
        SLANG_HOST_SHARED_LIBRARY,
        SLANG_WGSL,
        SLANG_WGSL_SPIRV_ASM,
        SLANG_WGSL_SPIRV,
        SLANG_HOST_VM,
        SLANG_CPP_HEADER,
        SLANG_CUDA_HEADER,
        SLANG_HOST_OBJECT_CODE,
        SLANG_HOST_LLVM_IR,
        SLANG_SHADER_LLVM_IR,
        SLANG_TARGET_COUNT_OF,
    }

    [NativeTypeName("SlangContainerFormatIntegral")]
    public enum SlangContainerFormat
    {
        SLANG_CONTAINER_FORMAT_NONE,
        SLANG_CONTAINER_FORMAT_SLANG_MODULE,
    }

    [NativeTypeName("SlangPassThroughIntegral")]
    public enum SlangPassThrough
    {
        SLANG_PASS_THROUGH_NONE,
        SLANG_PASS_THROUGH_FXC,
        SLANG_PASS_THROUGH_DXC,
        SLANG_PASS_THROUGH_GLSLANG,
        SLANG_PASS_THROUGH_SPIRV_DIS,
        SLANG_PASS_THROUGH_CLANG,
        SLANG_PASS_THROUGH_VISUAL_STUDIO,
        SLANG_PASS_THROUGH_GCC,
        SLANG_PASS_THROUGH_GENERIC_C_CPP,
        SLANG_PASS_THROUGH_NVRTC,
        SLANG_PASS_THROUGH_LLVM,
        SLANG_PASS_THROUGH_SPIRV_OPT,
        SLANG_PASS_THROUGH_METAL,
        SLANG_PASS_THROUGH_TINT,
        SLANG_PASS_THROUGH_SPIRV_LINK,
        SLANG_PASS_THROUGH_COUNT_OF,
    }

    [NativeTypeName("SlangArchiveTypeIntegral")]
    public enum SlangArchiveType
    {
        SLANG_ARCHIVE_TYPE_UNDEFINED,
        SLANG_ARCHIVE_TYPE_ZIP,
        SLANG_ARCHIVE_TYPE_RIFF,
        SLANG_ARCHIVE_TYPE_RIFF_DEFLATE,
        SLANG_ARCHIVE_TYPE_RIFF_LZ4,
        SLANG_ARCHIVE_TYPE_COUNT_OF,
    }

    public enum SlangCompile
    {
        SLANG_COMPILE_FLAG_NO_MANGLING = 1 << 3,
        SLANG_COMPILE_FLAG_NO_CODEGEN = 1 << 4,
        SLANG_COMPILE_FLAG_OBFUSCATE = 1 << 5,
        SLANG_COMPILE_FLAG_NO_CHECKING = 0,
        SLANG_COMPILE_FLAG_SPLIT_MIXED_TYPES = 0,
    }

    public enum SlangTarget
    {
        SLANG_TARGET_FLAG_PARAMETER_BLOCKS_USE_REGISTER_SPACES = 1 << 4,
        SLANG_TARGET_FLAG_GENERATE_WHOLE_PROGRAM = 1 << 8,
        SLANG_TARGET_FLAG_DUMP_IR = 1 << 9,
        SLANG_TARGET_FLAG_GENERATE_SPIRV_DIRECTLY = 1 << 10,
    }

    [NativeTypeName("SlangFloatingPointModeIntegral")]
    public enum SlangFloatingPointMode : uint
    {
        SLANG_FLOATING_POINT_MODE_DEFAULT = 0,
        SLANG_FLOATING_POINT_MODE_FAST,
        SLANG_FLOATING_POINT_MODE_PRECISE,
    }

    [NativeTypeName("SlangFpDenormalModeIntegral")]
    public enum SlangFpDenormalMode : uint
    {
        SLANG_FP_DENORM_MODE_ANY = 0,
        SLANG_FP_DENORM_MODE_PRESERVE,
        SLANG_FP_DENORM_MODE_FTZ,
    }

    [NativeTypeName("SlangLineDirectiveModeIntegral")]
    public enum SlangLineDirectiveMode : uint
    {
        SLANG_LINE_DIRECTIVE_MODE_DEFAULT = 0,
        SLANG_LINE_DIRECTIVE_MODE_NONE,
        SLANG_LINE_DIRECTIVE_MODE_STANDARD,
        SLANG_LINE_DIRECTIVE_MODE_GLSL,
        SLANG_LINE_DIRECTIVE_MODE_SOURCE_MAP,
    }

    [NativeTypeName("SlangSourceLanguageIntegral")]
    public enum SlangSourceLanguage
    {
        SLANG_SOURCE_LANGUAGE_UNKNOWN,
        SLANG_SOURCE_LANGUAGE_SLANG,
        SLANG_SOURCE_LANGUAGE_HLSL,
        SLANG_SOURCE_LANGUAGE_GLSL,
        SLANG_SOURCE_LANGUAGE_C,
        SLANG_SOURCE_LANGUAGE_CPP,
        SLANG_SOURCE_LANGUAGE_CUDA,
        SLANG_SOURCE_LANGUAGE_SPIRV,
        SLANG_SOURCE_LANGUAGE_METAL,
        SLANG_SOURCE_LANGUAGE_WGSL,
        SLANG_SOURCE_LANGUAGE_LLVM,
        SLANG_SOURCE_LANGUAGE_COUNT_OF,
    }

    [NativeTypeName("SlangProfileIDIntegral")]
    public enum SlangProfileID : uint
    {
        SLANG_PROFILE_UNKNOWN,
    }

    [NativeTypeName("SlangCapabilityIDIntegral")]
    public enum SlangCapabilityID
    {
        SLANG_CAPABILITY_UNKNOWN = 0,
    }

    [NativeTypeName("SlangMatrixLayoutModeIntegral")]
    public enum SlangMatrixLayoutMode : uint
    {
        SLANG_MATRIX_LAYOUT_MODE_UNKNOWN = 0,
        SLANG_MATRIX_LAYOUT_ROW_MAJOR,
        SLANG_MATRIX_LAYOUT_COLUMN_MAJOR,
    }

    [NativeTypeName("SlangStageIntegral")]
    public enum SlangStage : uint
    {
        SLANG_STAGE_NONE,
        SLANG_STAGE_VERTEX,
        SLANG_STAGE_HULL,
        SLANG_STAGE_DOMAIN,
        SLANG_STAGE_GEOMETRY,
        SLANG_STAGE_FRAGMENT,
        SLANG_STAGE_COMPUTE,
        SLANG_STAGE_RAY_GENERATION,
        SLANG_STAGE_INTERSECTION,
        SLANG_STAGE_ANY_HIT,
        SLANG_STAGE_CLOSEST_HIT,
        SLANG_STAGE_MISS,
        SLANG_STAGE_CALLABLE,
        SLANG_STAGE_MESH,
        SLANG_STAGE_AMPLIFICATION,
        SLANG_STAGE_DISPATCH,
        SLANG_STAGE_COUNT,
        SLANG_STAGE_PIXEL = SLANG_STAGE_FRAGMENT,
    }

    [NativeTypeName("SlangDebugInfoLevelIntegral")]
    public enum SlangDebugInfoLevel : uint
    {
        SLANG_DEBUG_INFO_LEVEL_NONE = 0,
        SLANG_DEBUG_INFO_LEVEL_MINIMAL,
        SLANG_DEBUG_INFO_LEVEL_STANDARD,
        SLANG_DEBUG_INFO_LEVEL_MAXIMAL,
    }

    [NativeTypeName("SlangDebugInfoFormatIntegral")]
    public enum SlangDebugInfoFormat : uint
    {
        SLANG_DEBUG_INFO_FORMAT_DEFAULT,
        SLANG_DEBUG_INFO_FORMAT_C7,
        SLANG_DEBUG_INFO_FORMAT_PDB,
        SLANG_DEBUG_INFO_FORMAT_STABS,
        SLANG_DEBUG_INFO_FORMAT_COFF,
        SLANG_DEBUG_INFO_FORMAT_DWARF,
        SLANG_DEBUG_INFO_FORMAT_COUNT_OF,
    }

    [NativeTypeName("SlangOptimizationLevelIntegral")]
    public enum SlangOptimizationLevel : uint
    {
        SLANG_OPTIMIZATION_LEVEL_NONE = 0,
        SLANG_OPTIMIZATION_LEVEL_DEFAULT,
        SLANG_OPTIMIZATION_LEVEL_HIGH,
        SLANG_OPTIMIZATION_LEVEL_MAXIMAL,
    }

    public enum SlangEmitSpirvMethod
    {
        SLANG_EMIT_SPIRV_DEFAULT = 0,
        SLANG_EMIT_SPIRV_VIA_GLSL,
        SLANG_EMIT_SPIRV_DIRECTLY,
    }

    public enum SlangEmitCPUMethod
    {
        SLANG_EMIT_CPU_DEFAULT = 0,
        SLANG_EMIT_CPU_VIA_CPP,
        SLANG_EMIT_CPU_VIA_LLVM,
    }

    public enum SlangDiagnosticColor
    {
        SLANG_DIAGNOSTIC_COLOR_AUTO = 0,
        SLANG_DIAGNOSTIC_COLOR_ALWAYS,
        SLANG_DIAGNOSTIC_COLOR_NEVER,
    }

    public enum CompilerOptionName
    {
        MacroDefine,
        DepFile,
        EntryPointName,
        Specialize,
        Help,
        HelpStyle,
        Include,
        Language,
        MatrixLayoutColumn,
        MatrixLayoutRow,
        ZeroInitialize,
        IgnoreCapabilities,
        RestrictiveCapabilityCheck,
        ModuleName,
        Output,
        Profile,
        Stage,
        Target,
        Version,
        WarningsAsErrors,
        DisableWarnings,
        EnableWarning,
        DisableWarning,
        DumpWarningDiagnostics,
        InputFilesRemain,
        EmitIr,
        ReportDownstreamTime,
        ReportPerfBenchmark,
        ReportCheckpointIntermediates,
        SkipSPIRVValidation,
        SourceEmbedStyle,
        SourceEmbedName,
        SourceEmbedLanguage,
        DisableShortCircuit,
        MinimumSlangOptimization,
        DisableNonEssentialValidations,
        DisableSourceMap,
        UnscopedEnum,
        PreserveParameters,
        Capability,
        DefaultImageFormatUnknown,
        DisableDynamicDispatch,
        DisableSpecialization,
        FloatingPointMode,
        DebugInformation,
        LineDirectiveMode,
        Optimization,
        Obfuscate,
        VulkanBindShift,
        VulkanBindGlobals,
        VulkanInvertY,
        VulkanUseDxPositionW,
        VulkanUseEntryPointName,
        VulkanUseGLLayout,
        VulkanEmitReflection,
        GLSLForceScalarLayout,
        EnableEffectAnnotations,
        EmitSpirvViaGLSL,
        EmitSpirvDirectly,
        SPIRVCoreGrammarJSON,
        IncompleteLibrary,
        CompilerPath,
        DefaultDownstreamCompiler,
        DownstreamArgs,
        PassThrough,
        DumpRepro,
        DumpReproOnError,
        ExtractRepro,
        LoadRepro,
        LoadReproDirectory,
        ReproFallbackDirectory,
        DumpAst,
        DumpIntermediatePrefix,
        DumpIntermediates,
        DumpIr,
        DumpIrIds,
        PreprocessorOutput,
        OutputIncludes,
        ReproFileSystem,
        REMOVED_SerialIR,
        SkipCodeGen,
        ValidateIr,
        VerbosePaths,
        VerifyDebugSerialIr,
        NoCodeGen,
        FileSystem,
        Heterogeneous,
        NoMangle,
        NoHLSLBinding,
        NoHLSLPackConstantBufferElements,
        ValidateUniformity,
        AllowGLSL,
        EnableExperimentalPasses,
        BindlessSpaceIndex,
        SPIRVResourceHeapStride,
        SPIRVSamplerHeapStride,
        ArchiveType,
        CompileCoreModule,
        Doc,
        IrCompression,
        LoadCoreModule,
        ReferenceModule,
        SaveCoreModule,
        SaveCoreModuleBinSource,
        TrackLiveness,
        LoopInversion,
        ParameterBlocksUseRegisterSpaces,
        LanguageVersion,
        TypeConformance,
        EnableExperimentalDynamicDispatch,
        EmitReflectionJSON,
        CountOfParsableOptions,
        DebugInformationFormat,
        VulkanBindShiftAll,
        GenerateWholeProgram,
        UseUpToDateBinaryModule,
        EmbedDownstreamIR,
        ForceDXLayout,
        EmitSpirvMethod,
        SaveGLSLModuleBinSource,
        SkipDownstreamLinking,
        DumpModule,
        GetModuleInfo,
        GetSupportedModuleVersions,
        EmitSeparateDebug,
        DenormalModeFp16,
        DenormalModeFp32,
        DenormalModeFp64,
        UseMSVCStyleBitfieldPacking,
        ForceCLayout,
        ExperimentalFeature,
        ReportDetailedPerfBenchmark,
        ValidateIRDetailed,
        DumpIRBefore,
        DumpIRAfter,
        EmitCPUMethod,
        EmitCPUViaCPP,
        EmitCPUViaLLVM,
        LLVMTargetTriple,
        LLVMCPU,
        LLVMFeatures,
        EnableRichDiagnostics,
        ReportDynamicDispatchSites,
        EnableMachineReadableDiagnostics,
        DiagnosticColor,
        CountOf,
    }

    public enum CompilerOptionValueKind
    {
        Int,
        String,
    }

    public unsafe partial struct CompilerOptionValue
    {
        [NativeTypeName("slang::CompilerOptionValueKind")]
        public CompilerOptionValueKind kind;

        [NativeTypeName("int32_t")]
        public int intValue0;

        [NativeTypeName("int32_t")]
        public int intValue1;

        [NativeTypeName("const char *")]
        public sbyte* stringValue0;

        [NativeTypeName("const char *")]
        public sbyte* stringValue1;
    }

    public partial struct CompilerOptionEntry
    {
        [NativeTypeName("slang::CompilerOptionName")]
        public CompilerOptionName name;

        [NativeTypeName("slang::CompilerOptionValue")]
        public CompilerOptionValue value;
    }

    public partial struct SlangUUID
    {
        [NativeTypeName("uint32_t")]
        public uint data1;

        [NativeTypeName("uint16_t")]
        public ushort data2;

        [NativeTypeName("uint16_t")]
        public ushort data3;

        [NativeTypeName("uint8_t[8]")]
        public _data4_e__FixedBuffer data4;

        [InlineArray(8)]
        public partial struct _data4_e__FixedBuffer
        {
            public byte e0;
        }
    }

    public unsafe partial struct ISlangUnknown
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangUnknown*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangUnknown*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangUnknown*, uint>)(lpVtbl[1]))((ISlangUnknown*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangUnknown*, uint>)(lpVtbl[2]))((ISlangUnknown*)Unsafe.AsPointer(ref this));
        }
    }

    [NativeTypeName("struct ISlangCastable : ISlangUnknown")]
    public unsafe partial struct ISlangCastable
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangCastable*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangCastable*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangCastable*, uint>)(lpVtbl[1]))((ISlangCastable*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangCastable*, uint>)(lpVtbl[2]))((ISlangCastable*)Unsafe.AsPointer(ref this));
        }

        public void* castAs([NativeTypeName("const SlangUUID &")] SlangUUID* guid)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangCastable*, SlangUUID*, void*>)(lpVtbl[3]))((ISlangCastable*)Unsafe.AsPointer(ref this), guid);
        }
    }

    [NativeTypeName("struct ISlangClonable : ISlangCastable")]
    public unsafe partial struct ISlangClonable
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangClonable*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangClonable*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangClonable*, uint>)(lpVtbl[1]))((ISlangClonable*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangClonable*, uint>)(lpVtbl[2]))((ISlangClonable*)Unsafe.AsPointer(ref this));
        }

        public void* castAs([NativeTypeName("const SlangUUID &")] SlangUUID* guid)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangClonable*, SlangUUID*, void*>)(lpVtbl[3]))((ISlangClonable*)Unsafe.AsPointer(ref this), guid);
        }

        public void* clone([NativeTypeName("const SlangUUID &")] SlangUUID* guid)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangClonable*, SlangUUID*, void*>)(lpVtbl[4]))((ISlangClonable*)Unsafe.AsPointer(ref this), guid);
        }
    }

    [NativeTypeName("struct ISlangBlob : ISlangUnknown")]
    public unsafe partial struct ISlangBlob
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangBlob*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangBlob*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangBlob*, uint>)(lpVtbl[1]))((ISlangBlob*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangBlob*, uint>)(lpVtbl[2]))((ISlangBlob*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("const void *")]
        public void* getBufferPointer()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangBlob*, void*>)(lpVtbl[3]))((ISlangBlob*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("size_t")]
        public nuint getBufferSize()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangBlob*, nuint>)(lpVtbl[4]))((ISlangBlob*)Unsafe.AsPointer(ref this));
        }
    }

    public unsafe partial struct SlangTerminatedChars
    {
        [NativeTypeName("char[1]")]
        public _chars_e__FixedBuffer chars;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0xbe0db1a8,
                data2 = 0x3594,
                data3 = 0x4603,
                data4 = new byte[8]
                {
                    0xa7,
                    0x8b,
                    0xc4,
                    0x86,
                    0x84,
                    0x30,
                    0xdf,
                    0xbb,
                },
            };
        }

        [return: NativeTypeName("const char *")]
        public readonly sbyte* ToSBytePointer()
        {
            return chars;
        }

        public partial struct _chars_e__FixedBuffer
        {
            public sbyte e0;

            [UnscopedRef]
            public ref sbyte this[int index]
            {
                get
                {
                    return ref Unsafe.Add(ref e0, index);
                }
            }

            [UnscopedRef]
            public Span<sbyte> AsSpan(int length) => MemoryMarshal.CreateSpan(ref e0, length);
        }
    }

    [NativeTypeName("struct ISlangFileSystem : ISlangCastable")]
    public unsafe partial struct ISlangFileSystem
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystem*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangFileSystem*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystem*, uint>)(lpVtbl[1]))((ISlangFileSystem*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystem*, uint>)(lpVtbl[2]))((ISlangFileSystem*)Unsafe.AsPointer(ref this));
        }

        public void* castAs([NativeTypeName("const SlangUUID &")] SlangUUID* guid)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystem*, SlangUUID*, void*>)(lpVtbl[3]))((ISlangFileSystem*)Unsafe.AsPointer(ref this), guid);
        }

        [return: NativeTypeName("SlangResult")]
        public int loadFile([NativeTypeName("const char *")] sbyte* path, ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystem*, sbyte*, ISlangBlob**, int>)(lpVtbl[4]))((ISlangFileSystem*)Unsafe.AsPointer(ref this), path, outBlob);
        }
    }

    [NativeTypeName("struct ISlangSharedLibrary_Dep1 : ISlangUnknown")]
    public unsafe partial struct ISlangSharedLibrary_Dep1
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibrary_Dep1*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangSharedLibrary_Dep1*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibrary_Dep1*, uint>)(lpVtbl[1]))((ISlangSharedLibrary_Dep1*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibrary_Dep1*, uint>)(lpVtbl[2]))((ISlangSharedLibrary_Dep1*)Unsafe.AsPointer(ref this));
        }

        public void* findSymbolAddressByName([NativeTypeName("const char *")] sbyte* name)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibrary_Dep1*, sbyte*, void*>)(lpVtbl[3]))((ISlangSharedLibrary_Dep1*)Unsafe.AsPointer(ref this), name);
        }
    }

    [NativeTypeName("struct ISlangSharedLibrary : ISlangCastable")]
    public unsafe partial struct ISlangSharedLibrary
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangFuncPtr")]
        public delegate* unmanaged[Thiscall]<ISlangSharedLibrary*, void> findFuncByName([NativeTypeName("const char *")] sbyte* name)
        {
            return (delegate* unmanaged[Cdecl]<void>)(findSymbolAddressByName(name));
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibrary*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangSharedLibrary*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibrary*, uint>)(lpVtbl[1]))((ISlangSharedLibrary*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibrary*, uint>)(lpVtbl[2]))((ISlangSharedLibrary*)Unsafe.AsPointer(ref this));
        }

        public void* castAs([NativeTypeName("const SlangUUID &")] SlangUUID* guid)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibrary*, SlangUUID*, void*>)(lpVtbl[3]))((ISlangSharedLibrary*)Unsafe.AsPointer(ref this), guid);
        }

        public void* findSymbolAddressByName([NativeTypeName("const char *")] sbyte* name)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibrary*, sbyte*, void*>)(lpVtbl[4]))((ISlangSharedLibrary*)Unsafe.AsPointer(ref this), name);
        }
    }

    [NativeTypeName("struct ISlangSharedLibraryLoader : ISlangUnknown")]
    public unsafe partial struct ISlangSharedLibraryLoader
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibraryLoader*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangSharedLibraryLoader*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibraryLoader*, uint>)(lpVtbl[1]))((ISlangSharedLibraryLoader*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibraryLoader*, uint>)(lpVtbl[2]))((ISlangSharedLibraryLoader*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int loadSharedLibrary([NativeTypeName("const char *")] sbyte* path, ISlangSharedLibrary** sharedLibraryOut)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangSharedLibraryLoader*, sbyte*, ISlangSharedLibrary**, int>)(lpVtbl[3]))((ISlangSharedLibraryLoader*)Unsafe.AsPointer(ref this), path, sharedLibraryOut);
        }
    }

    [NativeTypeName("SlangPathTypeIntegral")]
    public enum SlangPathType : uint
    {
        SLANG_PATH_TYPE_DIRECTORY,
        SLANG_PATH_TYPE_FILE,
    }

    [NativeTypeName("uint8_t")]
    public enum OSPathKind : byte
    {
        None,
        Direct,
        OperatingSystem,
    }

    public enum PathKind
    {
        Simplified,
        Canonical,
        Display,
        OperatingSystem,
        CountOf,
    }

    [NativeTypeName("struct ISlangFileSystemExt : ISlangFileSystem")]
    public unsafe partial struct ISlangFileSystemExt
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, uint>)(lpVtbl[1]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, uint>)(lpVtbl[2]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this));
        }

        public void* castAs([NativeTypeName("const SlangUUID &")] SlangUUID* guid)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, SlangUUID*, void*>)(lpVtbl[3]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this), guid);
        }

        [return: NativeTypeName("SlangResult")]
        public int loadFile([NativeTypeName("const char *")] sbyte* path, ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, sbyte*, ISlangBlob**, int>)(lpVtbl[4]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this), path, outBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int getFileUniqueIdentity([NativeTypeName("const char *")] sbyte* path, ISlangBlob** outUniqueIdentity)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, sbyte*, ISlangBlob**, int>)(lpVtbl[5]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this), path, outUniqueIdentity);
        }

        [return: NativeTypeName("SlangResult")]
        public int calcCombinedPath(SlangPathType fromPathType, [NativeTypeName("const char *")] sbyte* fromPath, [NativeTypeName("const char *")] sbyte* path, ISlangBlob** pathOut)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, SlangPathType, sbyte*, sbyte*, ISlangBlob**, int>)(lpVtbl[6]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this), fromPathType, fromPath, path, pathOut);
        }

        [return: NativeTypeName("SlangResult")]
        public int getPathType([NativeTypeName("const char *")] sbyte* path, SlangPathType* pathTypeOut)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, sbyte*, SlangPathType*, int>)(lpVtbl[7]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this), path, pathTypeOut);
        }

        [return: NativeTypeName("SlangResult")]
        public int getPath(PathKind kind, [NativeTypeName("const char *")] sbyte* path, ISlangBlob** outPath)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, PathKind, sbyte*, ISlangBlob**, int>)(lpVtbl[8]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this), kind, path, outPath);
        }

        public void clearCache()
        {
            ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, void>)(lpVtbl[9]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int enumeratePathContents([NativeTypeName("const char *")] sbyte* path, [NativeTypeName("FileSystemContentsCallBack")] delegate* unmanaged[Cdecl]<SlangPathType, sbyte*, void*, void> callback, void* userData)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, sbyte*, delegate* unmanaged[Thiscall]<SlangPathType, sbyte*, void*, void>, void*, int>)(lpVtbl[10]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this), path, callback, userData);
        }

        public OSPathKind getOSPathKind()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangFileSystemExt*, OSPathKind>)(lpVtbl[11]))((ISlangFileSystemExt*)Unsafe.AsPointer(ref this));
        }
    }

    [NativeTypeName("struct ISlangMutableFileSystem : ISlangFileSystemExt")]
    public unsafe partial struct ISlangMutableFileSystem
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, uint>)(lpVtbl[1]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, uint>)(lpVtbl[2]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this));
        }

        public void* castAs([NativeTypeName("const SlangUUID &")] SlangUUID* guid)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, SlangUUID*, void*>)(lpVtbl[3]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), guid);
        }

        [return: NativeTypeName("SlangResult")]
        public int loadFile([NativeTypeName("const char *")] sbyte* path, ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, sbyte*, ISlangBlob**, int>)(lpVtbl[4]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), path, outBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int getFileUniqueIdentity([NativeTypeName("const char *")] sbyte* path, ISlangBlob** outUniqueIdentity)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, sbyte*, ISlangBlob**, int>)(lpVtbl[5]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), path, outUniqueIdentity);
        }

        [return: NativeTypeName("SlangResult")]
        public int calcCombinedPath(SlangPathType fromPathType, [NativeTypeName("const char *")] sbyte* fromPath, [NativeTypeName("const char *")] sbyte* path, ISlangBlob** pathOut)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, SlangPathType, sbyte*, sbyte*, ISlangBlob**, int>)(lpVtbl[6]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), fromPathType, fromPath, path, pathOut);
        }

        [return: NativeTypeName("SlangResult")]
        public int getPathType([NativeTypeName("const char *")] sbyte* path, SlangPathType* pathTypeOut)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, sbyte*, SlangPathType*, int>)(lpVtbl[7]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), path, pathTypeOut);
        }

        [return: NativeTypeName("SlangResult")]
        public int getPath(PathKind kind, [NativeTypeName("const char *")] sbyte* path, ISlangBlob** outPath)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, PathKind, sbyte*, ISlangBlob**, int>)(lpVtbl[8]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), kind, path, outPath);
        }

        public void clearCache()
        {
            ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, void>)(lpVtbl[9]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int enumeratePathContents([NativeTypeName("const char *")] sbyte* path, [NativeTypeName("FileSystemContentsCallBack")] delegate* unmanaged[Cdecl]<SlangPathType, sbyte*, void*, void> callback, void* userData)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, sbyte*, delegate* unmanaged[Thiscall]<SlangPathType, sbyte*, void*, void>, void*, int>)(lpVtbl[10]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), path, callback, userData);
        }

        public OSPathKind getOSPathKind()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, OSPathKind>)(lpVtbl[11]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int saveFile([NativeTypeName("const char *")] sbyte* path, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint size)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, sbyte*, void*, nuint, int>)(lpVtbl[12]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), path, data, size);
        }

        [return: NativeTypeName("SlangResult")]
        public int saveFileBlob([NativeTypeName("const char *")] sbyte* path, ISlangBlob* dataBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, sbyte*, ISlangBlob*, int>)(lpVtbl[13]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), path, dataBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int remove([NativeTypeName("const char *")] sbyte* path)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, sbyte*, int>)(lpVtbl[14]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), path);
        }

        [return: NativeTypeName("SlangResult")]
        public int createDirectory([NativeTypeName("const char *")] sbyte* path)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangMutableFileSystem*, sbyte*, int>)(lpVtbl[15]))((ISlangMutableFileSystem*)Unsafe.AsPointer(ref this), path);
        }
    }

    [NativeTypeName("SlangWriterChannelIntegral")]
    public enum SlangWriterChannel : uint
    {
        SLANG_WRITER_CHANNEL_DIAGNOSTIC,
        SLANG_WRITER_CHANNEL_STD_OUTPUT,
        SLANG_WRITER_CHANNEL_STD_ERROR,
        SLANG_WRITER_CHANNEL_COUNT_OF,
    }

    [NativeTypeName("SlangWriterModeIntegral")]
    public enum SlangWriterMode : uint
    {
        SLANG_WRITER_MODE_TEXT,
        SLANG_WRITER_MODE_BINARY,
    }

    [NativeTypeName("struct ISlangWriter : ISlangUnknown")]
    public unsafe partial struct ISlangWriter
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangWriter*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangWriter*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangWriter*, uint>)(lpVtbl[1]))((ISlangWriter*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangWriter*, uint>)(lpVtbl[2]))((ISlangWriter*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("char *")]
        public sbyte* beginAppendBuffer([NativeTypeName("size_t")] nuint maxNumChars)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangWriter*, nuint, sbyte*>)(lpVtbl[3]))((ISlangWriter*)Unsafe.AsPointer(ref this), maxNumChars);
        }

        [return: NativeTypeName("SlangResult")]
        public int endAppendBuffer([NativeTypeName("char *")] sbyte* buffer, [NativeTypeName("size_t")] nuint numChars)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangWriter*, sbyte*, nuint, int>)(lpVtbl[4]))((ISlangWriter*)Unsafe.AsPointer(ref this), buffer, numChars);
        }

        [return: NativeTypeName("SlangResult")]
        public int write([NativeTypeName("const char *")] sbyte* chars, [NativeTypeName("size_t")] nuint numChars)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangWriter*, sbyte*, nuint, int>)(lpVtbl[5]))((ISlangWriter*)Unsafe.AsPointer(ref this), chars, numChars);
        }

        public void flush()
        {
            ((delegate* unmanaged[Stdcall]<ISlangWriter*, void>)(lpVtbl[6]))((ISlangWriter*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangBool")]
        public bool isConsole()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangWriter*, byte>)(lpVtbl[7]))((ISlangWriter*)Unsafe.AsPointer(ref this)) != 0;
        }

        [return: NativeTypeName("SlangResult")]
        public int setMode(SlangWriterMode mode)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangWriter*, SlangWriterMode, int>)(lpVtbl[8]))((ISlangWriter*)Unsafe.AsPointer(ref this), mode);
        }
    }

    [NativeTypeName("struct ISlangProfiler : ISlangUnknown")]
    public unsafe partial struct ISlangProfiler
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangProfiler*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISlangProfiler*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangProfiler*, uint>)(lpVtbl[1]))((ISlangProfiler*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangProfiler*, uint>)(lpVtbl[2]))((ISlangProfiler*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("size_t")]
        public nuint getEntryCount()
        {
            return ((delegate* unmanaged[Stdcall]<ISlangProfiler*, nuint>)(lpVtbl[3]))((ISlangProfiler*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getEntryName([NativeTypeName("uint32_t")] uint index)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangProfiler*, uint, sbyte*>)(lpVtbl[4]))((ISlangProfiler*)Unsafe.AsPointer(ref this), index);
        }

        [return: NativeTypeName("long")]
        public int getEntryTimeMS([NativeTypeName("uint32_t")] uint index)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangProfiler*, uint, int>)(lpVtbl[5]))((ISlangProfiler*)Unsafe.AsPointer(ref this), index);
        }

        [return: NativeTypeName("uint32_t")]
        public uint getEntryInvocationTimes([NativeTypeName("uint32_t")] uint index)
        {
            return ((delegate* unmanaged[Stdcall]<ISlangProfiler*, uint, uint>)(lpVtbl[6]))((ISlangProfiler*)Unsafe.AsPointer(ref this), index);
        }
    }

    public partial struct SlangProgramLayout
    {
    }

    public partial struct SlangEntryPoint
    {
    }

    public partial struct SlangEntryPointLayout
    {
    }

    public partial struct SlangReflectionDecl
    {
    }

    public partial struct SlangReflectionModifier
    {
    }

    public partial struct SlangReflectionType
    {
    }

    public partial struct SlangReflectionTypeLayout
    {
    }

    public partial struct SlangReflectionVariable
    {
    }

    public partial struct SlangReflectionVariableLayout
    {
    }

    public partial struct SlangReflectionTypeParameter
    {
    }

    public partial struct SlangReflectionUserAttribute
    {
    }

    public partial struct SlangReflectionFunction
    {
    }

    public partial struct SlangReflectionGeneric
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct SlangReflectionGenericArg
    {
        [FieldOffset(0)]
        public SlangReflectionType* typeVal;

        [FieldOffset(0)]
        [NativeTypeName("int64_t")]
        public long intVal;

        [FieldOffset(0)]
        [NativeTypeName("bool")]
        public byte boolVal;
    }

    public enum SlangReflectionGenericArgType
    {
        SLANG_GENERIC_ARG_TYPE = 0,
        SLANG_GENERIC_ARG_INT = 1,
        SLANG_GENERIC_ARG_BOOL = 2,
    }

    [NativeTypeName("SlangTypeKindIntegral")]
    public enum SlangTypeKind : uint
    {
        SLANG_TYPE_KIND_NONE,
        SLANG_TYPE_KIND_STRUCT,
        SLANG_TYPE_KIND_ARRAY,
        SLANG_TYPE_KIND_MATRIX,
        SLANG_TYPE_KIND_VECTOR,
        SLANG_TYPE_KIND_SCALAR,
        SLANG_TYPE_KIND_CONSTANT_BUFFER,
        SLANG_TYPE_KIND_RESOURCE,
        SLANG_TYPE_KIND_SAMPLER_STATE,
        SLANG_TYPE_KIND_TEXTURE_BUFFER,
        SLANG_TYPE_KIND_SHADER_STORAGE_BUFFER,
        SLANG_TYPE_KIND_PARAMETER_BLOCK,
        SLANG_TYPE_KIND_GENERIC_TYPE_PARAMETER,
        SLANG_TYPE_KIND_INTERFACE,
        SLANG_TYPE_KIND_OUTPUT_STREAM,
        SLANG_TYPE_KIND_MESH_OUTPUT,
        SLANG_TYPE_KIND_SPECIALIZED,
        SLANG_TYPE_KIND_FEEDBACK,
        SLANG_TYPE_KIND_POINTER,
        SLANG_TYPE_KIND_DYNAMIC_RESOURCE,
        SLANG_TYPE_KIND_ENUM,
        SLANG_TYPE_KIND_COUNT,
    }

    [NativeTypeName("SlangScalarTypeIntegral")]
    public enum SlangScalarType : uint
    {
        SLANG_SCALAR_TYPE_NONE,
        SLANG_SCALAR_TYPE_VOID,
        SLANG_SCALAR_TYPE_BOOL,
        SLANG_SCALAR_TYPE_INT32,
        SLANG_SCALAR_TYPE_UINT32,
        SLANG_SCALAR_TYPE_INT64,
        SLANG_SCALAR_TYPE_UINT64,
        SLANG_SCALAR_TYPE_FLOAT16,
        SLANG_SCALAR_TYPE_FLOAT32,
        SLANG_SCALAR_TYPE_FLOAT64,
        SLANG_SCALAR_TYPE_INT8,
        SLANG_SCALAR_TYPE_UINT8,
        SLANG_SCALAR_TYPE_INT16,
        SLANG_SCALAR_TYPE_UINT16,
        SLANG_SCALAR_TYPE_INTPTR,
        SLANG_SCALAR_TYPE_UINTPTR,
    }

    [NativeTypeName("SlangDeclKindIntegral")]
    public enum SlangDeclKind : uint
    {
        SLANG_DECL_KIND_UNSUPPORTED_FOR_REFLECTION,
        SLANG_DECL_KIND_STRUCT,
        SLANG_DECL_KIND_FUNC,
        SLANG_DECL_KIND_MODULE,
        SLANG_DECL_KIND_GENERIC,
        SLANG_DECL_KIND_VARIABLE,
        SLANG_DECL_KIND_NAMESPACE,
        SLANG_DECL_KIND_ENUM,
    }

    [NativeTypeName("SlangResourceShapeIntegral")]
    public enum SlangResourceShape : uint
    {
        SLANG_RESOURCE_BASE_SHAPE_MASK = 0x0F,
        SLANG_RESOURCE_NONE = 0x00,
        SLANG_TEXTURE_1D = 0x01,
        SLANG_TEXTURE_2D = 0x02,
        SLANG_TEXTURE_3D = 0x03,
        SLANG_TEXTURE_CUBE = 0x04,
        SLANG_TEXTURE_BUFFER = 0x05,
        SLANG_STRUCTURED_BUFFER = 0x06,
        SLANG_BYTE_ADDRESS_BUFFER = 0x07,
        SLANG_RESOURCE_UNKNOWN = 0x08,
        SLANG_ACCELERATION_STRUCTURE = 0x09,
        SLANG_TEXTURE_SUBPASS = 0x0A,
        SLANG_RESOURCE_EXT_SHAPE_MASK = 0x1F0,
        SLANG_TEXTURE_FEEDBACK_FLAG = 0x10,
        SLANG_TEXTURE_SHADOW_FLAG = 0x20,
        SLANG_TEXTURE_ARRAY_FLAG = 0x40,
        SLANG_TEXTURE_MULTISAMPLE_FLAG = 0x80,
        SLANG_TEXTURE_COMBINED_FLAG = 0x100,
        SLANG_TEXTURE_1D_ARRAY = SLANG_TEXTURE_1D | SLANG_TEXTURE_ARRAY_FLAG,
        SLANG_TEXTURE_2D_ARRAY = SLANG_TEXTURE_2D | SLANG_TEXTURE_ARRAY_FLAG,
        SLANG_TEXTURE_CUBE_ARRAY = SLANG_TEXTURE_CUBE | SLANG_TEXTURE_ARRAY_FLAG,
        SLANG_TEXTURE_2D_MULTISAMPLE = SLANG_TEXTURE_2D | SLANG_TEXTURE_MULTISAMPLE_FLAG,
        SLANG_TEXTURE_2D_MULTISAMPLE_ARRAY = SLANG_TEXTURE_2D | SLANG_TEXTURE_MULTISAMPLE_FLAG | SLANG_TEXTURE_ARRAY_FLAG,
        SLANG_TEXTURE_SUBPASS_MULTISAMPLE = SLANG_TEXTURE_SUBPASS | SLANG_TEXTURE_MULTISAMPLE_FLAG,
    }

    [NativeTypeName("SlangResourceAccessIntegral")]
    public enum SlangResourceAccess : uint
    {
        SLANG_RESOURCE_ACCESS_NONE,
        SLANG_RESOURCE_ACCESS_READ,
        SLANG_RESOURCE_ACCESS_READ_WRITE,
        SLANG_RESOURCE_ACCESS_RASTER_ORDERED,
        SLANG_RESOURCE_ACCESS_APPEND,
        SLANG_RESOURCE_ACCESS_CONSUME,
        SLANG_RESOURCE_ACCESS_WRITE,
        SLANG_RESOURCE_ACCESS_FEEDBACK,
        SLANG_RESOURCE_ACCESS_UNKNOWN = 0x7FFFFFFF,
    }

    [NativeTypeName("SlangParameterCategoryIntegral")]
    public enum SlangParameterCategory : uint
    {
        SLANG_PARAMETER_CATEGORY_NONE,
        SLANG_PARAMETER_CATEGORY_MIXED,
        SLANG_PARAMETER_CATEGORY_CONSTANT_BUFFER,
        SLANG_PARAMETER_CATEGORY_SHADER_RESOURCE,
        SLANG_PARAMETER_CATEGORY_UNORDERED_ACCESS,
        SLANG_PARAMETER_CATEGORY_VARYING_INPUT,
        SLANG_PARAMETER_CATEGORY_VARYING_OUTPUT,
        SLANG_PARAMETER_CATEGORY_SAMPLER_STATE,
        SLANG_PARAMETER_CATEGORY_UNIFORM,
        SLANG_PARAMETER_CATEGORY_DESCRIPTOR_TABLE_SLOT,
        SLANG_PARAMETER_CATEGORY_SPECIALIZATION_CONSTANT,
        SLANG_PARAMETER_CATEGORY_PUSH_CONSTANT_BUFFER,
        SLANG_PARAMETER_CATEGORY_REGISTER_SPACE,
        SLANG_PARAMETER_CATEGORY_GENERIC,
        SLANG_PARAMETER_CATEGORY_RAY_PAYLOAD,
        SLANG_PARAMETER_CATEGORY_HIT_ATTRIBUTES,
        SLANG_PARAMETER_CATEGORY_CALLABLE_PAYLOAD,
        SLANG_PARAMETER_CATEGORY_SHADER_RECORD,
        SLANG_PARAMETER_CATEGORY_EXISTENTIAL_TYPE_PARAM,
        SLANG_PARAMETER_CATEGORY_EXISTENTIAL_OBJECT_PARAM,
        SLANG_PARAMETER_CATEGORY_SUB_ELEMENT_REGISTER_SPACE,
        SLANG_PARAMETER_CATEGORY_SUBPASS,
        SLANG_PARAMETER_CATEGORY_METAL_ARGUMENT_BUFFER_ELEMENT,
        SLANG_PARAMETER_CATEGORY_METAL_ATTRIBUTE,
        SLANG_PARAMETER_CATEGORY_METAL_PAYLOAD,
        SLANG_PARAMETER_CATEGORY_COUNT,
        SLANG_PARAMETER_CATEGORY_METAL_BUFFER = SLANG_PARAMETER_CATEGORY_CONSTANT_BUFFER,
        SLANG_PARAMETER_CATEGORY_METAL_TEXTURE = SLANG_PARAMETER_CATEGORY_SHADER_RESOURCE,
        SLANG_PARAMETER_CATEGORY_METAL_SAMPLER = SLANG_PARAMETER_CATEGORY_SAMPLER_STATE,
        SLANG_PARAMETER_CATEGORY_VERTEX_INPUT = SLANG_PARAMETER_CATEGORY_VARYING_INPUT,
        SLANG_PARAMETER_CATEGORY_FRAGMENT_OUTPUT = SLANG_PARAMETER_CATEGORY_VARYING_OUTPUT,
        SLANG_PARAMETER_CATEGORY_COUNT_V1 = SLANG_PARAMETER_CATEGORY_SUBPASS,
    }

    [NativeTypeName("SlangBindingTypeIntegral")]
    public enum SlangBindingType : uint
    {
        SLANG_BINDING_TYPE_UNKNOWN = 0,
        SLANG_BINDING_TYPE_SAMPLER,
        SLANG_BINDING_TYPE_TEXTURE,
        SLANG_BINDING_TYPE_CONSTANT_BUFFER,
        SLANG_BINDING_TYPE_PARAMETER_BLOCK,
        SLANG_BINDING_TYPE_TYPED_BUFFER,
        SLANG_BINDING_TYPE_RAW_BUFFER,
        SLANG_BINDING_TYPE_COMBINED_TEXTURE_SAMPLER,
        SLANG_BINDING_TYPE_INPUT_RENDER_TARGET,
        SLANG_BINDING_TYPE_INLINE_UNIFORM_DATA,
        SLANG_BINDING_TYPE_RAY_TRACING_ACCELERATION_STRUCTURE,
        SLANG_BINDING_TYPE_VARYING_INPUT,
        SLANG_BINDING_TYPE_VARYING_OUTPUT,
        SLANG_BINDING_TYPE_EXISTENTIAL_VALUE,
        SLANG_BINDING_TYPE_PUSH_CONSTANT,
        SLANG_BINDING_TYPE_MUTABLE_FLAG = 0x100,
        SLANG_BINDING_TYPE_MUTABLE_TETURE = SLANG_BINDING_TYPE_TEXTURE | SLANG_BINDING_TYPE_MUTABLE_FLAG,
        SLANG_BINDING_TYPE_MUTABLE_TYPED_BUFFER = SLANG_BINDING_TYPE_TYPED_BUFFER | SLANG_BINDING_TYPE_MUTABLE_FLAG,
        SLANG_BINDING_TYPE_MUTABLE_RAW_BUFFER = SLANG_BINDING_TYPE_RAW_BUFFER | SLANG_BINDING_TYPE_MUTABLE_FLAG,
        SLANG_BINDING_TYPE_BASE_MASK = 0x00FF,
        SLANG_BINDING_TYPE_EXT_MASK = 0xFF00,
    }

    [NativeTypeName("SlangLayoutRulesIntegral")]
    public enum SlangLayoutRules : uint
    {
        SLANG_LAYOUT_RULES_DEFAULT,
        SLANG_LAYOUT_RULES_METAL_ARGUMENT_BUFFER_TIER_2,
        SLANG_LAYOUT_RULES_DEFAULT_STRUCTURED_BUFFER,
        SLANG_LAYOUT_RULES_DEFAULT_CONSTANT_BUFFER,
    }

    [NativeTypeName("SlangModifierIDIntegral")]
    public enum SlangModifierID : uint
    {
        SLANG_MODIFIER_SHARED,
        SLANG_MODIFIER_NO_DIFF,
        SLANG_MODIFIER_STATIC,
        SLANG_MODIFIER_CONST,
        SLANG_MODIFIER_EXPORT,
        SLANG_MODIFIER_EXTERN,
        SLANG_MODIFIER_DIFFERENTIABLE,
        SLANG_MODIFIER_MUTATING,
        SLANG_MODIFIER_IN,
        SLANG_MODIFIER_OUT,
        SLANG_MODIFIER_INOUT,
    }

    [NativeTypeName("SlangImageFormatIntegral")]
    public enum SlangImageFormat : uint
    {
        SLANG_IMAGE_FORMAT_unknown,
        SLANG_IMAGE_FORMAT_rgba32f,
        SLANG_IMAGE_FORMAT_rgba16f,
        SLANG_IMAGE_FORMAT_rg32f,
        SLANG_IMAGE_FORMAT_rg16f,
        SLANG_IMAGE_FORMAT_r11f_g11f_b10f,
        SLANG_IMAGE_FORMAT_r32f,
        SLANG_IMAGE_FORMAT_r16f,
        SLANG_IMAGE_FORMAT_rgba16,
        SLANG_IMAGE_FORMAT_rgb10_a2,
        SLANG_IMAGE_FORMAT_rgba8,
        SLANG_IMAGE_FORMAT_rg16,
        SLANG_IMAGE_FORMAT_rg8,
        SLANG_IMAGE_FORMAT_r16,
        SLANG_IMAGE_FORMAT_r8,
        SLANG_IMAGE_FORMAT_rgba16_snorm,
        SLANG_IMAGE_FORMAT_rgba8_snorm,
        SLANG_IMAGE_FORMAT_rg16_snorm,
        SLANG_IMAGE_FORMAT_rg8_snorm,
        SLANG_IMAGE_FORMAT_r16_snorm,
        SLANG_IMAGE_FORMAT_r8_snorm,
        SLANG_IMAGE_FORMAT_rgba32i,
        SLANG_IMAGE_FORMAT_rgba16i,
        SLANG_IMAGE_FORMAT_rgba8i,
        SLANG_IMAGE_FORMAT_rg32i,
        SLANG_IMAGE_FORMAT_rg16i,
        SLANG_IMAGE_FORMAT_rg8i,
        SLANG_IMAGE_FORMAT_r32i,
        SLANG_IMAGE_FORMAT_r16i,
        SLANG_IMAGE_FORMAT_r8i,
        SLANG_IMAGE_FORMAT_rgba32ui,
        SLANG_IMAGE_FORMAT_rgba16ui,
        SLANG_IMAGE_FORMAT_rgb10_a2ui,
        SLANG_IMAGE_FORMAT_rgba8ui,
        SLANG_IMAGE_FORMAT_rg32ui,
        SLANG_IMAGE_FORMAT_rg16ui,
        SLANG_IMAGE_FORMAT_rg8ui,
        SLANG_IMAGE_FORMAT_r32ui,
        SLANG_IMAGE_FORMAT_r16ui,
        SLANG_IMAGE_FORMAT_r8ui,
        SLANG_IMAGE_FORMAT_r64ui,
        SLANG_IMAGE_FORMAT_r64i,
        SLANG_IMAGE_FORMAT_bgra8,
    }

    [NativeTypeName("struct ICompileRequest : ISlangUnknown")]
    public unsafe partial struct ICompileRequest
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangUUID*, void**, int>)(lpVtbl[0]))((ICompileRequest*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, uint>)(lpVtbl[1]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, uint>)(lpVtbl[2]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        public void setFileSystem(ISlangFileSystem* fileSystem)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, ISlangFileSystem*, void>)(lpVtbl[3]))((ICompileRequest*)Unsafe.AsPointer(ref this), fileSystem);
        }

        public void setCompileFlags([NativeTypeName("SlangCompileFlags")] uint flags)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, uint, void>)(lpVtbl[4]))((ICompileRequest*)Unsafe.AsPointer(ref this), flags);
        }

        [return: NativeTypeName("SlangCompileFlags")]
        public uint getCompileFlags()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, uint>)(lpVtbl[5]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        public void setDumpIntermediates(int enable)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, void>)(lpVtbl[6]))((ICompileRequest*)Unsafe.AsPointer(ref this), enable);
        }

        public void setDumpIntermediatePrefix([NativeTypeName("const char *")] sbyte* prefix)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, sbyte*, void>)(lpVtbl[7]))((ICompileRequest*)Unsafe.AsPointer(ref this), prefix);
        }

        public void setLineDirectiveMode(SlangLineDirectiveMode mode)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangLineDirectiveMode, void>)(lpVtbl[8]))((ICompileRequest*)Unsafe.AsPointer(ref this), mode);
        }

        public void setCodeGenTarget(SlangCompileTarget target)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangCompileTarget, void>)(lpVtbl[9]))((ICompileRequest*)Unsafe.AsPointer(ref this), target);
        }

        public int addCodeGenTarget(SlangCompileTarget target)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangCompileTarget, int>)(lpVtbl[10]))((ICompileRequest*)Unsafe.AsPointer(ref this), target);
        }

        public void setTargetProfile(int targetIndex, SlangProfileID profile)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, SlangProfileID, void>)(lpVtbl[11]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, profile);
        }

        public void setTargetFlags(int targetIndex, [NativeTypeName("SlangTargetFlags")] uint flags)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, uint, void>)(lpVtbl[12]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, flags);
        }

        public void setTargetFloatingPointMode(int targetIndex, SlangFloatingPointMode mode)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, SlangFloatingPointMode, void>)(lpVtbl[13]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, mode);
        }

        public void setTargetMatrixLayoutMode(int targetIndex, SlangMatrixLayoutMode mode)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, SlangMatrixLayoutMode, void>)(lpVtbl[14]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, mode);
        }

        public void setMatrixLayoutMode(SlangMatrixLayoutMode mode)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangMatrixLayoutMode, void>)(lpVtbl[15]))((ICompileRequest*)Unsafe.AsPointer(ref this), mode);
        }

        public void setDebugInfoLevel(SlangDebugInfoLevel level)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangDebugInfoLevel, void>)(lpVtbl[16]))((ICompileRequest*)Unsafe.AsPointer(ref this), level);
        }

        public void setOptimizationLevel(SlangOptimizationLevel level)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangOptimizationLevel, void>)(lpVtbl[17]))((ICompileRequest*)Unsafe.AsPointer(ref this), level);
        }

        public void setOutputContainerFormat(SlangContainerFormat format)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangContainerFormat, void>)(lpVtbl[18]))((ICompileRequest*)Unsafe.AsPointer(ref this), format);
        }

        public void setPassThrough(SlangPassThrough passThrough)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangPassThrough, void>)(lpVtbl[19]))((ICompileRequest*)Unsafe.AsPointer(ref this), passThrough);
        }

        public void setDiagnosticCallback([NativeTypeName("SlangDiagnosticCallback")] delegate* unmanaged[Cdecl]<sbyte*, void*, void> callback, [NativeTypeName("const void *")] void* userData)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, delegate* unmanaged[Thiscall]<sbyte*, void*, void>, void*, void>)(lpVtbl[20]))((ICompileRequest*)Unsafe.AsPointer(ref this), callback, userData);
        }

        public void setWriter(SlangWriterChannel channel, ISlangWriter* writer)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangWriterChannel, ISlangWriter*, void>)(lpVtbl[21]))((ICompileRequest*)Unsafe.AsPointer(ref this), channel, writer);
        }

        public ISlangWriter* getWriter(SlangWriterChannel channel)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangWriterChannel, ISlangWriter*>)(lpVtbl[22]))((ICompileRequest*)Unsafe.AsPointer(ref this), channel);
        }

        public void addSearchPath([NativeTypeName("const char *")] sbyte* searchDir)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, sbyte*, void>)(lpVtbl[23]))((ICompileRequest*)Unsafe.AsPointer(ref this), searchDir);
        }

        public void addPreprocessorDefine([NativeTypeName("const char *")] sbyte* key, [NativeTypeName("const char *")] sbyte* value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, sbyte*, sbyte*, void>)(lpVtbl[24]))((ICompileRequest*)Unsafe.AsPointer(ref this), key, value);
        }

        [return: NativeTypeName("SlangResult")]
        public int processCommandLineArguments([NativeTypeName("const char *const *")] sbyte** args, int argCount)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, sbyte**, int, int>)(lpVtbl[25]))((ICompileRequest*)Unsafe.AsPointer(ref this), args, argCount);
        }

        public int addTranslationUnit(SlangSourceLanguage language, [NativeTypeName("const char *")] sbyte* name)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangSourceLanguage, sbyte*, int>)(lpVtbl[26]))((ICompileRequest*)Unsafe.AsPointer(ref this), language, name);
        }

        public void setDefaultModuleName([NativeTypeName("const char *")] sbyte* defaultModuleName)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, sbyte*, void>)(lpVtbl[27]))((ICompileRequest*)Unsafe.AsPointer(ref this), defaultModuleName);
        }

        public void addTranslationUnitPreprocessorDefine(int translationUnitIndex, [NativeTypeName("const char *")] sbyte* key, [NativeTypeName("const char *")] sbyte* value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte*, sbyte*, void>)(lpVtbl[28]))((ICompileRequest*)Unsafe.AsPointer(ref this), translationUnitIndex, key, value);
        }

        public void addTranslationUnitSourceFile(int translationUnitIndex, [NativeTypeName("const char *")] sbyte* path)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte*, void>)(lpVtbl[29]))((ICompileRequest*)Unsafe.AsPointer(ref this), translationUnitIndex, path);
        }

        public void addTranslationUnitSourceString(int translationUnitIndex, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("const char *")] sbyte* source)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte*, sbyte*, void>)(lpVtbl[30]))((ICompileRequest*)Unsafe.AsPointer(ref this), translationUnitIndex, path, source);
        }

        [return: NativeTypeName("SlangResult")]
        public int addLibraryReference([NativeTypeName("const char *")] sbyte* basePath, [NativeTypeName("const void *")] void* libData, [NativeTypeName("size_t")] nuint libDataSize)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, sbyte*, void*, nuint, int>)(lpVtbl[31]))((ICompileRequest*)Unsafe.AsPointer(ref this), basePath, libData, libDataSize);
        }

        public void addTranslationUnitSourceStringSpan(int translationUnitIndex, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("const char *")] sbyte* sourceBegin, [NativeTypeName("const char *")] sbyte* sourceEnd)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte*, sbyte*, sbyte*, void>)(lpVtbl[32]))((ICompileRequest*)Unsafe.AsPointer(ref this), translationUnitIndex, path, sourceBegin, sourceEnd);
        }

        public void addTranslationUnitSourceBlob(int translationUnitIndex, [NativeTypeName("const char *")] sbyte* path, ISlangBlob* sourceBlob)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte*, ISlangBlob*, void>)(lpVtbl[33]))((ICompileRequest*)Unsafe.AsPointer(ref this), translationUnitIndex, path, sourceBlob);
        }

        public int addEntryPoint(int translationUnitIndex, [NativeTypeName("const char *")] sbyte* name, SlangStage stage)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte*, SlangStage, int>)(lpVtbl[34]))((ICompileRequest*)Unsafe.AsPointer(ref this), translationUnitIndex, name, stage);
        }

        public int addEntryPointEx(int translationUnitIndex, [NativeTypeName("const char *")] sbyte* name, SlangStage stage, int genericArgCount, [NativeTypeName("const char **")] sbyte** genericArgs)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte*, SlangStage, int, sbyte**, int>)(lpVtbl[35]))((ICompileRequest*)Unsafe.AsPointer(ref this), translationUnitIndex, name, stage, genericArgCount, genericArgs);
        }

        [return: NativeTypeName("SlangResult")]
        public int setGlobalGenericArgs(int genericArgCount, [NativeTypeName("const char **")] sbyte** genericArgs)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte**, int>)(lpVtbl[36]))((ICompileRequest*)Unsafe.AsPointer(ref this), genericArgCount, genericArgs);
        }

        [return: NativeTypeName("SlangResult")]
        public int setTypeNameForGlobalExistentialTypeParam(int slotIndex, [NativeTypeName("const char *")] sbyte* typeName)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte*, int>)(lpVtbl[37]))((ICompileRequest*)Unsafe.AsPointer(ref this), slotIndex, typeName);
        }

        [return: NativeTypeName("SlangResult")]
        public int setTypeNameForEntryPointExistentialTypeParam(int entryPointIndex, int slotIndex, [NativeTypeName("const char *")] sbyte* typeName)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, int, sbyte*, int>)(lpVtbl[38]))((ICompileRequest*)Unsafe.AsPointer(ref this), entryPointIndex, slotIndex, typeName);
        }

        public void setAllowGLSLInput([NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, byte, void>)(lpVtbl[39]))((ICompileRequest*)Unsafe.AsPointer(ref this), value);
        }

        [return: NativeTypeName("SlangResult")]
        public int compile()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int>)(lpVtbl[40]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getDiagnosticOutput()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, sbyte*>)(lpVtbl[41]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getDiagnosticOutputBlob(ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, ISlangBlob**, int>)(lpVtbl[42]))((ICompileRequest*)Unsafe.AsPointer(ref this), outBlob);
        }

        public int getDependencyFileCount()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int>)(lpVtbl[43]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getDependencyFilePath(int index)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte*>)(lpVtbl[44]))((ICompileRequest*)Unsafe.AsPointer(ref this), index);
        }

        public int getTranslationUnitCount()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int>)(lpVtbl[45]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getEntryPointSource(int entryPointIndex)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, sbyte*>)(lpVtbl[46]))((ICompileRequest*)Unsafe.AsPointer(ref this), entryPointIndex);
        }

        [return: NativeTypeName("const void *")]
        public void* getEntryPointCode(int entryPointIndex, [NativeTypeName("size_t *")] nuint* outSize)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, nuint*, void*>)(lpVtbl[47]))((ICompileRequest*)Unsafe.AsPointer(ref this), entryPointIndex, outSize);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointCodeBlob(int entryPointIndex, int targetIndex, ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, int, ISlangBlob**, int>)(lpVtbl[48]))((ICompileRequest*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointHostCallable(int entryPointIndex, int targetIndex, ISlangSharedLibrary** outSharedLibrary)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, int, ISlangSharedLibrary**, int>)(lpVtbl[49]))((ICompileRequest*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outSharedLibrary);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetCodeBlob(int targetIndex, ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, ISlangBlob**, int>)(lpVtbl[50]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, outBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetHostCallable(int targetIndex, ISlangSharedLibrary** outSharedLibrary)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, ISlangSharedLibrary**, int>)(lpVtbl[51]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, outSharedLibrary);
        }

        [return: NativeTypeName("const void *")]
        public void* getCompileRequestCode([NativeTypeName("size_t *")] nuint* outSize)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, nuint*, void*>)(lpVtbl[52]))((ICompileRequest*)Unsafe.AsPointer(ref this), outSize);
        }

        public ISlangMutableFileSystem* getCompileRequestResultAsFileSystem()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, ISlangMutableFileSystem*>)(lpVtbl[53]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getContainerCode(ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, ISlangBlob**, int>)(lpVtbl[54]))((ICompileRequest*)Unsafe.AsPointer(ref this), outBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int loadRepro(ISlangFileSystem* fileSystem, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint size)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, ISlangFileSystem*, void*, nuint, int>)(lpVtbl[55]))((ICompileRequest*)Unsafe.AsPointer(ref this), fileSystem, data, size);
        }

        [return: NativeTypeName("SlangResult")]
        public int saveRepro(ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, ISlangBlob**, int>)(lpVtbl[56]))((ICompileRequest*)Unsafe.AsPointer(ref this), outBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int enableReproCapture()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int>)(lpVtbl[57]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getProgram([NativeTypeName("slang::IComponentType **")] IComponentType** outProgram)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, IComponentType**, int>)(lpVtbl[58]))((ICompileRequest*)Unsafe.AsPointer(ref this), outProgram);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPoint([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("slang::IComponentType **")] IComponentType** outEntryPoint)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, long, IComponentType**, int>)(lpVtbl[59]))((ICompileRequest*)Unsafe.AsPointer(ref this), entryPointIndex, outEntryPoint);
        }

        [return: NativeTypeName("SlangResult")]
        public int getModule([NativeTypeName("SlangInt")] long translationUnitIndex, [NativeTypeName("slang::IModule **")] IModule** outModule)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, long, IModule**, int>)(lpVtbl[60]))((ICompileRequest*)Unsafe.AsPointer(ref this), translationUnitIndex, outModule);
        }

        [return: NativeTypeName("SlangResult")]
        public int getSession([NativeTypeName("slang::ISession **")] ISession** outSession)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, ISession**, int>)(lpVtbl[61]))((ICompileRequest*)Unsafe.AsPointer(ref this), outSession);
        }

        [return: NativeTypeName("SlangReflection *")]
        public SlangProgramLayout* getReflection()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangProgramLayout*>)(lpVtbl[62]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        public void setCommandLineCompilerMode()
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, void>)(lpVtbl[63]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int addTargetCapability([NativeTypeName("SlangInt")] long targetIndex, SlangCapabilityID capability)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, long, SlangCapabilityID, int>)(lpVtbl[64]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, capability);
        }

        [return: NativeTypeName("SlangResult")]
        public int getProgramWithEntryPoints([NativeTypeName("slang::IComponentType **")] IComponentType** outProgram)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, IComponentType**, int>)(lpVtbl[65]))((ICompileRequest*)Unsafe.AsPointer(ref this), outProgram);
        }

        [return: NativeTypeName("SlangResult")]
        public int isParameterLocationUsed([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, SlangParameterCategory category, [NativeTypeName("SlangUInt")] ulong spaceIndex, [NativeTypeName("SlangUInt")] ulong registerIndex, [NativeTypeName("bool &")] bool* outUsed)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, long, long, SlangParameterCategory, ulong, ulong, bool*, int>)(lpVtbl[66]))((ICompileRequest*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, category, spaceIndex, registerIndex, outUsed);
        }

        public void setTargetLineDirectiveMode([NativeTypeName("SlangInt")] long targetIndex, SlangLineDirectiveMode mode)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, long, SlangLineDirectiveMode, void>)(lpVtbl[67]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, mode);
        }

        public void setTargetForceGLSLScalarBufferLayout(int targetIndex, [NativeTypeName("bool")] byte forceScalarLayout)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, byte, void>)(lpVtbl[68]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, forceScalarLayout);
        }

        public void overrideDiagnosticSeverity([NativeTypeName("SlangInt")] long messageID, SlangSeverity overrideSeverity)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, long, SlangSeverity, void>)(lpVtbl[69]))((ICompileRequest*)Unsafe.AsPointer(ref this), messageID, overrideSeverity);
        }

        [return: NativeTypeName("SlangDiagnosticFlags")]
        public int getDiagnosticFlags()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, int>)(lpVtbl[70]))((ICompileRequest*)Unsafe.AsPointer(ref this));
        }

        public void setDiagnosticFlags([NativeTypeName("SlangDiagnosticFlags")] int flags)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, void>)(lpVtbl[71]))((ICompileRequest*)Unsafe.AsPointer(ref this), flags);
        }

        public void setDebugInfoFormat(SlangDebugInfoFormat debugFormat)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, SlangDebugInfoFormat, void>)(lpVtbl[72]))((ICompileRequest*)Unsafe.AsPointer(ref this), debugFormat);
        }

        public void setEnableEffectAnnotations([NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, byte, void>)(lpVtbl[73]))((ICompileRequest*)Unsafe.AsPointer(ref this), value);
        }

        public void setReportDownstreamTime([NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, byte, void>)(lpVtbl[74]))((ICompileRequest*)Unsafe.AsPointer(ref this), value);
        }

        public void setReportPerfBenchmark([NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, byte, void>)(lpVtbl[75]))((ICompileRequest*)Unsafe.AsPointer(ref this), value);
        }

        public void setSkipSPIRVValidation([NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, byte, void>)(lpVtbl[76]))((ICompileRequest*)Unsafe.AsPointer(ref this), value);
        }

        public void setTargetUseMinimumSlangOptimization(int targetIndex, [NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, byte, void>)(lpVtbl[77]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, value);
        }

        public void setIgnoreCapabilityCheck([NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, byte, void>)(lpVtbl[78]))((ICompileRequest*)Unsafe.AsPointer(ref this), value);
        }

        [return: NativeTypeName("SlangResult")]
        public int getCompileTimeProfile(ISlangProfiler** compileTimeProfile, [NativeTypeName("bool")] byte shouldClear)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileRequest*, ISlangProfiler**, byte, int>)(lpVtbl[79]))((ICompileRequest*)Unsafe.AsPointer(ref this), compileTimeProfile, shouldClear);
        }

        public void setTargetGenerateWholeProgram(int targetIndex, [NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, byte, void>)(lpVtbl[80]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, value);
        }

        public void setTargetForceDXLayout(int targetIndex, [NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, byte, void>)(lpVtbl[81]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, value);
        }

        public void setTargetEmbedDownstreamIR(int targetIndex, [NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, byte, void>)(lpVtbl[82]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, value);
        }

        public void setTargetForceCLayout(int targetIndex, [NativeTypeName("bool")] byte value)
        {
            ((delegate* unmanaged[Stdcall]<ICompileRequest*, int, byte, void>)(lpVtbl[83]))((ICompileRequest*)Unsafe.AsPointer(ref this), targetIndex, value);
        }
    }

    public partial struct BufferReflection
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct GenericArgReflection
    {
        [FieldOffset(0)]
        [NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* typeVal;

        [FieldOffset(0)]
        [NativeTypeName("int64_t")]
        public long intVal;

        [FieldOffset(0)]
        [NativeTypeName("bool")]
        public byte boolVal;
    }

    public unsafe partial struct Attribute
    {
        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            return spReflectionUserAttribute_GetName((SlangReflectionUserAttribute*)(this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint getArgumentCount()
        {
            return (uint)(spReflectionUserAttribute_GetArgumentCount((SlangReflectionUserAttribute*)(this)));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getArgumentType([NativeTypeName("uint32_t")] uint index)
        {
            return (TypeReflection*)(spReflectionUserAttribute_GetArgumentType((SlangReflectionUserAttribute*)(this), index));
        }

        [return: NativeTypeName("SlangResult")]
        public int getArgumentValueInt([NativeTypeName("uint32_t")] uint index, int* value)
        {
            return spReflectionUserAttribute_GetArgumentValueInt(unchecked((SlangReflectionUserAttribute*)(this)), index, value);
        }

        [return: NativeTypeName("SlangResult")]
        public int getArgumentValueFloat([NativeTypeName("uint32_t")] uint index, float* value)
        {
            return spReflectionUserAttribute_GetArgumentValueFloat(unchecked((SlangReflectionUserAttribute*)(this)), index, value);
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getArgumentValueString([NativeTypeName("uint32_t")] uint index, [NativeTypeName("size_t *")] nuint* outSize)
        {
            return spReflectionUserAttribute_GetArgumentValueString((SlangReflectionUserAttribute*)(this), index, outSize);
        }
    }

    public unsafe partial struct TypeReflection
    {
        [return: NativeTypeName("slang::TypeReflection::Kind")]
        public Kind getKind()
        {
            return (Kind)(spReflectionType_GetKind(unchecked((SlangReflectionType*)(this))));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getFieldCount()
        {
            return spReflectionType_GetFieldCount((SlangReflectionType*)(this));
        }

        [return: NativeTypeName("slang::VariableReflection *")]
        public VariableReflection* getFieldByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return (VariableReflection*)(spReflectionType_GetFieldByIndex((SlangReflectionType*)(this), index));
        }

        public bool isArray()
        {
            return getKind() == Array;
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* unwrapArray()
        {
            TypeReflection* type = this;

            while (type->isArray())
            {
                type = type->getElementType();
            }

            return type;
        }

        [return: NativeTypeName("size_t")]
        public nuint getElementCount([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection = null)
        {
            return spReflectionType_GetSpecializedElementCount((SlangReflectionType*)(this), reflection);
        }

        [return: NativeTypeName("size_t")]
        public nuint getTotalArrayElementCount()
        {
            if (!isArray())
            {
                return 0;
            }

            nuint result = 1;
            TypeReflection* type = this;

            for (; ; )
            {
                if (!type->isArray())
                {
                    return result;
                }

                nuint c = type->getElementCount();

                if (c == unchecked((~(nuint)(0)) - 1))
                {
                    return ((~(nuint)(0)) - 1);
                }

                if (c == unchecked(~(nuint)(0)))
                {
                    return (~(nuint)(0));
                }

                result *= c;
                type = type->getElementType();
            }
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getElementType()
        {
            return (TypeReflection*)(spReflectionType_GetElementType((SlangReflectionType*)(this)));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getRowCount()
        {
            return spReflectionType_GetRowCount((SlangReflectionType*)(this));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getColumnCount()
        {
            return spReflectionType_GetColumnCount((SlangReflectionType*)(this));
        }

        [return: NativeTypeName("slang::TypeReflection::ScalarType")]
        public ScalarType getScalarType()
        {
            return (ScalarType)(spReflectionType_GetScalarType(unchecked((SlangReflectionType*)(this))));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getResourceResultType()
        {
            return (TypeReflection*)(spReflectionType_GetResourceResultType((SlangReflectionType*)(this)));
        }

        public SlangResourceShape getResourceShape()
        {
            return spReflectionType_GetResourceShape(unchecked((SlangReflectionType*)(this)));
        }

        public SlangResourceAccess getResourceAccess()
        {
            return spReflectionType_GetResourceAccess(unchecked((SlangReflectionType*)(this)));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            return spReflectionType_GetName((SlangReflectionType*)(this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getFullName(ISlangBlob** outNameBlob)
        {
            return spReflectionType_GetFullName(unchecked((SlangReflectionType*)(this)), outNameBlob);
        }

        [return: NativeTypeName("unsigned int")]
        public uint getUserAttributeCount()
        {
            return spReflectionType_GetUserAttributeCount((SlangReflectionType*)(this));
        }

        [return: NativeTypeName("slang::UserAttribute *")]
        public Attribute* getUserAttributeByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return (Attribute*)(spReflectionType_GetUserAttribute((SlangReflectionType*)(this), index));
        }

        [return: NativeTypeName("slang::UserAttribute *")]
        public Attribute* findAttributeByName([NativeTypeName("const char *")] sbyte* name)
        {
            return (Attribute*)(spReflectionType_FindUserAttributeByName((SlangReflectionType*)(this), name));
        }

        [return: NativeTypeName("slang::UserAttribute *")]
        public Attribute* findUserAttributeByName([NativeTypeName("const char *")] sbyte* name)
        {
            return findAttributeByName(name);
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* applySpecializations([NativeTypeName("slang::GenericReflection *")] GenericReflection* generic)
        {
            return (TypeReflection*)(spReflectionType_applySpecializations((SlangReflectionType*)(this), (SlangReflectionGeneric*)(generic)));
        }

        [return: NativeTypeName("slang::GenericReflection *")]
        public GenericReflection* getGenericContainer()
        {
            return (GenericReflection*)(spReflectionType_GetGenericContainer((SlangReflectionType*)(this)));
        }

        public enum Kind
        {
            None = SLANG_TYPE_KIND_NONE,
            Struct = SLANG_TYPE_KIND_STRUCT,
            Array = SLANG_TYPE_KIND_ARRAY,
            Matrix = SLANG_TYPE_KIND_MATRIX,
            Vector = SLANG_TYPE_KIND_VECTOR,
            Scalar = SLANG_TYPE_KIND_SCALAR,
            ConstantBuffer = SLANG_TYPE_KIND_CONSTANT_BUFFER,
            Resource = SLANG_TYPE_KIND_RESOURCE,
            SamplerState = SLANG_TYPE_KIND_SAMPLER_STATE,
            TextureBuffer = SLANG_TYPE_KIND_TEXTURE_BUFFER,
            ShaderStorageBuffer = SLANG_TYPE_KIND_SHADER_STORAGE_BUFFER,
            ParameterBlock = SLANG_TYPE_KIND_PARAMETER_BLOCK,
            GenericTypeParameter = SLANG_TYPE_KIND_GENERIC_TYPE_PARAMETER,
            Interface = SLANG_TYPE_KIND_INTERFACE,
            OutputStream = SLANG_TYPE_KIND_OUTPUT_STREAM,
            Specialized = SLANG_TYPE_KIND_SPECIALIZED,
            Feedback = SLANG_TYPE_KIND_FEEDBACK,
            Pointer = SLANG_TYPE_KIND_POINTER,
            DynamicResource = SLANG_TYPE_KIND_DYNAMIC_RESOURCE,
            MeshOutput = SLANG_TYPE_KIND_MESH_OUTPUT,
            Enum = SLANG_TYPE_KIND_ENUM,
        }

        [NativeTypeName("SlangScalarTypeIntegral")]
        public enum ScalarType : uint
        {
            None = SLANG_SCALAR_TYPE_NONE,
            Void = SLANG_SCALAR_TYPE_VOID,
            Bool = SLANG_SCALAR_TYPE_BOOL,
            Int32 = SLANG_SCALAR_TYPE_INT32,
            UInt32 = SLANG_SCALAR_TYPE_UINT32,
            Int64 = SLANG_SCALAR_TYPE_INT64,
            UInt64 = SLANG_SCALAR_TYPE_UINT64,
            Float16 = SLANG_SCALAR_TYPE_FLOAT16,
            Float32 = SLANG_SCALAR_TYPE_FLOAT32,
            Float64 = SLANG_SCALAR_TYPE_FLOAT64,
            Int8 = SLANG_SCALAR_TYPE_INT8,
            UInt8 = SLANG_SCALAR_TYPE_UINT8,
            Int16 = SLANG_SCALAR_TYPE_INT16,
            UInt16 = SLANG_SCALAR_TYPE_UINT16,
        }
    }

    [NativeTypeName("SlangParameterCategoryIntegral")]
    public enum ParameterCategory : uint
    {
        None = SLANG_PARAMETER_CATEGORY_NONE,
        Mixed = SLANG_PARAMETER_CATEGORY_MIXED,
        ConstantBuffer = SLANG_PARAMETER_CATEGORY_CONSTANT_BUFFER,
        ShaderResource = SLANG_PARAMETER_CATEGORY_SHADER_RESOURCE,
        UnorderedAccess = SLANG_PARAMETER_CATEGORY_UNORDERED_ACCESS,
        VaryingInput = SLANG_PARAMETER_CATEGORY_VARYING_INPUT,
        VaryingOutput = SLANG_PARAMETER_CATEGORY_VARYING_OUTPUT,
        SamplerState = SLANG_PARAMETER_CATEGORY_SAMPLER_STATE,
        Uniform = SLANG_PARAMETER_CATEGORY_UNIFORM,
        DescriptorTableSlot = SLANG_PARAMETER_CATEGORY_DESCRIPTOR_TABLE_SLOT,
        SpecializationConstant = SLANG_PARAMETER_CATEGORY_SPECIALIZATION_CONSTANT,
        PushConstantBuffer = SLANG_PARAMETER_CATEGORY_PUSH_CONSTANT_BUFFER,
        RegisterSpace = SLANG_PARAMETER_CATEGORY_REGISTER_SPACE,
        GenericResource = SLANG_PARAMETER_CATEGORY_GENERIC,
        RayPayload = SLANG_PARAMETER_CATEGORY_RAY_PAYLOAD,
        HitAttributes = SLANG_PARAMETER_CATEGORY_HIT_ATTRIBUTES,
        CallablePayload = SLANG_PARAMETER_CATEGORY_CALLABLE_PAYLOAD,
        ShaderRecord = SLANG_PARAMETER_CATEGORY_SHADER_RECORD,
        ExistentialTypeParam = SLANG_PARAMETER_CATEGORY_EXISTENTIAL_TYPE_PARAM,
        ExistentialObjectParam = SLANG_PARAMETER_CATEGORY_EXISTENTIAL_OBJECT_PARAM,
        SubElementRegisterSpace = SLANG_PARAMETER_CATEGORY_SUB_ELEMENT_REGISTER_SPACE,
        InputAttachmentIndex = SLANG_PARAMETER_CATEGORY_SUBPASS,
        MetalBuffer = SLANG_PARAMETER_CATEGORY_CONSTANT_BUFFER,
        MetalTexture = SLANG_PARAMETER_CATEGORY_METAL_TEXTURE,
        MetalArgumentBufferElement = SLANG_PARAMETER_CATEGORY_METAL_ARGUMENT_BUFFER_ELEMENT,
        MetalAttribute = SLANG_PARAMETER_CATEGORY_METAL_ATTRIBUTE,
        MetalPayload = SLANG_PARAMETER_CATEGORY_METAL_PAYLOAD,
        VertexInput = SLANG_PARAMETER_CATEGORY_VERTEX_INPUT,
        FragmentOutput = SLANG_PARAMETER_CATEGORY_FRAGMENT_OUTPUT,
    }

    [NativeTypeName("SlangBindingTypeIntegral")]
    public enum BindingType : uint
    {
        Unknown = SLANG_BINDING_TYPE_UNKNOWN,
        Sampler = SLANG_BINDING_TYPE_SAMPLER,
        Texture = SLANG_BINDING_TYPE_TEXTURE,
        ConstantBuffer = SLANG_BINDING_TYPE_CONSTANT_BUFFER,
        ParameterBlock = SLANG_BINDING_TYPE_PARAMETER_BLOCK,
        TypedBuffer = SLANG_BINDING_TYPE_TYPED_BUFFER,
        RawBuffer = SLANG_BINDING_TYPE_RAW_BUFFER,
        CombinedTextureSampler = SLANG_BINDING_TYPE_COMBINED_TEXTURE_SAMPLER,
        InputRenderTarget = SLANG_BINDING_TYPE_INPUT_RENDER_TARGET,
        InlineUniformData = SLANG_BINDING_TYPE_INLINE_UNIFORM_DATA,
        RayTracingAccelerationStructure = SLANG_BINDING_TYPE_RAY_TRACING_ACCELERATION_STRUCTURE,
        VaryingInput = SLANG_BINDING_TYPE_VARYING_INPUT,
        VaryingOutput = SLANG_BINDING_TYPE_VARYING_OUTPUT,
        ExistentialValue = SLANG_BINDING_TYPE_EXISTENTIAL_VALUE,
        PushConstant = SLANG_BINDING_TYPE_PUSH_CONSTANT,
        MutableFlag = SLANG_BINDING_TYPE_MUTABLE_FLAG,
        MutableTexture = SLANG_BINDING_TYPE_MUTABLE_TETURE,
        MutableTypedBuffer = SLANG_BINDING_TYPE_MUTABLE_TYPED_BUFFER,
        MutableRawBuffer = SLANG_BINDING_TYPE_MUTABLE_RAW_BUFFER,
        BaseMask = SLANG_BINDING_TYPE_BASE_MASK,
        ExtMask = SLANG_BINDING_TYPE_EXT_MASK,
    }

    public unsafe partial struct TypeLayoutReflection
    {
        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getType()
        {
            return (TypeReflection*)(spReflectionTypeLayout_GetType((SlangReflectionTypeLayout*)(this)));
        }

        [return: NativeTypeName("slang::TypeReflection::Kind")]
        public Kind getKind()
        {
            return (Kind)(spReflectionTypeLayout_getKind(unchecked((SlangReflectionTypeLayout*)(this))));
        }

        [return: NativeTypeName("size_t")]
        public nuint getSize(SlangParameterCategory category)
        {
            return spReflectionTypeLayout_GetSize((SlangReflectionTypeLayout*)(this), category);
        }

        [return: NativeTypeName("size_t")]
        public nuint getStride(SlangParameterCategory category)
        {
            return spReflectionTypeLayout_GetStride((SlangReflectionTypeLayout*)(this), category);
        }

        [return: NativeTypeName("int32_t")]
        public int getAlignment(SlangParameterCategory category)
        {
            return spReflectionTypeLayout_getAlignment(unchecked((SlangReflectionTypeLayout*)(this)), category);
        }

        [return: NativeTypeName("size_t")]
        public nuint getSize([NativeTypeName("slang::ParameterCategory")] ParameterCategory category = Uniform)
        {
            return spReflectionTypeLayout_GetSize((SlangReflectionTypeLayout*)(this), unchecked((SlangParameterCategory)(category)));
        }

        [return: NativeTypeName("size_t")]
        public nuint getStride([NativeTypeName("slang::ParameterCategory")] ParameterCategory category = Uniform)
        {
            return spReflectionTypeLayout_GetStride((SlangReflectionTypeLayout*)(this), unchecked((SlangParameterCategory)(category)));
        }

        [return: NativeTypeName("int32_t")]
        public int getAlignment([NativeTypeName("slang::ParameterCategory")] ParameterCategory category = Uniform)
        {
            return spReflectionTypeLayout_getAlignment(unchecked((SlangReflectionTypeLayout*)(this)), (SlangParameterCategory)(category));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getFieldCount()
        {
            return spReflectionTypeLayout_GetFieldCount((SlangReflectionTypeLayout*)(this));
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        public VariableLayoutReflection* getFieldByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return (VariableLayoutReflection*)(spReflectionTypeLayout_GetFieldByIndex((SlangReflectionTypeLayout*)(this), index));
        }

        [return: NativeTypeName("SlangInt")]
        public long findFieldIndexByName([NativeTypeName("const char *")] sbyte* nameBegin, [NativeTypeName("const char *")] sbyte* nameEnd = null)
        {
            return spReflectionTypeLayout_findFieldIndexByName(unchecked((SlangReflectionTypeLayout*)(this)), nameBegin, nameEnd);
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        public VariableLayoutReflection* getExplicitCounter()
        {
            return (VariableLayoutReflection*)(spReflectionTypeLayout_GetExplicitCounter((SlangReflectionTypeLayout*)(this)));
        }

        public bool isArray()
        {
            return getType()->isArray();
        }

        [return: NativeTypeName("slang::TypeLayoutReflection *")]
        public TypeLayoutReflection* unwrapArray()
        {
            TypeLayoutReflection* typeLayout = this;

            while (typeLayout->isArray())
            {
                typeLayout = typeLayout->getElementTypeLayout();
            }

            return typeLayout;
        }

        [return: NativeTypeName("size_t")]
        public nuint getElementCount([NativeTypeName("slang::ShaderReflection *")] ShaderReflection* reflection = null)
        {
            return getType()->getElementCount((SlangProgramLayout*)(reflection));
        }

        [return: NativeTypeName("size_t")]
        public nuint getTotalArrayElementCount()
        {
            return getType()->getTotalArrayElementCount();
        }

        [return: NativeTypeName("size_t")]
        public nuint getElementStride(SlangParameterCategory category)
        {
            return spReflectionTypeLayout_GetElementStride((SlangReflectionTypeLayout*)(this), category);
        }

        [return: NativeTypeName("slang::TypeLayoutReflection *")]
        public TypeLayoutReflection* getElementTypeLayout()
        {
            return (TypeLayoutReflection*)(spReflectionTypeLayout_GetElementTypeLayout((SlangReflectionTypeLayout*)(this)));
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        public VariableLayoutReflection* getElementVarLayout()
        {
            return (VariableLayoutReflection*)(spReflectionTypeLayout_GetElementVarLayout((SlangReflectionTypeLayout*)(this)));
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        public VariableLayoutReflection* getContainerVarLayout()
        {
            return (VariableLayoutReflection*)(spReflectionTypeLayout_getContainerVarLayout((SlangReflectionTypeLayout*)(this)));
        }

        [return: NativeTypeName("slang::ParameterCategory")]
        public ParameterCategory getParameterCategory()
        {
            return (ParameterCategory)(spReflectionTypeLayout_GetParameterCategory(unchecked((SlangReflectionTypeLayout*)(this))));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getCategoryCount()
        {
            return spReflectionTypeLayout_GetCategoryCount((SlangReflectionTypeLayout*)(this));
        }

        [return: NativeTypeName("slang::ParameterCategory")]
        public ParameterCategory getCategoryByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return (ParameterCategory)(spReflectionTypeLayout_GetCategoryByIndex(unchecked((SlangReflectionTypeLayout*)(this)), index));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getRowCount()
        {
            return getType()->getRowCount();
        }

        [return: NativeTypeName("unsigned int")]
        public uint getColumnCount()
        {
            return getType()->getColumnCount();
        }

        [return: NativeTypeName("slang::TypeReflection::ScalarType")]
        public ScalarType getScalarType()
        {
            return getType()->getScalarType();
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getResourceResultType()
        {
            return getType()->getResourceResultType();
        }

        public SlangResourceShape getResourceShape()
        {
            return getType()->getResourceShape();
        }

        public SlangResourceAccess getResourceAccess()
        {
            return getType()->getResourceAccess();
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            return getType()->getName();
        }

        public SlangMatrixLayoutMode getMatrixLayoutMode()
        {
            return spReflectionTypeLayout_GetMatrixLayoutMode(unchecked((SlangReflectionTypeLayout*)(this)));
        }

        public int getGenericParamIndex()
        {
            return spReflectionTypeLayout_getGenericParamIndex(unchecked((SlangReflectionTypeLayout*)(this)));
        }

        [return: NativeTypeName("slang::TypeLayoutReflection *")]
        [Obsolete]
        public TypeLayoutReflection* getPendingDataTypeLayout()
        {
            return null;
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        [Obsolete]
        public VariableLayoutReflection* getSpecializedTypePendingDataVarLayout()
        {
            return null;
        }

        [return: NativeTypeName("SlangInt")]
        public long getBindingRangeCount()
        {
            return spReflectionTypeLayout_getBindingRangeCount(unchecked((SlangReflectionTypeLayout*)(this)));
        }

        [return: NativeTypeName("slang::BindingType")]
        public BindingType getBindingRangeType([NativeTypeName("SlangInt")] long index)
        {
            return (BindingType)(spReflectionTypeLayout_getBindingRangeType(unchecked((SlangReflectionTypeLayout*)(this)), index));
        }

        public bool isBindingRangeSpecializable([NativeTypeName("SlangInt")] long index)
        {
            return (bool)(spReflectionTypeLayout_isBindingRangeSpecializable(unchecked((SlangReflectionTypeLayout*)(this)), index));
        }

        [return: NativeTypeName("SlangInt")]
        public long getBindingRangeBindingCount([NativeTypeName("SlangInt")] long index)
        {
            return spReflectionTypeLayout_getBindingRangeBindingCount(unchecked((SlangReflectionTypeLayout*)(this)), index);
        }

        [return: NativeTypeName("SlangInt")]
        public long getFieldBindingRangeOffset([NativeTypeName("SlangInt")] long fieldIndex)
        {
            return spReflectionTypeLayout_getFieldBindingRangeOffset(unchecked((SlangReflectionTypeLayout*)(this)), fieldIndex);
        }

        [return: NativeTypeName("SlangInt")]
        public long getExplicitCounterBindingRangeOffset()
        {
            return spReflectionTypeLayout_getExplicitCounterBindingRangeOffset(unchecked((SlangReflectionTypeLayout*)(this)));
        }

        [return: NativeTypeName("slang::TypeLayoutReflection *")]
        public TypeLayoutReflection* getBindingRangeLeafTypeLayout([NativeTypeName("SlangInt")] long index)
        {
            return (TypeLayoutReflection*)(spReflectionTypeLayout_getBindingRangeLeafTypeLayout((SlangReflectionTypeLayout*)(this), index));
        }

        [return: NativeTypeName("slang::VariableReflection *")]
        public VariableReflection* getBindingRangeLeafVariable([NativeTypeName("SlangInt")] long index)
        {
            return (VariableReflection*)(spReflectionTypeLayout_getBindingRangeLeafVariable((SlangReflectionTypeLayout*)(this), index));
        }

        public SlangImageFormat getBindingRangeImageFormat([NativeTypeName("SlangInt")] long index)
        {
            return spReflectionTypeLayout_getBindingRangeImageFormat(unchecked((SlangReflectionTypeLayout*)(this)), index);
        }

        [return: NativeTypeName("SlangInt")]
        public long getBindingRangeDescriptorSetIndex([NativeTypeName("SlangInt")] long index)
        {
            return spReflectionTypeLayout_getBindingRangeDescriptorSetIndex(unchecked((SlangReflectionTypeLayout*)(this)), index);
        }

        [return: NativeTypeName("SlangInt")]
        public long getBindingRangeFirstDescriptorRangeIndex([NativeTypeName("SlangInt")] long index)
        {
            return spReflectionTypeLayout_getBindingRangeFirstDescriptorRangeIndex(unchecked((SlangReflectionTypeLayout*)(this)), index);
        }

        [return: NativeTypeName("SlangInt")]
        public long getBindingRangeDescriptorRangeCount([NativeTypeName("SlangInt")] long index)
        {
            return spReflectionTypeLayout_getBindingRangeDescriptorRangeCount(unchecked((SlangReflectionTypeLayout*)(this)), index);
        }

        [return: NativeTypeName("SlangInt")]
        public long getDescriptorSetCount()
        {
            return spReflectionTypeLayout_getDescriptorSetCount(unchecked((SlangReflectionTypeLayout*)(this)));
        }

        [return: NativeTypeName("SlangInt")]
        public long getDescriptorSetSpaceOffset([NativeTypeName("SlangInt")] long setIndex)
        {
            return spReflectionTypeLayout_getDescriptorSetSpaceOffset(unchecked((SlangReflectionTypeLayout*)(this)), setIndex);
        }

        [return: NativeTypeName("SlangInt")]
        public long getDescriptorSetDescriptorRangeCount([NativeTypeName("SlangInt")] long setIndex)
        {
            return spReflectionTypeLayout_getDescriptorSetDescriptorRangeCount(unchecked((SlangReflectionTypeLayout*)(this)), setIndex);
        }

        [return: NativeTypeName("SlangInt")]
        public long getDescriptorSetDescriptorRangeIndexOffset([NativeTypeName("SlangInt")] long setIndex, [NativeTypeName("SlangInt")] long rangeIndex)
        {
            return spReflectionTypeLayout_getDescriptorSetDescriptorRangeIndexOffset(unchecked((SlangReflectionTypeLayout*)(this)), setIndex, rangeIndex);
        }

        [return: NativeTypeName("SlangInt")]
        public long getDescriptorSetDescriptorRangeDescriptorCount([NativeTypeName("SlangInt")] long setIndex, [NativeTypeName("SlangInt")] long rangeIndex)
        {
            return spReflectionTypeLayout_getDescriptorSetDescriptorRangeDescriptorCount(unchecked((SlangReflectionTypeLayout*)(this)), setIndex, rangeIndex);
        }

        [return: NativeTypeName("slang::BindingType")]
        public BindingType getDescriptorSetDescriptorRangeType([NativeTypeName("SlangInt")] long setIndex, [NativeTypeName("SlangInt")] long rangeIndex)
        {
            return (BindingType)(spReflectionTypeLayout_getDescriptorSetDescriptorRangeType(unchecked((SlangReflectionTypeLayout*)(this)), setIndex, rangeIndex));
        }

        [return: NativeTypeName("slang::ParameterCategory")]
        public ParameterCategory getDescriptorSetDescriptorRangeCategory([NativeTypeName("SlangInt")] long setIndex, [NativeTypeName("SlangInt")] long rangeIndex)
        {
            return (ParameterCategory)(spReflectionTypeLayout_getDescriptorSetDescriptorRangeCategory(unchecked((SlangReflectionTypeLayout*)(this)), setIndex, rangeIndex));
        }

        [return: NativeTypeName("SlangInt")]
        public long getSubObjectRangeCount()
        {
            return spReflectionTypeLayout_getSubObjectRangeCount(unchecked((SlangReflectionTypeLayout*)(this)));
        }

        [return: NativeTypeName("SlangInt")]
        public long getSubObjectRangeBindingRangeIndex([NativeTypeName("SlangInt")] long subObjectRangeIndex)
        {
            return spReflectionTypeLayout_getSubObjectRangeBindingRangeIndex(unchecked((SlangReflectionTypeLayout*)(this)), subObjectRangeIndex);
        }

        [return: NativeTypeName("SlangInt")]
        public long getSubObjectRangeSpaceOffset([NativeTypeName("SlangInt")] long subObjectRangeIndex)
        {
            return spReflectionTypeLayout_getSubObjectRangeSpaceOffset(unchecked((SlangReflectionTypeLayout*)(this)), subObjectRangeIndex);
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        public VariableLayoutReflection* getSubObjectRangeOffset([NativeTypeName("SlangInt")] long subObjectRangeIndex)
        {
            return (VariableLayoutReflection*)(spReflectionTypeLayout_getSubObjectRangeOffset((SlangReflectionTypeLayout*)(this), subObjectRangeIndex));
        }
    }

    public partial struct Modifier
    {

        [NativeTypeName("SlangModifierIDIntegral")]
        public enum ID : uint
        {
            Shared = SLANG_MODIFIER_SHARED,
            NoDiff = SLANG_MODIFIER_NO_DIFF,
            Static = SLANG_MODIFIER_STATIC,
            Const = SLANG_MODIFIER_CONST,
            Export = SLANG_MODIFIER_EXPORT,
            Extern = SLANG_MODIFIER_EXTERN,
            Differentiable = SLANG_MODIFIER_DIFFERENTIABLE,
            Mutating = SLANG_MODIFIER_MUTATING,
            In = SLANG_MODIFIER_IN,
            Out = SLANG_MODIFIER_OUT,
            InOut = SLANG_MODIFIER_INOUT,
        }
    }

    public unsafe partial struct VariableReflection
    {
        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            return spReflectionVariable_GetName((SlangReflectionVariable*)(this));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getType()
        {
            return (TypeReflection*)(spReflectionVariable_GetType((SlangReflectionVariable*)(this)));
        }

        [return: NativeTypeName("slang::Modifier *")]
        public Modifier* findModifier([NativeTypeName("slang::Modifier::ID")] ID id)
        {
            return (Modifier*)(spReflectionVariable_FindModifier((SlangReflectionVariable*)(this), unchecked((SlangModifierID)(id))));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getUserAttributeCount()
        {
            return spReflectionVariable_GetUserAttributeCount((SlangReflectionVariable*)(this));
        }

        [return: NativeTypeName("slang::Attribute *")]
        public Attribute* getUserAttributeByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return (Attribute*)(spReflectionVariable_GetUserAttribute((SlangReflectionVariable*)(this), index));
        }

        [return: NativeTypeName("slang::Attribute *")]
        public Attribute* findAttributeByName([NativeTypeName("SlangSession *")] IGlobalSession* globalSession, [NativeTypeName("const char *")] sbyte* name)
        {
            return (Attribute*)(spReflectionVariable_FindUserAttributeByName((SlangReflectionVariable*)(this), globalSession, name));
        }

        [return: NativeTypeName("slang::Attribute *")]
        public Attribute* findUserAttributeByName([NativeTypeName("SlangSession *")] IGlobalSession* globalSession, [NativeTypeName("const char *")] sbyte* name)
        {
            return findAttributeByName(globalSession, name);
        }

        public bool hasDefaultValue()
        {
            return spReflectionVariable_HasDefaultValue(unchecked((SlangReflectionVariable*)(this)));
        }

        [return: NativeTypeName("SlangResult")]
        public int getDefaultValueInt([NativeTypeName("int64_t *")] long* value)
        {
            return spReflectionVariable_GetDefaultValueInt(unchecked((SlangReflectionVariable*)(this)), value);
        }

        [return: NativeTypeName("SlangResult")]
        public int getDefaultValueFloat(float* value)
        {
            return spReflectionVariable_GetDefaultValueFloat(unchecked((SlangReflectionVariable*)(this)), value);
        }

        [return: NativeTypeName("slang::GenericReflection *")]
        public GenericReflection* getGenericContainer()
        {
            return (GenericReflection*)(spReflectionVariable_GetGenericContainer((SlangReflectionVariable*)(this)));
        }

        [return: NativeTypeName("slang::VariableReflection *")]
        public VariableReflection* applySpecializations([NativeTypeName("slang::GenericReflection *")] GenericReflection* generic)
        {
            return (VariableReflection*)(spReflectionVariable_applySpecializations((SlangReflectionVariable*)(this), (SlangReflectionGeneric*)(generic)));
        }
    }

    public unsafe partial struct VariableLayoutReflection
    {
        [return: NativeTypeName("slang::VariableReflection *")]
        public VariableReflection* getVariable()
        {
            return (VariableReflection*)(spReflectionVariableLayout_GetVariable((SlangReflectionVariableLayout*)(this)));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            if ((var) != null)
            {
                return var->getName();
            }

            return null;
        }

        [return: NativeTypeName("slang::Modifier *")]
        public Modifier* findModifier([NativeTypeName("slang::Modifier::ID")] ID id)
        {
            return getVariable()->findModifier(id);
        }

        [return: NativeTypeName("slang::TypeLayoutReflection *")]
        public TypeLayoutReflection* getTypeLayout()
        {
            return (TypeLayoutReflection*)(spReflectionVariableLayout_GetTypeLayout((SlangReflectionVariableLayout*)(this)));
        }

        [return: NativeTypeName("slang::ParameterCategory")]
        public ParameterCategory getCategory()
        {
            return getTypeLayout()->getParameterCategory();
        }

        [return: NativeTypeName("unsigned int")]
        public uint getCategoryCount()
        {
            return getTypeLayout()->getCategoryCount();
        }

        [return: NativeTypeName("slang::ParameterCategory")]
        public ParameterCategory getCategoryByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return getTypeLayout()->getCategoryByIndex(index);
        }

        [return: NativeTypeName("size_t")]
        public nuint getOffset(SlangParameterCategory category)
        {
            return spReflectionVariableLayout_GetOffset((SlangReflectionVariableLayout*)(this), category);
        }

        [return: NativeTypeName("size_t")]
        public nuint getOffset([NativeTypeName("slang::ParameterCategory")] ParameterCategory category = Uniform)
        {
            return spReflectionVariableLayout_GetOffset((SlangReflectionVariableLayout*)(this), unchecked((SlangParameterCategory)(category)));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getType()
        {
            return getVariable()->getType();
        }

        [return: NativeTypeName("unsigned int")]
        public uint getBindingIndex()
        {
            return spReflectionParameter_GetBindingIndex((SlangReflectionVariableLayout*)(this));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getBindingSpace()
        {
            return spReflectionParameter_GetBindingSpace((SlangReflectionVariableLayout*)(this));
        }

        [return: NativeTypeName("size_t")]
        public nuint getBindingSpace(SlangParameterCategory category)
        {
            return spReflectionVariableLayout_GetSpace((SlangReflectionVariableLayout*)(this), category);
        }

        [return: NativeTypeName("size_t")]
        public nuint getBindingSpace([NativeTypeName("slang::ParameterCategory")] ParameterCategory category)
        {
            return spReflectionVariableLayout_GetSpace((SlangReflectionVariableLayout*)(this), unchecked((SlangParameterCategory)(category)));
        }

        public SlangImageFormat getImageFormat()
        {
            return spReflectionVariableLayout_GetImageFormat(unchecked((SlangReflectionVariableLayout*)(this)));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getSemanticName()
        {
            return spReflectionVariableLayout_GetSemanticName((SlangReflectionVariableLayout*)(this));
        }

        [return: NativeTypeName("size_t")]
        public nuint getSemanticIndex()
        {
            return spReflectionVariableLayout_GetSemanticIndex((SlangReflectionVariableLayout*)(this));
        }

        public SlangStage getStage()
        {
            return spReflectionVariableLayout_getStage(unchecked((SlangReflectionVariableLayout*)(this)));
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        [Obsolete]
        public VariableLayoutReflection* getPendingDataLayout()
        {
            return null;
        }
    }

    public unsafe partial struct FunctionReflection
    {
        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            return spReflectionFunction_GetName((SlangReflectionFunction*)(this));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getReturnType()
        {
            return (TypeReflection*)(spReflectionFunction_GetResultType((SlangReflectionFunction*)(this)));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getParameterCount()
        {
            return spReflectionFunction_GetParameterCount((SlangReflectionFunction*)(this));
        }

        [return: NativeTypeName("slang::VariableReflection *")]
        public VariableReflection* getParameterByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return (VariableReflection*)(spReflectionFunction_GetParameter((SlangReflectionFunction*)(this), index));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getUserAttributeCount()
        {
            return spReflectionFunction_GetUserAttributeCount((SlangReflectionFunction*)(this));
        }

        [return: NativeTypeName("slang::Attribute *")]
        public Attribute* getUserAttributeByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return (Attribute*)(spReflectionFunction_GetUserAttribute((SlangReflectionFunction*)(this), index));
        }

        [return: NativeTypeName("slang::Attribute *")]
        public Attribute* findAttributeByName([NativeTypeName("SlangSession *")] IGlobalSession* globalSession, [NativeTypeName("const char *")] sbyte* name)
        {
            return (Attribute*)(spReflectionFunction_FindUserAttributeByName((SlangReflectionFunction*)(this), globalSession, name));
        }

        [return: NativeTypeName("slang::Attribute *")]
        public Attribute* findUserAttributeByName([NativeTypeName("SlangSession *")] IGlobalSession* globalSession, [NativeTypeName("const char *")] sbyte* name)
        {
            return findAttributeByName(globalSession, name);
        }

        [return: NativeTypeName("slang::Modifier *")]
        public Modifier* findModifier([NativeTypeName("slang::Modifier::ID")] ID id)
        {
            return (Modifier*)(spReflectionFunction_FindModifier((SlangReflectionFunction*)(this), unchecked((SlangModifierID)(id))));
        }

        [return: NativeTypeName("slang::GenericReflection *")]
        public GenericReflection* getGenericContainer()
        {
            return (GenericReflection*)(spReflectionFunction_GetGenericContainer((SlangReflectionFunction*)(this)));
        }

        [return: NativeTypeName("slang::FunctionReflection *")]
        public FunctionReflection* applySpecializations([NativeTypeName("slang::GenericReflection *")] GenericReflection* generic)
        {
            return (FunctionReflection*)(spReflectionFunction_applySpecializations((SlangReflectionFunction*)(this), (SlangReflectionGeneric*)(generic)));
        }

        [return: NativeTypeName("slang::FunctionReflection *")]
        public FunctionReflection* specializeWithArgTypes([NativeTypeName("unsigned int")] uint argCount, [NativeTypeName("TypeReflection *const *")] TypeReflection** types)
        {
            return (FunctionReflection*)(spReflectionFunction_specializeWithArgTypes((SlangReflectionFunction*)(this), argCount, (SlangReflectionType**)(types)));
        }

        public bool isOverloaded()
        {
            return spReflectionFunction_isOverloaded(unchecked((SlangReflectionFunction*)(this)));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getOverloadCount()
        {
            return spReflectionFunction_getOverloadCount((SlangReflectionFunction*)(this));
        }

        [return: NativeTypeName("slang::FunctionReflection *")]
        public FunctionReflection* getOverload([NativeTypeName("unsigned int")] uint index)
        {
            return (FunctionReflection*)(spReflectionFunction_getOverload((SlangReflectionFunction*)(this), index));
        }
    }

    public unsafe partial struct GenericReflection
    {
        [return: NativeTypeName("slang::DeclReflection *")]
        public DeclReflection* asDecl()
        {
            return (DeclReflection*)(spReflectionGeneric_asDecl((SlangReflectionGeneric*)(this)));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            return spReflectionGeneric_GetName((SlangReflectionGeneric*)(this));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getTypeParameterCount()
        {
            return spReflectionGeneric_GetTypeParameterCount((SlangReflectionGeneric*)(this));
        }

        [return: NativeTypeName("slang::VariableReflection *")]
        public VariableReflection* getTypeParameter([NativeTypeName("unsigned int")] uint index)
        {
            return (VariableReflection*)(spReflectionGeneric_GetTypeParameter((SlangReflectionGeneric*)(this), index));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getValueParameterCount()
        {
            return spReflectionGeneric_GetValueParameterCount((SlangReflectionGeneric*)(this));
        }

        [return: NativeTypeName("slang::VariableReflection *")]
        public VariableReflection* getValueParameter([NativeTypeName("unsigned int")] uint index)
        {
            return (VariableReflection*)(spReflectionGeneric_GetValueParameter((SlangReflectionGeneric*)(this), index));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getTypeParameterConstraintCount([NativeTypeName("slang::VariableReflection *")] VariableReflection* typeParam)
        {
            return spReflectionGeneric_GetTypeParameterConstraintCount((SlangReflectionGeneric*)(this), (SlangReflectionVariable*)(typeParam));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getTypeParameterConstraintType([NativeTypeName("slang::VariableReflection *")] VariableReflection* typeParam, [NativeTypeName("unsigned int")] uint index)
        {
            return (TypeReflection*)(spReflectionGeneric_GetTypeParameterConstraintType((SlangReflectionGeneric*)(this), (SlangReflectionVariable*)(typeParam), index));
        }

        [return: NativeTypeName("slang::DeclReflection *")]
        public DeclReflection* getInnerDecl()
        {
            return (DeclReflection*)(spReflectionGeneric_GetInnerDecl((SlangReflectionGeneric*)(this)));
        }

        public SlangDeclKind getInnerKind()
        {
            return spReflectionGeneric_GetInnerKind(unchecked((SlangReflectionGeneric*)(this)));
        }

        [return: NativeTypeName("slang::GenericReflection *")]
        public GenericReflection* getOuterGenericContainer()
        {
            return (GenericReflection*)(spReflectionGeneric_GetOuterGenericContainer((SlangReflectionGeneric*)(this)));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getConcreteType([NativeTypeName("slang::VariableReflection *")] VariableReflection* typeParam)
        {
            return (TypeReflection*)(spReflectionGeneric_GetConcreteType((SlangReflectionGeneric*)(this), (SlangReflectionVariable*)(typeParam)));
        }

        [return: NativeTypeName("int64_t")]
        public long getConcreteIntVal([NativeTypeName("slang::VariableReflection *")] VariableReflection* valueParam)
        {
            return spReflectionGeneric_GetConcreteIntVal(unchecked((SlangReflectionGeneric*)(this)), unchecked((SlangReflectionVariable*)(valueParam)));
        }

        [return: NativeTypeName("slang::GenericReflection *")]
        public GenericReflection* applySpecializations([NativeTypeName("slang::GenericReflection *")] GenericReflection* generic)
        {
            return (GenericReflection*)(spReflectionGeneric_applySpecializations((SlangReflectionGeneric*)(this), (SlangReflectionGeneric*)(generic)));
        }
    }

    public unsafe partial struct EntryPointReflection
    {
        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            return spReflectionEntryPoint_getName((SlangEntryPointLayout*)(this));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getNameOverride()
        {
            return spReflectionEntryPoint_getNameOverride((SlangEntryPointLayout*)(this));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getParameterCount()
        {
            return spReflectionEntryPoint_getParameterCount((SlangEntryPointLayout*)(this));
        }

        [return: NativeTypeName("slang::FunctionReflection *")]
        public FunctionReflection* getFunction()
        {
            return (FunctionReflection*)(spReflectionEntryPoint_getFunction((SlangEntryPointLayout*)(this)));
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        public VariableLayoutReflection* getParameterByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return (VariableLayoutReflection*)(spReflectionEntryPoint_getParameterByIndex((SlangEntryPointLayout*)(this), index));
        }

        public SlangStage getStage()
        {
            return spReflectionEntryPoint_getStage(unchecked((SlangEntryPointLayout*)(this)));
        }

        public void getComputeThreadGroupSize([NativeTypeName("SlangUInt")] ulong axisCount, [NativeTypeName("SlangUInt *")] ulong* outSizeAlongAxis)
        {
            spReflectionEntryPoint_getComputeThreadGroupSize(unchecked((SlangEntryPointLayout*)(this)), axisCount, outSizeAlongAxis);
        }

        public void getComputeWaveSize([NativeTypeName("SlangUInt *")] ulong* outWaveSize)
        {
            spReflectionEntryPoint_getComputeWaveSize(unchecked((SlangEntryPointLayout*)(this)), outWaveSize);
        }

        public bool usesAnySampleRateInput()
        {
            return 0 != spReflectionEntryPoint_usesAnySampleRateInput(unchecked((SlangEntryPointLayout*)(this)));
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        public VariableLayoutReflection* getVarLayout()
        {
            return (VariableLayoutReflection*)(spReflectionEntryPoint_getVarLayout((SlangEntryPointLayout*)(this)));
        }

        [return: NativeTypeName("slang::TypeLayoutReflection *")]
        public TypeLayoutReflection* getTypeLayout()
        {
            return getVarLayout()->getTypeLayout();
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        public VariableLayoutReflection* getResultVarLayout()
        {
            return (VariableLayoutReflection*)(spReflectionEntryPoint_getResultVarLayout((SlangEntryPointLayout*)(this)));
        }

        public bool hasDefaultConstantBuffer()
        {
            return spReflectionEntryPoint_hasDefaultConstantBuffer(unchecked((SlangEntryPointLayout*)(this))) != 0;
        }
    }

    public unsafe partial struct TypeParameterReflection
    {
        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            return spReflectionTypeParameter_GetName((SlangReflectionTypeParameter*)(this));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getIndex()
        {
            return spReflectionTypeParameter_GetIndex((SlangReflectionTypeParameter*)(this));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getConstraintCount()
        {
            return spReflectionTypeParameter_GetConstraintCount((SlangReflectionTypeParameter*)(this));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getConstraintByIndex(int index)
        {
            return (TypeReflection*)(spReflectionTypeParameter_GetConstraintByIndex((SlangReflectionTypeParameter*)(this), index));
        }
    }

    [NativeTypeName("SlangLayoutRulesIntegral")]
    public enum LayoutRules : uint
    {
        Default = SLANG_LAYOUT_RULES_DEFAULT,
        MetalArgumentBufferTier2 = SLANG_LAYOUT_RULES_METAL_ARGUMENT_BUFFER_TIER_2,
        DefaultStructuredBuffer = SLANG_LAYOUT_RULES_DEFAULT_STRUCTURED_BUFFER,
        DefaultConstantBuffer = SLANG_LAYOUT_RULES_DEFAULT_CONSTANT_BUFFER,
    }

    public unsafe partial struct ShaderReflection
    {
        [return: NativeTypeName("unsigned int")]
        public uint getParameterCount()
        {
            return spReflection_GetParameterCount((SlangProgramLayout*)(this));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getTypeParameterCount()
        {
            return spReflection_GetTypeParameterCount((SlangProgramLayout*)(this));
        }

        [return: NativeTypeName("slang::ISession *")]
        public ISession* getSession()
        {
            return spReflection_GetSession((SlangProgramLayout*)(this));
        }

        [return: NativeTypeName("slang::TypeParameterReflection *")]
        public TypeParameterReflection* getTypeParameterByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return (TypeParameterReflection*)(spReflection_GetTypeParameterByIndex((SlangProgramLayout*)(this), index));
        }

        [return: NativeTypeName("slang::TypeParameterReflection *")]
        public TypeParameterReflection* findTypeParameter([NativeTypeName("const char *")] sbyte* name)
        {
            return (TypeParameterReflection*)(spReflection_FindTypeParameter((SlangProgramLayout*)(this), name));
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        public VariableLayoutReflection* getParameterByIndex([NativeTypeName("unsigned int")] uint index)
        {
            return (VariableLayoutReflection*)(spReflection_GetParameterByIndex((SlangProgramLayout*)(this), index));
        }

        [return: NativeTypeName("slang::ProgramLayout *")]
        public static ShaderReflection* get([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request)
        {
            return (ShaderReflection*)(spGetReflection(request));
        }

        [return: NativeTypeName("SlangUInt")]
        public ulong getEntryPointCount()
        {
            return spReflection_getEntryPointCount((SlangProgramLayout*)(this));
        }

        [return: NativeTypeName("slang::EntryPointReflection *")]
        public EntryPointReflection* getEntryPointByIndex([NativeTypeName("SlangUInt")] ulong index)
        {
            return (EntryPointReflection*)(spReflection_getEntryPointByIndex((SlangProgramLayout*)(this), index));
        }

        [return: NativeTypeName("SlangUInt")]
        public ulong getGlobalConstantBufferBinding()
        {
            return spReflection_getGlobalConstantBufferBinding((SlangProgramLayout*)(this));
        }

        [return: NativeTypeName("size_t")]
        public nuint getGlobalConstantBufferSize()
        {
            return spReflection_getGlobalConstantBufferSize((SlangProgramLayout*)(this));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* findTypeByName([NativeTypeName("const char *")] sbyte* name)
        {
            return (TypeReflection*)(spReflection_FindTypeByName((SlangProgramLayout*)(this), name));
        }

        [return: NativeTypeName("slang::FunctionReflection *")]
        public FunctionReflection* findFunctionByName([NativeTypeName("const char *")] sbyte* name)
        {
            return (FunctionReflection*)(spReflection_FindFunctionByName((SlangProgramLayout*)(this), name));
        }

        [return: NativeTypeName("slang::FunctionReflection *")]
        public FunctionReflection* findFunctionByNameInType([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, [NativeTypeName("const char *")] sbyte* name)
        {
            return (FunctionReflection*)(spReflection_FindFunctionByNameInType((SlangProgramLayout*)(this), (SlangReflectionType*)(type), name));
        }

        [return: NativeTypeName("slang::FunctionReflection *")]
        [Obsolete]
        public FunctionReflection* tryResolveOverloadedFunction([NativeTypeName("uint32_t")] uint candidateCount, FunctionReflection** candidates)
        {
            return (FunctionReflection*)(spReflection_TryResolveOverloadedFunction((SlangProgramLayout*)(this), candidateCount, (SlangReflectionFunction**)(candidates)));
        }

        [return: NativeTypeName("slang::VariableReflection *")]
        public VariableReflection* findVarByNameInType([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, [NativeTypeName("const char *")] sbyte* name)
        {
            return (VariableReflection*)(spReflection_FindVarByNameInType((SlangProgramLayout*)(this), (SlangReflectionType*)(type), name));
        }

        [return: NativeTypeName("slang::TypeLayoutReflection *")]
        public TypeLayoutReflection* getTypeLayout([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, [NativeTypeName("slang::LayoutRules")] LayoutRules rules = Default)
        {
            return (TypeLayoutReflection*)(spReflection_GetTypeLayout((SlangProgramLayout*)(this), (SlangReflectionType*)(type), unchecked((SlangLayoutRules)(rules))));
        }

        [return: NativeTypeName("slang::EntryPointReflection *")]
        public EntryPointReflection* findEntryPointByName([NativeTypeName("const char *")] sbyte* name)
        {
            return (EntryPointReflection*)(spReflection_findEntryPointByName((SlangProgramLayout*)(this), name));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* specializeType([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, [NativeTypeName("SlangInt")] long specializationArgCount, [NativeTypeName("TypeReflection *const *")] TypeReflection** specializationArgs, ISlangBlob** outDiagnostics)
        {
            return (TypeReflection*)(spReflection_specializeType((SlangProgramLayout*)(this), (SlangReflectionType*)(type), specializationArgCount, (SlangReflectionType**)(specializationArgs), outDiagnostics));
        }

        [return: NativeTypeName("slang::GenericReflection *")]
        public GenericReflection* specializeGeneric([NativeTypeName("slang::GenericReflection *")] GenericReflection* generic, [NativeTypeName("SlangInt")] long specializationArgCount, [NativeTypeName("const GenericArgType *")] SlangReflectionGenericArgType* specializationArgTypes, [NativeTypeName("const GenericArgReflection *")] GenericArgReflection* specializationArgVals, ISlangBlob** outDiagnostics)
        {
            return (GenericReflection*)(spReflection_specializeGeneric((SlangProgramLayout*)(this), (SlangReflectionGeneric*)(generic), specializationArgCount, (SlangReflectionGenericArgType*)(specializationArgTypes), (SlangReflectionGenericArg*)(specializationArgVals), outDiagnostics));
        }

        public bool isSubType([NativeTypeName("slang::TypeReflection *")] TypeReflection* subType, [NativeTypeName("slang::TypeReflection *")] TypeReflection* superType)
        {
            return spReflection_isSubType(unchecked((SlangProgramLayout*)(this)), unchecked((SlangReflectionType*)(subType)), unchecked((SlangReflectionType*)(superType)));
        }

        [return: NativeTypeName("SlangUInt")]
        public readonly ulong getHashedStringCount()
        {
            return spReflection_getHashedStringCount((SlangProgramLayout*)(this));
        }

        [return: NativeTypeName("const char *")]
        public readonly sbyte* getHashedString([NativeTypeName("SlangUInt")] ulong index, [NativeTypeName("size_t *")] nuint* outCount)
        {
            return spReflection_getHashedString((SlangProgramLayout*)(this), index, outCount);
        }

        [return: NativeTypeName("slang::TypeLayoutReflection *")]
        public TypeLayoutReflection* getGlobalParamsTypeLayout()
        {
            return (TypeLayoutReflection*)(spReflection_getGlobalParamsTypeLayout((SlangProgramLayout*)(this)));
        }

        [return: NativeTypeName("slang::VariableLayoutReflection *")]
        public VariableLayoutReflection* getGlobalParamsVarLayout()
        {
            return (VariableLayoutReflection*)(spReflection_getGlobalParamsVarLayout((SlangProgramLayout*)(this)));
        }

        [return: NativeTypeName("SlangResult")]
        public int toJson(ISlangBlob** outBlob)
        {
            return spReflection_ToJson(unchecked((SlangProgramLayout*)(this)), null, outBlob);
        }

        [return: NativeTypeName("SlangInt")]
        public long getBindlessSpaceIndex()
        {
            return spReflection_getBindlessSpaceIndex(unchecked((SlangProgramLayout*)(this)));
        }
    }

    public unsafe partial struct DeclReflection
    {
        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            return spReflectionDecl_getName((SlangReflectionDecl*)(this));
        }

        [return: NativeTypeName("slang::DeclReflection::Kind")]
        public Kind getKind()
        {
            return (Kind)(spReflectionDecl_getKind(unchecked((SlangReflectionDecl*)(this))));
        }

        [return: NativeTypeName("unsigned int")]
        public uint getChildrenCount()
        {
            return spReflectionDecl_getChildrenCount((SlangReflectionDecl*)(this));
        }

        [return: NativeTypeName("slang::DeclReflection *")]
        public DeclReflection* getChild([NativeTypeName("unsigned int")] uint index)
        {
            return (DeclReflection*)(spReflectionDecl_getChild((SlangReflectionDecl*)(this), index));
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getType()
        {
            return (TypeReflection*)(spReflection_getTypeFromDecl((SlangReflectionDecl*)(this)));
        }

        [return: NativeTypeName("slang::VariableReflection *")]
        public VariableReflection* asVariable()
        {
            return (VariableReflection*)(spReflectionDecl_castToVariable((SlangReflectionDecl*)(this)));
        }

        [return: NativeTypeName("slang::FunctionReflection *")]
        public FunctionReflection* asFunction()
        {
            return (FunctionReflection*)(spReflectionDecl_castToFunction((SlangReflectionDecl*)(this)));
        }

        [return: NativeTypeName("slang::GenericReflection *")]
        public GenericReflection* asGeneric()
        {
            return (GenericReflection*)(spReflectionDecl_castToGeneric((SlangReflectionDecl*)(this)));
        }

        [return: NativeTypeName("slang::DeclReflection *")]
        public DeclReflection* getParent()
        {
            return (DeclReflection*)(spReflectionDecl_getParent((SlangReflectionDecl*)(this)));
        }

        [return: NativeTypeName("slang::Modifier *")]
        public Modifier* findModifier([NativeTypeName("slang::Modifier::ID")] ID id)
        {
            return (Modifier*)(spReflectionDecl_findModifier((SlangReflectionDecl*)(this), unchecked((SlangModifierID)(id))));
        }

        [return: NativeTypeName("slang::DeclReflection::IteratedList")]
        public IteratedList getChildren()
        {
            return (IteratedList)(new IteratedList
            {
                count = getChildrenCount(),
                parent = unchecked((DeclReflection*)(this)),
            });
        }

        public enum Kind
        {
            Unsupported = SLANG_DECL_KIND_UNSUPPORTED_FOR_REFLECTION,
            Struct = SLANG_DECL_KIND_STRUCT,
            Func = SLANG_DECL_KIND_FUNC,
            Module = SLANG_DECL_KIND_MODULE,
            Generic = SLANG_DECL_KIND_GENERIC,
            Variable = SLANG_DECL_KIND_VARIABLE,
            Namespace = SLANG_DECL_KIND_NAMESPACE,
            Enum = SLANG_DECL_KIND_ENUM,
        }

        public unsafe partial struct IteratedList
        {
            [NativeTypeName("unsigned int")]
            public uint count;

            [NativeTypeName("slang::DeclReflection *")]
            public DeclReflection* parent;

            [return: NativeTypeName("slang::DeclReflection::IteratedList::Iterator")]
            public Iterator begin()
            {
                return (Iterator)(new Iterator
                {
                    parent = parent,
                    count = count,
                    index = 0,
                });
            }

            [return: NativeTypeName("slang::DeclReflection::IteratedList::Iterator")]
            public Iterator end()
            {
                return (Iterator)(new Iterator
                {
                    parent = parent,
                    count = count,
                    index = count,
                });
            }

            public unsafe partial struct Iterator
            {
                [NativeTypeName("slang::DeclReflection *")]
                public DeclReflection* parent;

                [NativeTypeName("unsigned int")]
                public uint count;

                [NativeTypeName("unsigned int")]
                public uint index;

                [return: NativeTypeName("slang::DeclReflection *")]
                public DeclReflection* Multiply()
                {
                    return parent->getChild(index);
                }

                public void Increment()
                {
                    index++;
                }

                public bool NotEquals([NativeTypeName("const Iterator &")] Iterator* other)
                {
                    return index != other->index;
                }
            }
        }
    }

    public partial struct CompileCoreModuleFlag
    {

        [NativeTypeName("slang::CompileCoreModuleFlags")]
        public enum Enum : uint
        {
            WriteDocumentation = 0x1,
        }
    }

    public enum BuiltinModuleName
    {
        Core,
        GLSL,
    }

    [NativeTypeName("struct IGlobalSession : ISlangUnknown")]
    public unsafe partial struct IGlobalSession
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangUUID*, void**, int>)(lpVtbl[0]))((IGlobalSession*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, uint>)(lpVtbl[1]))((IGlobalSession*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, uint>)(lpVtbl[2]))((IGlobalSession*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int createSession([NativeTypeName("const SessionDesc &")] SessionDesc* desc, ISession** outSession)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, SessionDesc*, ISession**, int>)(lpVtbl[3]))((IGlobalSession*)Unsafe.AsPointer(ref this), desc, outSession);
        }

        public SlangProfileID findProfile([NativeTypeName("const char *")] sbyte* name)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, sbyte*, SlangProfileID>)(lpVtbl[4]))((IGlobalSession*)Unsafe.AsPointer(ref this), name);
        }

        public void setDownstreamCompilerPath(SlangPassThrough passThrough, [NativeTypeName("const char *")] sbyte* path)
        {
            ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangPassThrough, sbyte*, void>)(lpVtbl[5]))((IGlobalSession*)Unsafe.AsPointer(ref this), passThrough, path);
        }

        public void setDownstreamCompilerPrelude(SlangPassThrough passThrough, [NativeTypeName("const char *")] sbyte* preludeText)
        {
            ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangPassThrough, sbyte*, void>)(lpVtbl[6]))((IGlobalSession*)Unsafe.AsPointer(ref this), passThrough, preludeText);
        }

        public void getDownstreamCompilerPrelude(SlangPassThrough passThrough, ISlangBlob** outPrelude)
        {
            ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangPassThrough, ISlangBlob**, void>)(lpVtbl[7]))((IGlobalSession*)Unsafe.AsPointer(ref this), passThrough, outPrelude);
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getBuildTagString()
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, sbyte*>)(lpVtbl[8]))((IGlobalSession*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int setDefaultDownstreamCompiler(SlangSourceLanguage sourceLanguage, SlangPassThrough defaultCompiler)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangSourceLanguage, SlangPassThrough, int>)(lpVtbl[9]))((IGlobalSession*)Unsafe.AsPointer(ref this), sourceLanguage, defaultCompiler);
        }

        public SlangPassThrough getDefaultDownstreamCompiler(SlangSourceLanguage sourceLanguage)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangSourceLanguage, SlangPassThrough>)(lpVtbl[10]))((IGlobalSession*)Unsafe.AsPointer(ref this), sourceLanguage);
        }

        public void setLanguagePrelude(SlangSourceLanguage sourceLanguage, [NativeTypeName("const char *")] sbyte* preludeText)
        {
            ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangSourceLanguage, sbyte*, void>)(lpVtbl[11]))((IGlobalSession*)Unsafe.AsPointer(ref this), sourceLanguage, preludeText);
        }

        public void getLanguagePrelude(SlangSourceLanguage sourceLanguage, ISlangBlob** outPrelude)
        {
            ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangSourceLanguage, ISlangBlob**, void>)(lpVtbl[12]))((IGlobalSession*)Unsafe.AsPointer(ref this), sourceLanguage, outPrelude);
        }

        [return: NativeTypeName("SlangResult")]
        [Obsolete]
        public int createCompileRequest([NativeTypeName("slang::ICompileRequest **")] ICompileRequest** outCompileRequest)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, ICompileRequest**, int>)(lpVtbl[13]))((IGlobalSession*)Unsafe.AsPointer(ref this), outCompileRequest);
        }

        public void addBuiltins([NativeTypeName("const char *")] sbyte* sourcePath, [NativeTypeName("const char *")] sbyte* sourceString)
        {
            ((delegate* unmanaged[Stdcall]<IGlobalSession*, sbyte*, sbyte*, void>)(lpVtbl[14]))((IGlobalSession*)Unsafe.AsPointer(ref this), sourcePath, sourceString);
        }

        public void setSharedLibraryLoader(ISlangSharedLibraryLoader* loader)
        {
            ((delegate* unmanaged[Stdcall]<IGlobalSession*, ISlangSharedLibraryLoader*, void>)(lpVtbl[15]))((IGlobalSession*)Unsafe.AsPointer(ref this), loader);
        }

        public ISlangSharedLibraryLoader* getSharedLibraryLoader()
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, ISlangSharedLibraryLoader*>)(lpVtbl[16]))((IGlobalSession*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int checkCompileTargetSupport(SlangCompileTarget target)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangCompileTarget, int>)(lpVtbl[17]))((IGlobalSession*)Unsafe.AsPointer(ref this), target);
        }

        [return: NativeTypeName("SlangResult")]
        public int checkPassThroughSupport(SlangPassThrough passThrough)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangPassThrough, int>)(lpVtbl[18]))((IGlobalSession*)Unsafe.AsPointer(ref this), passThrough);
        }

        [return: NativeTypeName("SlangResult")]
        public int compileCoreModule([NativeTypeName("slang::CompileCoreModuleFlags")] uint flags)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, uint, int>)(lpVtbl[19]))((IGlobalSession*)Unsafe.AsPointer(ref this), flags);
        }

        [return: NativeTypeName("SlangResult")]
        public int loadCoreModule([NativeTypeName("const void *")] void* coreModule, [NativeTypeName("size_t")] nuint coreModuleSizeInBytes)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, void*, nuint, int>)(lpVtbl[20]))((IGlobalSession*)Unsafe.AsPointer(ref this), coreModule, coreModuleSizeInBytes);
        }

        [return: NativeTypeName("SlangResult")]
        public int saveCoreModule(SlangArchiveType archiveType, ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangArchiveType, ISlangBlob**, int>)(lpVtbl[21]))((IGlobalSession*)Unsafe.AsPointer(ref this), archiveType, outBlob);
        }

        public SlangCapabilityID findCapability([NativeTypeName("const char *")] sbyte* name)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, sbyte*, SlangCapabilityID>)(lpVtbl[22]))((IGlobalSession*)Unsafe.AsPointer(ref this), name);
        }

        public void setDownstreamCompilerForTransition(SlangCompileTarget source, SlangCompileTarget target, SlangPassThrough compiler)
        {
            ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangCompileTarget, SlangCompileTarget, SlangPassThrough, void>)(lpVtbl[23]))((IGlobalSession*)Unsafe.AsPointer(ref this), source, target, compiler);
        }

        public SlangPassThrough getDownstreamCompilerForTransition(SlangCompileTarget source, SlangCompileTarget target)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, SlangCompileTarget, SlangCompileTarget, SlangPassThrough>)(lpVtbl[24]))((IGlobalSession*)Unsafe.AsPointer(ref this), source, target);
        }

        public void getCompilerElapsedTime(double* outTotalTime, double* outDownstreamTime)
        {
            ((delegate* unmanaged[Stdcall]<IGlobalSession*, double*, double*, void>)(lpVtbl[25]))((IGlobalSession*)Unsafe.AsPointer(ref this), outTotalTime, outDownstreamTime);
        }

        [return: NativeTypeName("SlangResult")]
        public int setSPIRVCoreGrammar([NativeTypeName("const char *")] sbyte* jsonPath)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, sbyte*, int>)(lpVtbl[26]))((IGlobalSession*)Unsafe.AsPointer(ref this), jsonPath);
        }

        [return: NativeTypeName("SlangResult")]
        public int parseCommandLineArguments(int argc, [NativeTypeName("const char *const *")] sbyte** argv, [NativeTypeName("slang::SessionDesc *")] SessionDesc* outSessionDesc, ISlangUnknown** outAuxAllocation)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, int, sbyte**, SessionDesc*, ISlangUnknown**, int>)(lpVtbl[27]))((IGlobalSession*)Unsafe.AsPointer(ref this), argc, argv, outSessionDesc, outAuxAllocation);
        }

        [return: NativeTypeName("SlangResult")]
        public int getSessionDescDigest([NativeTypeName("slang::SessionDesc *")] SessionDesc* sessionDesc, ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, SessionDesc*, ISlangBlob**, int>)(lpVtbl[28]))((IGlobalSession*)Unsafe.AsPointer(ref this), sessionDesc, outBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int compileBuiltinModule([NativeTypeName("slang::BuiltinModuleName")] BuiltinModuleName module, [NativeTypeName("slang::CompileCoreModuleFlags")] uint flags)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, BuiltinModuleName, uint, int>)(lpVtbl[29]))((IGlobalSession*)Unsafe.AsPointer(ref this), module, flags);
        }

        [return: NativeTypeName("SlangResult")]
        public int loadBuiltinModule([NativeTypeName("slang::BuiltinModuleName")] BuiltinModuleName module, [NativeTypeName("const void *")] void* moduleData, [NativeTypeName("size_t")] nuint sizeInBytes)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, BuiltinModuleName, void*, nuint, int>)(lpVtbl[30]))((IGlobalSession*)Unsafe.AsPointer(ref this), module, moduleData, sizeInBytes);
        }

        [return: NativeTypeName("SlangResult")]
        public int saveBuiltinModule([NativeTypeName("slang::BuiltinModuleName")] BuiltinModuleName module, SlangArchiveType archiveType, ISlangBlob** outBlob)
        {
            return ((delegate* unmanaged[Stdcall]<IGlobalSession*, BuiltinModuleName, SlangArchiveType, ISlangBlob**, int>)(lpVtbl[31]))((IGlobalSession*)Unsafe.AsPointer(ref this), module, archiveType, outBlob);
        }
    }

    public unsafe partial struct TargetDesc
    {
        [NativeTypeName("size_t")]
        public nuint structureSize;

        public SlangCompileTarget format;

        public SlangProfileID profile;

        [NativeTypeName("SlangTargetFlags")]
        public uint flags;

        public SlangFloatingPointMode floatingPointMode;

        public SlangLineDirectiveMode lineDirectiveMode;

        [NativeTypeName("bool")]
        public byte forceGLSLScalarBufferLayout;

        [NativeTypeName("const CompilerOptionEntry *")]
        public CompilerOptionEntry* compilerOptionEntries;

        [NativeTypeName("uint32_t")]
        public uint compilerOptionEntryCount;
    }

    public enum Session
    {
        kSessionFlags_None = 0,
    }

    public unsafe partial struct PreprocessorMacroDesc
    {
        [NativeTypeName("const char *")]
        public sbyte* name;

        [NativeTypeName("const char *")]
        public sbyte* value;
    }

    public unsafe partial struct SessionDesc
    {
        [NativeTypeName("size_t")]
        public nuint structureSize;

        [NativeTypeName("const TargetDesc *")]
        public TargetDesc* targets;

        [NativeTypeName("SlangInt")]
        public long targetCount;

        [NativeTypeName("slang::SessionFlags")]
        public uint flags;

        public SlangMatrixLayoutMode defaultMatrixLayoutMode;

        [NativeTypeName("const char *const *")]
        public sbyte** searchPaths;

        [NativeTypeName("SlangInt")]
        public long searchPathCount;

        [NativeTypeName("const PreprocessorMacroDesc *")]
        public PreprocessorMacroDesc* preprocessorMacros;

        [NativeTypeName("SlangInt")]
        public long preprocessorMacroCount;

        public ISlangFileSystem* fileSystem;

        [NativeTypeName("bool")]
        public byte enableEffectAnnotations;

        [NativeTypeName("bool")]
        public byte allowGLSLSyntax;

        [NativeTypeName("slang::CompilerOptionEntry *")]
        public CompilerOptionEntry* compilerOptionEntries;

        [NativeTypeName("uint32_t")]
        public uint compilerOptionEntryCount;

        [NativeTypeName("bool")]
        public byte skipSPIRVValidation;
    }

    public enum ContainerType
    {
        None,
        UnsizedArray,
        StructuredBuffer,
        ConstantBuffer,
        ParameterBlock,
    }

    [NativeTypeName("struct ISession : ISlangUnknown")]
    public unsafe partial struct ISession
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, SlangUUID*, void**, int>)(lpVtbl[0]))((ISession*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, uint>)(lpVtbl[1]))((ISession*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, uint>)(lpVtbl[2]))((ISession*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::IGlobalSession *")]
        public IGlobalSession* getGlobalSession()
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, IGlobalSession*>)(lpVtbl[3]))((ISession*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::IModule *")]
        public IModule* loadModule([NativeTypeName("const char *")] sbyte* moduleName, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, sbyte*, ISlangBlob**, IModule*>)(lpVtbl[4]))((ISession*)Unsafe.AsPointer(ref this), moduleName, outDiagnostics);
        }

        [return: NativeTypeName("slang::IModule *")]
        public IModule* loadModuleFromSource([NativeTypeName("const char *")] sbyte* moduleName, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("slang::IBlob *")] ISlangBlob* source, [NativeTypeName("slang::IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, sbyte*, sbyte*, ISlangBlob*, ISlangBlob**, IModule*>)(lpVtbl[5]))((ISession*)Unsafe.AsPointer(ref this), moduleName, path, source, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int createCompositeComponentType([NativeTypeName("IComponentType *const *")] IComponentType** componentTypes, [NativeTypeName("SlangInt")] long componentTypeCount, IComponentType** outCompositeComponentType, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, IComponentType**, long, IComponentType**, ISlangBlob**, int>)(lpVtbl[6]))((ISession*)Unsafe.AsPointer(ref this), componentTypes, componentTypeCount, outCompositeComponentType, outDiagnostics);
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* specializeType([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, [NativeTypeName("const SpecializationArg *")] SpecializationArg* specializationArgs, [NativeTypeName("SlangInt")] long specializationArgCount, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, TypeReflection*, SpecializationArg*, long, ISlangBlob**, TypeReflection*>)(lpVtbl[7]))((ISession*)Unsafe.AsPointer(ref this), type, specializationArgs, specializationArgCount, outDiagnostics);
        }

        [return: NativeTypeName("slang::TypeLayoutReflection *")]
        public TypeLayoutReflection* getTypeLayout([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, [NativeTypeName("SlangInt")] long targetIndex = 0, [NativeTypeName("slang::LayoutRules")] LayoutRules rules = Default, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, TypeReflection*, long, LayoutRules, ISlangBlob**, TypeLayoutReflection*>)(lpVtbl[8]))((ISession*)Unsafe.AsPointer(ref this), type, targetIndex, rules, outDiagnostics);
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getContainerType([NativeTypeName("slang::TypeReflection *")] TypeReflection* elementType, [NativeTypeName("slang::ContainerType")] ContainerType containerType, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, TypeReflection*, ContainerType, ISlangBlob**, TypeReflection*>)(lpVtbl[9]))((ISession*)Unsafe.AsPointer(ref this), elementType, containerType, outDiagnostics);
        }

        [return: NativeTypeName("slang::TypeReflection *")]
        public TypeReflection* getDynamicType()
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, TypeReflection*>)(lpVtbl[10]))((ISession*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getTypeRTTIMangledName([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, ISlangBlob** outNameBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, TypeReflection*, ISlangBlob**, int>)(lpVtbl[11]))((ISession*)Unsafe.AsPointer(ref this), type, outNameBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTypeConformanceWitnessMangledName([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, [NativeTypeName("slang::TypeReflection *")] TypeReflection* interfaceType, ISlangBlob** outNameBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, TypeReflection*, TypeReflection*, ISlangBlob**, int>)(lpVtbl[12]))((ISession*)Unsafe.AsPointer(ref this), type, interfaceType, outNameBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTypeConformanceWitnessSequentialID([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, [NativeTypeName("slang::TypeReflection *")] TypeReflection* interfaceType, [NativeTypeName("uint32_t *")] uint* outId)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, TypeReflection*, TypeReflection*, uint*, int>)(lpVtbl[13]))((ISession*)Unsafe.AsPointer(ref this), type, interfaceType, outId);
        }

        [return: NativeTypeName("SlangResult")]
        public int createCompileRequest([NativeTypeName("SlangCompileRequest **")] ICompileRequest** outCompileRequest)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, ICompileRequest**, int>)(lpVtbl[14]))((ISession*)Unsafe.AsPointer(ref this), outCompileRequest);
        }

        [return: NativeTypeName("SlangResult")]
        public int createTypeConformanceComponentType([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, [NativeTypeName("slang::TypeReflection *")] TypeReflection* interfaceType, ITypeConformance** outConformance, [NativeTypeName("SlangInt")] long conformanceIdOverride, ISlangBlob** outDiagnostics)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, TypeReflection*, TypeReflection*, ITypeConformance**, long, ISlangBlob**, int>)(lpVtbl[15]))((ISession*)Unsafe.AsPointer(ref this), type, interfaceType, outConformance, conformanceIdOverride, outDiagnostics);
        }

        [return: NativeTypeName("slang::IModule *")]
        public IModule* loadModuleFromIRBlob([NativeTypeName("const char *")] sbyte* moduleName, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("slang::IBlob *")] ISlangBlob* source, [NativeTypeName("slang::IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, sbyte*, sbyte*, ISlangBlob*, ISlangBlob**, IModule*>)(lpVtbl[16]))((ISession*)Unsafe.AsPointer(ref this), moduleName, path, source, outDiagnostics);
        }

        [return: NativeTypeName("SlangInt")]
        public long getLoadedModuleCount()
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, long>)(lpVtbl[17]))((ISession*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::IModule *")]
        public IModule* getLoadedModule([NativeTypeName("SlangInt")] long index)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, long, IModule*>)(lpVtbl[18]))((ISession*)Unsafe.AsPointer(ref this), index);
        }

        public bool isBinaryModuleUpToDate([NativeTypeName("const char *")] sbyte* modulePath, [NativeTypeName("slang::IBlob *")] ISlangBlob* binaryModuleBlob)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, sbyte*, ISlangBlob*, byte>)(lpVtbl[19]))((ISession*)Unsafe.AsPointer(ref this), modulePath, binaryModuleBlob) != 0;
        }

        [return: NativeTypeName("slang::IModule *")]
        public IModule* loadModuleFromSourceString([NativeTypeName("const char *")] sbyte* moduleName, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("const char *")] sbyte* @string, [NativeTypeName("slang::IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, sbyte*, sbyte*, sbyte*, ISlangBlob**, IModule*>)(lpVtbl[20]))((ISession*)Unsafe.AsPointer(ref this), moduleName, path, @string, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getDynamicObjectRTTIBytes([NativeTypeName("slang::TypeReflection *")] TypeReflection* type, [NativeTypeName("slang::TypeReflection *")] TypeReflection* interfaceType, [NativeTypeName("uint32_t *")] uint* outRTTIDataBuffer, [NativeTypeName("uint32_t")] uint bufferSizeInBytes)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, TypeReflection*, TypeReflection*, uint*, uint, int>)(lpVtbl[21]))((ISession*)Unsafe.AsPointer(ref this), type, interfaceType, outRTTIDataBuffer, bufferSizeInBytes);
        }

        [return: NativeTypeName("SlangResult")]
        public int loadModuleInfoFromIRBlob([NativeTypeName("slang::IBlob *")] ISlangBlob* source, [NativeTypeName("SlangInt &")] long* outModuleVersion, [NativeTypeName("const char *&")] sbyte** outModuleCompilerVersion, [NativeTypeName("const char *&")] sbyte** outModuleName)
        {
            return ((delegate* unmanaged[Stdcall]<ISession*, ISlangBlob*, long*, sbyte**, sbyte**, int>)(lpVtbl[22]))((ISession*)Unsafe.AsPointer(ref this), source, outModuleVersion, outModuleCompilerVersion, outModuleName);
        }
    }

    [NativeTypeName("struct IMetadata : ISlangCastable")]
    public unsafe partial struct IMetadata
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<IMetadata*, SlangUUID*, void**, int>)(lpVtbl[0]))((IMetadata*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<IMetadata*, uint>)(lpVtbl[1]))((IMetadata*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<IMetadata*, uint>)(lpVtbl[2]))((IMetadata*)Unsafe.AsPointer(ref this));
        }

        public void* castAs([NativeTypeName("const SlangUUID &")] SlangUUID* guid)
        {
            return ((delegate* unmanaged[Stdcall]<IMetadata*, SlangUUID*, void*>)(lpVtbl[3]))((IMetadata*)Unsafe.AsPointer(ref this), guid);
        }

        [return: NativeTypeName("SlangResult")]
        public int isParameterLocationUsed(SlangParameterCategory category, [NativeTypeName("SlangUInt")] ulong spaceIndex, [NativeTypeName("SlangUInt")] ulong registerIndex, [NativeTypeName("bool &")] bool* outUsed)
        {
            return ((delegate* unmanaged[Thiscall]<IMetadata*, SlangParameterCategory, ulong, ulong, bool*, int>)(lpVtbl[4]))((IMetadata*)Unsafe.AsPointer(ref this), category, spaceIndex, registerIndex, outUsed);
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getDebugBuildIdentifier()
        {
            return ((delegate* unmanaged[Stdcall]<IMetadata*, sbyte*>)(lpVtbl[5]))((IMetadata*)Unsafe.AsPointer(ref this));
        }
    }

    [NativeTypeName("struct ICompileResult : ISlangCastable")]
    public unsafe partial struct ICompileResult
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileResult*, SlangUUID*, void**, int>)(lpVtbl[0]))((ICompileResult*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileResult*, uint>)(lpVtbl[1]))((ICompileResult*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileResult*, uint>)(lpVtbl[2]))((ICompileResult*)Unsafe.AsPointer(ref this));
        }

        public void* castAs([NativeTypeName("const SlangUUID &")] SlangUUID* guid)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileResult*, SlangUUID*, void*>)(lpVtbl[3]))((ICompileResult*)Unsafe.AsPointer(ref this), guid);
        }

        [return: NativeTypeName("uint32_t")]
        public uint getItemCount()
        {
            return ((delegate* unmanaged[Stdcall]<ICompileResult*, uint>)(lpVtbl[4]))((ICompileResult*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getItemData([NativeTypeName("uint32_t")] uint index, [NativeTypeName("IBlob **")] ISlangBlob** outblob)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileResult*, uint, ISlangBlob**, int>)(lpVtbl[5]))((ICompileResult*)Unsafe.AsPointer(ref this), index, outblob);
        }

        [return: NativeTypeName("SlangResult")]
        public int getMetadata(IMetadata** outMetadata)
        {
            return ((delegate* unmanaged[Stdcall]<ICompileResult*, IMetadata**, int>)(lpVtbl[6]))((ICompileResult*)Unsafe.AsPointer(ref this), outMetadata);
        }
    }

    [NativeTypeName("struct IComponentType : ISlangUnknown")]
    public unsafe partial struct IComponentType
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, SlangUUID*, void**, int>)(lpVtbl[0]))((IComponentType*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, uint>)(lpVtbl[1]))((IComponentType*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, uint>)(lpVtbl[2]))((IComponentType*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::ISession *")]
        public ISession* getSession()
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, ISession*>)(lpVtbl[3]))((IComponentType*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::ProgramLayout *")]
        public ShaderReflection* getLayout([NativeTypeName("SlangInt")] long targetIndex = 0, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, long, ISlangBlob**, ShaderReflection*>)(lpVtbl[4]))((IComponentType*)Unsafe.AsPointer(ref this), targetIndex, outDiagnostics);
        }

        [return: NativeTypeName("SlangInt")]
        public long getSpecializationParamCount()
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, long>)(lpVtbl[5]))((IComponentType*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointCode([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outCode, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, long, long, ISlangBlob**, ISlangBlob**, int>)(lpVtbl[6]))((IComponentType*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outCode, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getResultAsFileSystem([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, ISlangMutableFileSystem** outFileSystem)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, long, long, ISlangMutableFileSystem**, int>)(lpVtbl[7]))((IComponentType*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outFileSystem);
        }

        public void getEntryPointHash([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outHash)
        {
            ((delegate* unmanaged[Stdcall]<IComponentType*, long, long, ISlangBlob**, void>)(lpVtbl[8]))((IComponentType*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outHash);
        }

        [return: NativeTypeName("SlangResult")]
        public int specialize([NativeTypeName("const SpecializationArg *")] SpecializationArg* specializationArgs, [NativeTypeName("SlangInt")] long specializationArgCount, IComponentType** outSpecializedComponentType, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, SpecializationArg*, long, IComponentType**, ISlangBlob**, int>)(lpVtbl[9]))((IComponentType*)Unsafe.AsPointer(ref this), specializationArgs, specializationArgCount, outSpecializedComponentType, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int link(IComponentType** outLinkedComponentType, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, IComponentType**, ISlangBlob**, int>)(lpVtbl[10]))((IComponentType*)Unsafe.AsPointer(ref this), outLinkedComponentType, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointHostCallable(int entryPointIndex, int targetIndex, ISlangSharedLibrary** outSharedLibrary, [NativeTypeName("slang::IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, int, int, ISlangSharedLibrary**, ISlangBlob**, int>)(lpVtbl[11]))((IComponentType*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outSharedLibrary, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int renameEntryPoint([NativeTypeName("const char *")] sbyte* newName, IComponentType** outEntryPoint)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, sbyte*, IComponentType**, int>)(lpVtbl[12]))((IComponentType*)Unsafe.AsPointer(ref this), newName, outEntryPoint);
        }

        [return: NativeTypeName("SlangResult")]
        public int linkWithOptions(IComponentType** outLinkedComponentType, [NativeTypeName("uint32_t")] uint compilerOptionEntryCount, [NativeTypeName("slang::CompilerOptionEntry *")] CompilerOptionEntry* compilerOptionEntries, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, IComponentType**, uint, CompilerOptionEntry*, ISlangBlob**, int>)(lpVtbl[13]))((IComponentType*)Unsafe.AsPointer(ref this), outLinkedComponentType, compilerOptionEntryCount, compilerOptionEntries, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetCode([NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outCode, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, long, ISlangBlob**, ISlangBlob**, int>)(lpVtbl[14]))((IComponentType*)Unsafe.AsPointer(ref this), targetIndex, outCode, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetMetadata([NativeTypeName("SlangInt")] long targetIndex, IMetadata** outMetadata, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, long, IMetadata**, ISlangBlob**, int>)(lpVtbl[15]))((IComponentType*)Unsafe.AsPointer(ref this), targetIndex, outMetadata, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointMetadata([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, IMetadata** outMetadata, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType*, long, long, IMetadata**, ISlangBlob**, int>)(lpVtbl[16]))((IComponentType*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outMetadata, outDiagnostics);
        }
    }

    [NativeTypeName("struct IEntryPoint : slang::IComponentType")]
    public unsafe partial struct IEntryPoint
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, SlangUUID*, void**, int>)(lpVtbl[0]))((IEntryPoint*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, uint>)(lpVtbl[1]))((IEntryPoint*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, uint>)(lpVtbl[2]))((IEntryPoint*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::ISession *")]
        public ISession* getSession()
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, ISession*>)(lpVtbl[3]))((IEntryPoint*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::ProgramLayout *")]
        public ShaderReflection* getLayout([NativeTypeName("SlangInt")] long targetIndex = 0, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, long, ISlangBlob**, ShaderReflection*>)(lpVtbl[4]))((IEntryPoint*)Unsafe.AsPointer(ref this), targetIndex, outDiagnostics);
        }

        [return: NativeTypeName("SlangInt")]
        public long getSpecializationParamCount()
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, long>)(lpVtbl[5]))((IEntryPoint*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointCode([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outCode, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, long, long, ISlangBlob**, ISlangBlob**, int>)(lpVtbl[6]))((IEntryPoint*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outCode, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getResultAsFileSystem([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, ISlangMutableFileSystem** outFileSystem)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, long, long, ISlangMutableFileSystem**, int>)(lpVtbl[7]))((IEntryPoint*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outFileSystem);
        }

        public void getEntryPointHash([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outHash)
        {
            ((delegate* unmanaged[Stdcall]<IEntryPoint*, long, long, ISlangBlob**, void>)(lpVtbl[8]))((IEntryPoint*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outHash);
        }

        [return: NativeTypeName("SlangResult")]
        public int specialize([NativeTypeName("const SpecializationArg *")] SpecializationArg* specializationArgs, [NativeTypeName("SlangInt")] long specializationArgCount, IComponentType** outSpecializedComponentType, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, SpecializationArg*, long, IComponentType**, ISlangBlob**, int>)(lpVtbl[9]))((IEntryPoint*)Unsafe.AsPointer(ref this), specializationArgs, specializationArgCount, outSpecializedComponentType, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int link(IComponentType** outLinkedComponentType, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, IComponentType**, ISlangBlob**, int>)(lpVtbl[10]))((IEntryPoint*)Unsafe.AsPointer(ref this), outLinkedComponentType, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointHostCallable(int entryPointIndex, int targetIndex, ISlangSharedLibrary** outSharedLibrary, [NativeTypeName("slang::IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, int, int, ISlangSharedLibrary**, ISlangBlob**, int>)(lpVtbl[11]))((IEntryPoint*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outSharedLibrary, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int renameEntryPoint([NativeTypeName("const char *")] sbyte* newName, IComponentType** outEntryPoint)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, sbyte*, IComponentType**, int>)(lpVtbl[12]))((IEntryPoint*)Unsafe.AsPointer(ref this), newName, outEntryPoint);
        }

        [return: NativeTypeName("SlangResult")]
        public int linkWithOptions(IComponentType** outLinkedComponentType, [NativeTypeName("uint32_t")] uint compilerOptionEntryCount, [NativeTypeName("slang::CompilerOptionEntry *")] CompilerOptionEntry* compilerOptionEntries, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, IComponentType**, uint, CompilerOptionEntry*, ISlangBlob**, int>)(lpVtbl[13]))((IEntryPoint*)Unsafe.AsPointer(ref this), outLinkedComponentType, compilerOptionEntryCount, compilerOptionEntries, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetCode([NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outCode, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, long, ISlangBlob**, ISlangBlob**, int>)(lpVtbl[14]))((IEntryPoint*)Unsafe.AsPointer(ref this), targetIndex, outCode, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetMetadata([NativeTypeName("SlangInt")] long targetIndex, IMetadata** outMetadata, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, long, IMetadata**, ISlangBlob**, int>)(lpVtbl[15]))((IEntryPoint*)Unsafe.AsPointer(ref this), targetIndex, outMetadata, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointMetadata([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, IMetadata** outMetadata, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, long, long, IMetadata**, ISlangBlob**, int>)(lpVtbl[16]))((IEntryPoint*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outMetadata, outDiagnostics);
        }

        [return: NativeTypeName("slang::FunctionReflection *")]
        public FunctionReflection* getFunctionReflection()
        {
            return ((delegate* unmanaged[Stdcall]<IEntryPoint*, FunctionReflection*>)(lpVtbl[17]))((IEntryPoint*)Unsafe.AsPointer(ref this));
        }
    }

    [NativeTypeName("struct ITypeConformance : slang::IComponentType")]
    public unsafe partial struct ITypeConformance
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, SlangUUID*, void**, int>)(lpVtbl[0]))((ITypeConformance*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, uint>)(lpVtbl[1]))((ITypeConformance*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, uint>)(lpVtbl[2]))((ITypeConformance*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::ISession *")]
        public ISession* getSession()
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, ISession*>)(lpVtbl[3]))((ITypeConformance*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::ProgramLayout *")]
        public ShaderReflection* getLayout([NativeTypeName("SlangInt")] long targetIndex = 0, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, long, ISlangBlob**, ShaderReflection*>)(lpVtbl[4]))((ITypeConformance*)Unsafe.AsPointer(ref this), targetIndex, outDiagnostics);
        }

        [return: NativeTypeName("SlangInt")]
        public long getSpecializationParamCount()
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, long>)(lpVtbl[5]))((ITypeConformance*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointCode([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outCode, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, long, long, ISlangBlob**, ISlangBlob**, int>)(lpVtbl[6]))((ITypeConformance*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outCode, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getResultAsFileSystem([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, ISlangMutableFileSystem** outFileSystem)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, long, long, ISlangMutableFileSystem**, int>)(lpVtbl[7]))((ITypeConformance*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outFileSystem);
        }

        public void getEntryPointHash([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outHash)
        {
            ((delegate* unmanaged[Stdcall]<ITypeConformance*, long, long, ISlangBlob**, void>)(lpVtbl[8]))((ITypeConformance*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outHash);
        }

        [return: NativeTypeName("SlangResult")]
        public int specialize([NativeTypeName("const SpecializationArg *")] SpecializationArg* specializationArgs, [NativeTypeName("SlangInt")] long specializationArgCount, IComponentType** outSpecializedComponentType, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, SpecializationArg*, long, IComponentType**, ISlangBlob**, int>)(lpVtbl[9]))((ITypeConformance*)Unsafe.AsPointer(ref this), specializationArgs, specializationArgCount, outSpecializedComponentType, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int link(IComponentType** outLinkedComponentType, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, IComponentType**, ISlangBlob**, int>)(lpVtbl[10]))((ITypeConformance*)Unsafe.AsPointer(ref this), outLinkedComponentType, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointHostCallable(int entryPointIndex, int targetIndex, ISlangSharedLibrary** outSharedLibrary, [NativeTypeName("slang::IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, int, int, ISlangSharedLibrary**, ISlangBlob**, int>)(lpVtbl[11]))((ITypeConformance*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outSharedLibrary, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int renameEntryPoint([NativeTypeName("const char *")] sbyte* newName, IComponentType** outEntryPoint)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, sbyte*, IComponentType**, int>)(lpVtbl[12]))((ITypeConformance*)Unsafe.AsPointer(ref this), newName, outEntryPoint);
        }

        [return: NativeTypeName("SlangResult")]
        public int linkWithOptions(IComponentType** outLinkedComponentType, [NativeTypeName("uint32_t")] uint compilerOptionEntryCount, [NativeTypeName("slang::CompilerOptionEntry *")] CompilerOptionEntry* compilerOptionEntries, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, IComponentType**, uint, CompilerOptionEntry*, ISlangBlob**, int>)(lpVtbl[13]))((ITypeConformance*)Unsafe.AsPointer(ref this), outLinkedComponentType, compilerOptionEntryCount, compilerOptionEntries, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetCode([NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outCode, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, long, ISlangBlob**, ISlangBlob**, int>)(lpVtbl[14]))((ITypeConformance*)Unsafe.AsPointer(ref this), targetIndex, outCode, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetMetadata([NativeTypeName("SlangInt")] long targetIndex, IMetadata** outMetadata, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, long, IMetadata**, ISlangBlob**, int>)(lpVtbl[15]))((ITypeConformance*)Unsafe.AsPointer(ref this), targetIndex, outMetadata, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointMetadata([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, IMetadata** outMetadata, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<ITypeConformance*, long, long, IMetadata**, ISlangBlob**, int>)(lpVtbl[16]))((ITypeConformance*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outMetadata, outDiagnostics);
        }
    }

    [NativeTypeName("struct IComponentType2 : ISlangUnknown")]
    public unsafe partial struct IComponentType2
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType2*, SlangUUID*, void**, int>)(lpVtbl[0]))((IComponentType2*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType2*, uint>)(lpVtbl[1]))((IComponentType2*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType2*, uint>)(lpVtbl[2]))((IComponentType2*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetCompileResult([NativeTypeName("SlangInt")] long targetIndex, ICompileResult** outCompileResult, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType2*, long, ICompileResult**, ISlangBlob**, int>)(lpVtbl[3]))((IComponentType2*)Unsafe.AsPointer(ref this), targetIndex, outCompileResult, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointCompileResult([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, ICompileResult** outCompileResult, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType2*, long, long, ICompileResult**, ISlangBlob**, int>)(lpVtbl[4]))((IComponentType2*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outCompileResult, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetHostCallable(int targetIndex, ISlangSharedLibrary** outSharedLibrary, [NativeTypeName("slang::IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IComponentType2*, int, ISlangSharedLibrary**, ISlangBlob**, int>)(lpVtbl[5]))((IComponentType2*)Unsafe.AsPointer(ref this), targetIndex, outSharedLibrary, outDiagnostics);
        }
    }

    [NativeTypeName("struct IModule : slang::IComponentType")]
    public unsafe partial struct IModule
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, SlangUUID*, void**, int>)(lpVtbl[0]))((IModule*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, uint>)(lpVtbl[1]))((IModule*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, uint>)(lpVtbl[2]))((IModule*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::ISession *")]
        public ISession* getSession()
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, ISession*>)(lpVtbl[3]))((IModule*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("slang::ProgramLayout *")]
        public ShaderReflection* getLayout([NativeTypeName("SlangInt")] long targetIndex = 0, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, long, ISlangBlob**, ShaderReflection*>)(lpVtbl[4]))((IModule*)Unsafe.AsPointer(ref this), targetIndex, outDiagnostics);
        }

        [return: NativeTypeName("SlangInt")]
        public long getSpecializationParamCount()
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, long>)(lpVtbl[5]))((IModule*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointCode([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outCode, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, long, long, ISlangBlob**, ISlangBlob**, int>)(lpVtbl[6]))((IModule*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outCode, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getResultAsFileSystem([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, ISlangMutableFileSystem** outFileSystem)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, long, long, ISlangMutableFileSystem**, int>)(lpVtbl[7]))((IModule*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outFileSystem);
        }

        public void getEntryPointHash([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outHash)
        {
            ((delegate* unmanaged[Stdcall]<IModule*, long, long, ISlangBlob**, void>)(lpVtbl[8]))((IModule*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outHash);
        }

        [return: NativeTypeName("SlangResult")]
        public int specialize([NativeTypeName("const SpecializationArg *")] SpecializationArg* specializationArgs, [NativeTypeName("SlangInt")] long specializationArgCount, IComponentType** outSpecializedComponentType, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, SpecializationArg*, long, IComponentType**, ISlangBlob**, int>)(lpVtbl[9]))((IModule*)Unsafe.AsPointer(ref this), specializationArgs, specializationArgCount, outSpecializedComponentType, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int link(IComponentType** outLinkedComponentType, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, IComponentType**, ISlangBlob**, int>)(lpVtbl[10]))((IModule*)Unsafe.AsPointer(ref this), outLinkedComponentType, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointHostCallable(int entryPointIndex, int targetIndex, ISlangSharedLibrary** outSharedLibrary, [NativeTypeName("slang::IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, int, int, ISlangSharedLibrary**, ISlangBlob**, int>)(lpVtbl[11]))((IModule*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outSharedLibrary, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int renameEntryPoint([NativeTypeName("const char *")] sbyte* newName, IComponentType** outEntryPoint)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, sbyte*, IComponentType**, int>)(lpVtbl[12]))((IModule*)Unsafe.AsPointer(ref this), newName, outEntryPoint);
        }

        [return: NativeTypeName("SlangResult")]
        public int linkWithOptions(IComponentType** outLinkedComponentType, [NativeTypeName("uint32_t")] uint compilerOptionEntryCount, [NativeTypeName("slang::CompilerOptionEntry *")] CompilerOptionEntry* compilerOptionEntries, ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, IComponentType**, uint, CompilerOptionEntry*, ISlangBlob**, int>)(lpVtbl[13]))((IModule*)Unsafe.AsPointer(ref this), outLinkedComponentType, compilerOptionEntryCount, compilerOptionEntries, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetCode([NativeTypeName("SlangInt")] long targetIndex, [NativeTypeName("IBlob **")] ISlangBlob** outCode, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, long, ISlangBlob**, ISlangBlob**, int>)(lpVtbl[14]))((IModule*)Unsafe.AsPointer(ref this), targetIndex, outCode, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getTargetMetadata([NativeTypeName("SlangInt")] long targetIndex, IMetadata** outMetadata, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, long, IMetadata**, ISlangBlob**, int>)(lpVtbl[15]))((IModule*)Unsafe.AsPointer(ref this), targetIndex, outMetadata, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getEntryPointMetadata([NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, IMetadata** outMetadata, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, long, long, IMetadata**, ISlangBlob**, int>)(lpVtbl[16]))((IModule*)Unsafe.AsPointer(ref this), entryPointIndex, targetIndex, outMetadata, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int findEntryPointByName([NativeTypeName("const char *")] sbyte* name, IEntryPoint** outEntryPoint)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, sbyte*, IEntryPoint**, int>)(lpVtbl[17]))((IModule*)Unsafe.AsPointer(ref this), name, outEntryPoint);
        }

        [return: NativeTypeName("SlangInt32")]
        public int getDefinedEntryPointCount()
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, int>)(lpVtbl[18]))((IModule*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getDefinedEntryPoint([NativeTypeName("SlangInt32")] int index, IEntryPoint** outEntryPoint)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, int, IEntryPoint**, int>)(lpVtbl[19]))((IModule*)Unsafe.AsPointer(ref this), index, outEntryPoint);
        }

        [return: NativeTypeName("SlangResult")]
        public int serialize(ISlangBlob** outSerializedBlob)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, ISlangBlob**, int>)(lpVtbl[20]))((IModule*)Unsafe.AsPointer(ref this), outSerializedBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int writeToFile([NativeTypeName("const char *")] sbyte* fileName)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, sbyte*, int>)(lpVtbl[21]))((IModule*)Unsafe.AsPointer(ref this), fileName);
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getName()
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, sbyte*>)(lpVtbl[22]))((IModule*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getFilePath()
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, sbyte*>)(lpVtbl[23]))((IModule*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getUniqueIdentity()
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, sbyte*>)(lpVtbl[24]))((IModule*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int findAndCheckEntryPoint([NativeTypeName("const char *")] sbyte* name, SlangStage stage, IEntryPoint** outEntryPoint, ISlangBlob** outDiagnostics)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, sbyte*, SlangStage, IEntryPoint**, ISlangBlob**, int>)(lpVtbl[25]))((IModule*)Unsafe.AsPointer(ref this), name, stage, outEntryPoint, outDiagnostics);
        }

        [return: NativeTypeName("SlangInt32")]
        public int getDependencyFileCount()
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, int>)(lpVtbl[26]))((IModule*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("const char *")]
        public sbyte* getDependencyFilePath([NativeTypeName("SlangInt32")] int index)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, int, sbyte*>)(lpVtbl[27]))((IModule*)Unsafe.AsPointer(ref this), index);
        }

        [return: NativeTypeName("slang::DeclReflection *")]
        public DeclReflection* getModuleReflection()
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, DeclReflection*>)(lpVtbl[28]))((IModule*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int disassemble([NativeTypeName("slang::IBlob **")] ISlangBlob** outDisassembledBlob)
        {
            return ((delegate* unmanaged[Stdcall]<IModule*, ISlangBlob**, int>)(lpVtbl[29]))((IModule*)Unsafe.AsPointer(ref this), outDisassembledBlob);
        }
    }

    [NativeTypeName("struct IModulePrecompileService_Experimental : ISlangUnknown")]
    public unsafe partial struct IModulePrecompileService_Experimental
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<IModulePrecompileService_Experimental*, SlangUUID*, void**, int>)(lpVtbl[0]))((IModulePrecompileService_Experimental*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<IModulePrecompileService_Experimental*, uint>)(lpVtbl[1]))((IModulePrecompileService_Experimental*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<IModulePrecompileService_Experimental*, uint>)(lpVtbl[2]))((IModulePrecompileService_Experimental*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int precompileForTarget(SlangCompileTarget target, ISlangBlob** outDiagnostics)
        {
            return ((delegate* unmanaged[Stdcall]<IModulePrecompileService_Experimental*, SlangCompileTarget, ISlangBlob**, int>)(lpVtbl[3]))((IModulePrecompileService_Experimental*)Unsafe.AsPointer(ref this), target, outDiagnostics);
        }

        [return: NativeTypeName("SlangResult")]
        public int getPrecompiledTargetCode(SlangCompileTarget target, [NativeTypeName("IBlob **")] ISlangBlob** outCode, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModulePrecompileService_Experimental*, SlangCompileTarget, ISlangBlob**, ISlangBlob**, int>)(lpVtbl[4]))((IModulePrecompileService_Experimental*)Unsafe.AsPointer(ref this), target, outCode, outDiagnostics);
        }

        [return: NativeTypeName("SlangInt")]
        public long getModuleDependencyCount()
        {
            return ((delegate* unmanaged[Stdcall]<IModulePrecompileService_Experimental*, long>)(lpVtbl[5]))((IModulePrecompileService_Experimental*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int getModuleDependency([NativeTypeName("SlangInt")] long dependencyIndex, IModule** outModule, [NativeTypeName("IBlob **")] ISlangBlob** outDiagnostics = null)
        {
            return ((delegate* unmanaged[Stdcall]<IModulePrecompileService_Experimental*, long, IModule**, ISlangBlob**, int>)(lpVtbl[6]))((IModulePrecompileService_Experimental*)Unsafe.AsPointer(ref this), dependencyIndex, outModule, outDiagnostics);
        }
    }

    public unsafe partial struct SpecializationArg
    {
        [NativeTypeName("slang::SpecializationArg::Kind")]
        public Kind kind;

        [NativeTypeName("__AnonymousRecord_slang_L6626_C5")]
        public _Anonymous_e__Union Anonymous;

        [UnscopedRef]
        public ref TypeReflection* type
        {
            get
            {
                return ref Anonymous.type;
            }
        }

        [UnscopedRef]
        public ref sbyte* expr
        {
            get
            {
                return ref Anonymous.expr;
            }
        }

        [return: NativeTypeName("slang::SpecializationArg")]
        public static SpecializationArg fromType([NativeTypeName("slang::TypeReflection *")] TypeReflection* inType)
        {
            SpecializationArg rs = new SpecializationArg();

            rs.kind = Type;
            rs.Anonymous.type = inType;
            return rs;
        }

        [return: NativeTypeName("slang::SpecializationArg")]
        public static SpecializationArg fromExpr([NativeTypeName("const char *")] sbyte* inExpr)
        {
            SpecializationArg rs = new SpecializationArg();

            rs.kind = Expr;
            rs.Anonymous.expr = inExpr;
            return rs;
        }

        [NativeTypeName("int32_t")]
        public enum Kind
        {
            Unknown,
            Type,
            Expr,
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("slang::TypeReflection *")]
            public TypeReflection* type;

            [FieldOffset(0)]
            [NativeTypeName("const char *")]
            public sbyte* expr;
        }
    }

    public enum SlangLanguageVersion
    {
        SLANG_LANGUAGE_VERSION_UNKNOWN = 0,
        SLANG_LANGUAGE_VERSION_LEGACY = 2018,
        SLANG_LANGUAGE_VERSION_2025 = 2025,
        SLANG_LANGUAGE_VERSION_2026 = 2026,
        SLANG_LANGAUGE_VERSION_DEFAULT = SLANG_LANGUAGE_VERSION_LEGACY,
        SLANG_LANGUAGE_VERSION_LATEST = SLANG_LANGUAGE_VERSION_2026,
    }

    public partial struct SlangGlobalSessionDesc
    {
        [NativeTypeName("uint32_t")]
        public uint structureSize;

        [NativeTypeName("uint32_t")]
        public uint apiVersion;

        [NativeTypeName("uint32_t")]
        public uint minLanguageVersion;

        [NativeTypeName("bool")]
        public byte enableGLSL;

        [NativeTypeName("uint32_t[16]")]
        public _reserved_e__FixedBuffer reserved;

        [InlineArray(16)]
        public partial struct _reserved_e__FixedBuffer
        {
            public uint e0;
        }
    }

    public enum OperandDataType
    {
        General = 0,
        Int32 = 1,
        Int64 = 2,
        Float32 = 3,
        Float64 = 4,
        String = 5,
    }

    public unsafe partial struct VMExecOperand
    {
        [NativeTypeName("uint8_t **")]
        public byte** section;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 8")]
        public uint type
        {
            readonly get
            {
                return _bitfield & 0xFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~0xFFu) | (value & 0xFFu);
            }
        }

        [NativeTypeName("uint32_t : 24")]
        public uint size
        {
            readonly get
            {
                return (_bitfield >> 8) & 0xFFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0xFFFFFFu << 8)) | ((value & 0xFFFFFFu) << 8);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint offset;

        public readonly void* getPtr()
        {
            return *section + offset;
        }

        [return: NativeTypeName("slang::OperandDataType")]
        public readonly OperandDataType getType()
        {
            return (OperandDataType)(type);
        }
    }

    public unsafe partial struct VMExecInstHeader
    {
        [NativeTypeName("slang::VMExtFunction")]
        public delegate* unmanaged[Cdecl]<IByteCodeRunner*, VMExecInstHeader*, void*, void> functionPtr;

        [NativeTypeName("uint32_t")]
        public uint opcodeExtension;

        [NativeTypeName("uint32_t")]
        public uint operandCount;

        [return: NativeTypeName("slang::VMExecInstHeader *")]
        public VMExecInstHeader* getNextInst()
        {
            return (VMExecInstHeader*)((VMExecOperand*)(this + 1) + operandCount);
        }

        [return: NativeTypeName("slang::VMExecOperand &")]
        public readonly VMExecOperand* getOperand([NativeTypeName("SlangInt")] long index)
        {
            return ((VMExecOperand*)(this + 1) + index);
        }
    }

    public partial struct ByteCodeFuncInfo
    {
        [NativeTypeName("uint32_t")]
        public uint parameterCount;

        [NativeTypeName("uint32_t")]
        public uint returnValueSize;
    }

    public partial struct ByteCodeRunnerDesc
    {
        [NativeTypeName("size_t")]
        public nuint structSize;
    }

    [NativeTypeName("struct IByteCodeRunner : ISlangUnknown")]
    public unsafe partial struct IByteCodeRunner
    {
        public void** lpVtbl;

        public static SlangUUID getTypeGuid()
        {
            return new SlangUUID
            {
                data1 = 0x00000000,
                data2 = 0x0000,
                data3 = 0x0000,
                data4 = new byte[8]
                {
                    0xC0,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x46,
                },
            };
        }

        [return: NativeTypeName("SlangResult")]
        public int QueryInterface([NativeTypeName("const struct _GUID &")] _GUID* uuid, void** outObject)
        {
            return queryInterface(unchecked((SlangUUID*)(uuid)), outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint AddRef()
        {
            return addRef();
        }

        [return: NativeTypeName("uint32_t")]
        public uint Release()
        {
            return release();
        }

        [return: NativeTypeName("SlangResult")]
        public int queryInterface([NativeTypeName("const SlangUUID &")] SlangUUID* uuid, void** outObject)
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, SlangUUID*, void**, int>)(lpVtbl[0]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), uuid, outObject);
        }

        [return: NativeTypeName("uint32_t")]
        public uint addRef()
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, uint>)(lpVtbl[1]))((IByteCodeRunner*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("uint32_t")]
        public uint release()
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, uint>)(lpVtbl[2]))((IByteCodeRunner*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int loadModule([NativeTypeName("slang::IBlob *")] ISlangBlob* moduleBlob)
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, ISlangBlob*, int>)(lpVtbl[3]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), moduleBlob);
        }

        [return: NativeTypeName("SlangResult")]
        public int selectFunctionByIndex([NativeTypeName("uint32_t")] uint functionIndex)
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, uint, int>)(lpVtbl[4]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), functionIndex);
        }

        public int findFunctionByName([NativeTypeName("const char *")] sbyte* name)
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, sbyte*, int>)(lpVtbl[5]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), name);
        }

        [return: NativeTypeName("SlangResult")]
        public int getFunctionInfo([NativeTypeName("uint32_t")] uint index, [NativeTypeName("slang::ByteCodeFuncInfo *")] ByteCodeFuncInfo* outInfo)
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, uint, ByteCodeFuncInfo*, int>)(lpVtbl[6]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), index, outInfo);
        }

        public void* getCurrentWorkingSet()
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, void*>)(lpVtbl[7]))((IByteCodeRunner*)Unsafe.AsPointer(ref this));
        }

        [return: NativeTypeName("SlangResult")]
        public int execute(void* argumentData, [NativeTypeName("size_t")] nuint argumentSize)
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, void*, nuint, int>)(lpVtbl[8]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), argumentData, argumentSize);
        }

        public void getErrorString([NativeTypeName("IBlob **")] ISlangBlob** outBlob)
        {
            ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, ISlangBlob**, void>)(lpVtbl[9]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), outBlob);
        }

        public void* getReturnValue([NativeTypeName("size_t *")] nuint* outValueSize)
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, nuint*, void*>)(lpVtbl[10]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), outValueSize);
        }

        public void setExtInstHandlerUserData(void* userData)
        {
            ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, void*, void>)(lpVtbl[11]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), userData);
        }

        [return: NativeTypeName("SlangResult")]
        public int registerExtCall([NativeTypeName("const char *")] sbyte* name, [NativeTypeName("slang::VMExtFunction")] delegate* unmanaged[Cdecl]<IByteCodeRunner*, VMExecInstHeader*, void*, void> functionPtr)
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, sbyte*, delegate* unmanaged[Thiscall]<IByteCodeRunner*, VMExecInstHeader*, void*, void>, int>)(lpVtbl[12]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), name, functionPtr);
        }

        [return: NativeTypeName("SlangResult")]
        public int setPrintCallback([NativeTypeName("slang::VMPrintFunc")] delegate* unmanaged[Cdecl]<sbyte*, void*, void> callback, void* userData)
        {
            return ((delegate* unmanaged[Stdcall]<IByteCodeRunner*, delegate* unmanaged[Thiscall]<sbyte*, void*, void>, void*, int>)(lpVtbl[13]))((IByteCodeRunner*)Unsafe.AsPointer(ref this), callback, userData);
        }
    }

    public static unsafe partial class Methods
    {
        [NativeTypeName("const SlangTargetFlags")]
        public const uint kDefaultTargetFlags = (uint)(SLANG_TARGET_FLAG_GENERATE_SPIRV_DIRECTLY);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spGetBuildTagString();

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangSession *")]
        public static extern IGlobalSession* spCreateSession([NativeTypeName("const char *")] sbyte* deprecated = null);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spDestroySession([NativeTypeName("SlangSession *")] IGlobalSession* session);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSessionSetSharedLibraryLoader([NativeTypeName("SlangSession *")] IGlobalSession* session, ISlangSharedLibraryLoader* loader);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ISlangSharedLibraryLoader* spSessionGetSharedLibraryLoader([NativeTypeName("SlangSession *")] IGlobalSession* session);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spSessionCheckCompileTargetSupport([NativeTypeName("SlangSession *")] IGlobalSession* session, SlangCompileTarget target);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spSessionCheckPassThroughSupport([NativeTypeName("SlangSession *")] IGlobalSession* session, SlangPassThrough passThrough);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spAddBuiltins([NativeTypeName("SlangSession *")] IGlobalSession* session, [NativeTypeName("const char *")] sbyte* sourcePath, [NativeTypeName("const char *")] sbyte* sourceString);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangCompileRequest *")]
        public static extern ICompileRequest* spCreateCompileRequest([NativeTypeName("SlangSession *")] IGlobalSession* session);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spDestroyCompileRequest([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetFileSystem([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, ISlangFileSystem* fileSystem);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetCompileFlags([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("SlangCompileFlags")] uint flags);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangCompileFlags")]
        public static extern uint spGetCompileFlags([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetDumpIntermediates([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int enable);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetDumpIntermediatePrefix([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("const char *")] sbyte* prefix);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetLineDirectiveMode([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangLineDirectiveMode mode);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetTargetLineDirectiveMode([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int targetIndex, SlangLineDirectiveMode mode);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetTargetForceGLSLScalarBufferLayout([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int targetIndex, [NativeTypeName("bool")] byte forceScalarLayout);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetTargetUseMinimumSlangOptimization([NativeTypeName("slang::ICompileRequest *")] ICompileRequest* request, int targetIndex, [NativeTypeName("bool")] byte val);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetIgnoreCapabilityCheck([NativeTypeName("slang::ICompileRequest *")] ICompileRequest* request, [NativeTypeName("bool")] byte val);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetCodeGenTarget([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangCompileTarget target);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int spAddCodeGenTarget([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangCompileTarget target);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetTargetProfile([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int targetIndex, SlangProfileID profile);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetTargetFlags([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int targetIndex, [NativeTypeName("SlangTargetFlags")] uint flags);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetTargetFloatingPointMode([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int targetIndex, SlangFloatingPointMode mode);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spAddTargetCapability([NativeTypeName("slang::ICompileRequest *")] ICompileRequest* request, int targetIndex, SlangCapabilityID capability);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetTargetMatrixLayoutMode([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int targetIndex, SlangMatrixLayoutMode mode);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetMatrixLayoutMode([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangMatrixLayoutMode mode);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetDebugInfoLevel([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangDebugInfoLevel level);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetDebugInfoFormat([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangDebugInfoFormat format);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetOptimizationLevel([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangOptimizationLevel level);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetOutputContainerFormat([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangContainerFormat format);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetPassThrough([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangPassThrough passThrough);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetDiagnosticCallback([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("SlangDiagnosticCallback")] delegate* unmanaged[Cdecl]<sbyte*, void*, void> callback, [NativeTypeName("const void *")] void* userData);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetWriter([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangWriterChannel channel, ISlangWriter* writer);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ISlangWriter* spGetWriter([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangWriterChannel channel);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spAddSearchPath([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("const char *")] sbyte* searchDir);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spAddPreprocessorDefine([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("const char *")] sbyte* key, [NativeTypeName("const char *")] sbyte* value);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spProcessCommandLineArguments([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("const char *const *")] sbyte** args, int argCount);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int spAddTranslationUnit([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, SlangSourceLanguage language, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetDefaultModuleName([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("const char *")] sbyte* defaultModuleName);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spTranslationUnit_addPreprocessorDefine([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int translationUnitIndex, [NativeTypeName("const char *")] sbyte* key, [NativeTypeName("const char *")] sbyte* value);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spAddTranslationUnitSourceFile([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int translationUnitIndex, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spAddTranslationUnitSourceString([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int translationUnitIndex, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("const char *")] sbyte* source);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spAddLibraryReference([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("const char *")] sbyte* basePath, [NativeTypeName("const void *")] void* libData, [NativeTypeName("size_t")] nuint libDataSize);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spAddTranslationUnitSourceStringSpan([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int translationUnitIndex, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("const char *")] sbyte* sourceBegin, [NativeTypeName("const char *")] sbyte* sourceEnd);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spAddTranslationUnitSourceBlob([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int translationUnitIndex, [NativeTypeName("const char *")] sbyte* path, ISlangBlob* sourceBlob);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangProfileID spFindProfile([NativeTypeName("SlangSession *")] IGlobalSession* session, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangCapabilityID spFindCapability([NativeTypeName("SlangSession *")] IGlobalSession* session, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int spAddEntryPoint([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int translationUnitIndex, [NativeTypeName("const char *")] sbyte* name, SlangStage stage);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int spAddEntryPointEx([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int translationUnitIndex, [NativeTypeName("const char *")] sbyte* name, SlangStage stage, int genericArgCount, [NativeTypeName("const char **")] sbyte** genericArgs);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spSetGlobalGenericArgs([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int genericArgCount, [NativeTypeName("const char **")] sbyte** genericArgs);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spSetTypeNameForGlobalExistentialTypeParam([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int slotIndex, [NativeTypeName("const char *")] sbyte* typeName);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spSetTypeNameForEntryPointExistentialTypeParam([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int entryPointIndex, int slotIndex, [NativeTypeName("const char *")] sbyte* typeName);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spCompile([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spGetDiagnosticOutput([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spGetDiagnosticOutputBlob([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, ISlangBlob** outBlob);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int spGetDependencyFileCount([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spGetDependencyFilePath([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int spGetTranslationUnitCount([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spGetEntryPointSource([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int entryPointIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* spGetEntryPointCode([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int entryPointIndex, [NativeTypeName("size_t *")] nuint* outSize);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spGetEntryPointCodeBlob([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int entryPointIndex, int targetIndex, ISlangBlob** outBlob);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spGetEntryPointHostCallable([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int entryPointIndex, int targetIndex, ISlangSharedLibrary** outSharedLibrary);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spGetTargetCodeBlob([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int targetIndex, ISlangBlob** outBlob);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spGetTargetHostCallable([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int targetIndex, ISlangSharedLibrary** outSharedLibrary);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* spGetCompileRequestCode([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("size_t *")] nuint* outSize);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spGetContainerCode([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, ISlangBlob** outBlob);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spLoadRepro([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, ISlangFileSystem* fileSystem, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint size);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spSaveRepro([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, ISlangBlob** outBlob);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spEnableReproCapture([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spGetCompileTimeProfile([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, ISlangProfiler** compileTimeProfile, [NativeTypeName("bool")] byte shouldClear);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spExtractRepro([NativeTypeName("SlangSession *")] IGlobalSession* session, [NativeTypeName("const void *")] void* reproData, [NativeTypeName("size_t")] nuint reproDataSize, ISlangMutableFileSystem* fileSystem);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spLoadReproAsFileSystem([NativeTypeName("SlangSession *")] IGlobalSession* session, [NativeTypeName("const void *")] void* reproData, [NativeTypeName("size_t")] nuint reproDataSize, ISlangFileSystem* replaceFileSystem, ISlangFileSystemExt** outFileSystem);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spOverrideDiagnosticSeverity([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("SlangInt")] long messageID, SlangSeverity overrideSeverity);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangDiagnosticFlags")]
        public static extern int spGetDiagnosticFlags([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spSetDiagnosticFlags([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("SlangDiagnosticFlags")] int flags);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangReflection *")]
        public static extern SlangProgramLayout* spGetReflection([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionUserAttribute_GetName(SlangReflectionUserAttribute* attrib);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionUserAttribute_GetArgumentCount(SlangReflectionUserAttribute* attrib);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionUserAttribute_GetArgumentType(SlangReflectionUserAttribute* attrib, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spReflectionUserAttribute_GetArgumentValueInt(SlangReflectionUserAttribute* attrib, [NativeTypeName("unsigned int")] uint index, int* rs);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spReflectionUserAttribute_GetArgumentValueFloat(SlangReflectionUserAttribute* attrib, [NativeTypeName("unsigned int")] uint index, float* rs);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionUserAttribute_GetArgumentValueString(SlangReflectionUserAttribute* attrib, [NativeTypeName("unsigned int")] uint index, [NativeTypeName("size_t *")] nuint* outSize);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangTypeKind spReflectionType_GetKind(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionType_GetUserAttributeCount(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionUserAttribute* spReflectionType_GetUserAttribute(SlangReflectionType* type, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionUserAttribute* spReflectionType_FindUserAttributeByName(SlangReflectionType* type, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionType_applySpecializations(SlangReflectionType* type, SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionType_GetFieldCount(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariable* spReflectionType_GetFieldByIndex(SlangReflectionType* type, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint spReflectionType_GetElementCount(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint spReflectionType_GetSpecializedElementCount(SlangReflectionType* type, [NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionType_GetElementType(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionType_GetRowCount(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionType_GetColumnCount(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangScalarType spReflectionType_GetScalarType(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangResourceShape spReflectionType_GetResourceShape(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangResourceAccess spReflectionType_GetResourceAccess(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionType_GetResourceResultType(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionType_GetName(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spReflectionType_GetFullName(SlangReflectionType* type, ISlangBlob** outNameBlob);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionGeneric* spReflectionType_GetGenericContainer(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionTypeLayout_GetType(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangTypeKind spReflectionTypeLayout_getKind(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint spReflectionTypeLayout_GetSize(SlangReflectionTypeLayout* type, SlangParameterCategory category);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint spReflectionTypeLayout_GetStride(SlangReflectionTypeLayout* type, SlangParameterCategory category);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int spReflectionTypeLayout_getAlignment(SlangReflectionTypeLayout* type, SlangParameterCategory category);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint spReflectionTypeLayout_GetFieldCount(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflectionTypeLayout_GetFieldByIndex(SlangReflectionTypeLayout* type, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_findFieldIndexByName(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("const char *")] sbyte* nameBegin, [NativeTypeName("const char *")] sbyte* nameEnd);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflectionTypeLayout_GetExplicitCounter(SlangReflectionTypeLayout* typeLayout);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint spReflectionTypeLayout_GetElementStride(SlangReflectionTypeLayout* type, SlangParameterCategory category);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionTypeLayout* spReflectionTypeLayout_GetElementTypeLayout(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflectionTypeLayout_GetElementVarLayout(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflectionTypeLayout_getContainerVarLayout(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangParameterCategory spReflectionTypeLayout_GetParameterCategory(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionTypeLayout_GetCategoryCount(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangParameterCategory spReflectionTypeLayout_GetCategoryByIndex(SlangReflectionTypeLayout* type, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangMatrixLayoutMode spReflectionTypeLayout_GetMatrixLayoutMode(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int spReflectionTypeLayout_getGenericParamIndex(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionTypeLayout* spReflectionTypeLayout_getPendingDataTypeLayout(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflectionTypeLayout_getSpecializedTypePendingDataVarLayout(SlangReflectionTypeLayout* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionType_getSpecializedTypeArgCount(SlangReflectionType* type);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionType_getSpecializedTypeArgType(SlangReflectionType* type, [NativeTypeName("SlangInt")] long index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getBindingRangeCount(SlangReflectionTypeLayout* typeLayout);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangBindingType spReflectionTypeLayout_getBindingRangeType(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_isBindingRangeSpecializable(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getBindingRangeBindingCount(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionTypeLayout* spReflectionTypeLayout_getBindingRangeLeafTypeLayout(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariable* spReflectionTypeLayout_getBindingRangeLeafVariable(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangImageFormat spReflectionTypeLayout_getBindingRangeImageFormat(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getFieldBindingRangeOffset(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long fieldIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getExplicitCounterBindingRangeOffset(SlangReflectionTypeLayout* inTypeLayout);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getBindingRangeDescriptorSetIndex(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getBindingRangeFirstDescriptorRangeIndex(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getBindingRangeDescriptorRangeCount(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getDescriptorSetCount(SlangReflectionTypeLayout* typeLayout);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getDescriptorSetSpaceOffset(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long setIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getDescriptorSetDescriptorRangeCount(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long setIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getDescriptorSetDescriptorRangeIndexOffset(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long setIndex, [NativeTypeName("SlangInt")] long rangeIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getDescriptorSetDescriptorRangeDescriptorCount(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long setIndex, [NativeTypeName("SlangInt")] long rangeIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangBindingType spReflectionTypeLayout_getDescriptorSetDescriptorRangeType(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long setIndex, [NativeTypeName("SlangInt")] long rangeIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangParameterCategory spReflectionTypeLayout_getDescriptorSetDescriptorRangeCategory(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long setIndex, [NativeTypeName("SlangInt")] long rangeIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getSubObjectRangeCount(SlangReflectionTypeLayout* typeLayout);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getSubObjectRangeBindingRangeIndex(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long subObjectRangeIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflectionTypeLayout_getSubObjectRangeSpaceOffset(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long subObjectRangeIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflectionTypeLayout_getSubObjectRangeOffset(SlangReflectionTypeLayout* typeLayout, [NativeTypeName("SlangInt")] long subObjectRangeIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionVariable_GetName(SlangReflectionVariable* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionVariable_GetType(SlangReflectionVariable* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionModifier* spReflectionVariable_FindModifier(SlangReflectionVariable* var, SlangModifierID modifierID);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionVariable_GetUserAttributeCount(SlangReflectionVariable* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionUserAttribute* spReflectionVariable_GetUserAttribute(SlangReflectionVariable* var, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionUserAttribute* spReflectionVariable_FindUserAttributeByName(SlangReflectionVariable* var, [NativeTypeName("SlangSession *")] IGlobalSession* globalSession, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte spReflectionVariable_HasDefaultValue(SlangReflectionVariable* inVar);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spReflectionVariable_GetDefaultValueInt(SlangReflectionVariable* inVar, [NativeTypeName("int64_t *")] long* rs);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spReflectionVariable_GetDefaultValueFloat(SlangReflectionVariable* inVar, float* rs);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionGeneric* spReflectionVariable_GetGenericContainer(SlangReflectionVariable* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariable* spReflectionVariable_applySpecializations(SlangReflectionVariable* var, SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariable* spReflectionVariableLayout_GetVariable(SlangReflectionVariableLayout* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionTypeLayout* spReflectionVariableLayout_GetTypeLayout(SlangReflectionVariableLayout* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint spReflectionVariableLayout_GetOffset(SlangReflectionVariableLayout* var, SlangParameterCategory category);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint spReflectionVariableLayout_GetSpace(SlangReflectionVariableLayout* var, SlangParameterCategory category);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangImageFormat spReflectionVariableLayout_GetImageFormat(SlangReflectionVariableLayout* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionVariableLayout_GetSemanticName(SlangReflectionVariableLayout* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint spReflectionVariableLayout_GetSemanticIndex(SlangReflectionVariableLayout* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionDecl* spReflectionFunction_asDecl(SlangReflectionFunction* func);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionFunction_GetName(SlangReflectionFunction* func);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionModifier* spReflectionFunction_FindModifier(SlangReflectionFunction* var, SlangModifierID modifierID);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionFunction_GetUserAttributeCount(SlangReflectionFunction* func);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionUserAttribute* spReflectionFunction_GetUserAttribute(SlangReflectionFunction* func, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionUserAttribute* spReflectionFunction_FindUserAttributeByName(SlangReflectionFunction* func, [NativeTypeName("SlangSession *")] IGlobalSession* globalSession, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionFunction_GetParameterCount(SlangReflectionFunction* func);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariable* spReflectionFunction_GetParameter(SlangReflectionFunction* func, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionFunction_GetResultType(SlangReflectionFunction* func);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionGeneric* spReflectionFunction_GetGenericContainer(SlangReflectionFunction* func);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionFunction* spReflectionFunction_applySpecializations(SlangReflectionFunction* func, SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionFunction* spReflectionFunction_specializeWithArgTypes(SlangReflectionFunction* func, [NativeTypeName("SlangInt")] long argTypeCount, [NativeTypeName("SlangReflectionType *const *")] SlangReflectionType** argTypes);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte spReflectionFunction_isOverloaded(SlangReflectionFunction* func);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionFunction_getOverloadCount(SlangReflectionFunction* func);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionFunction* spReflectionFunction_getOverload(SlangReflectionFunction* func, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionDecl_getChildrenCount(SlangReflectionDecl* parentDecl);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionDecl* spReflectionDecl_getChild(SlangReflectionDecl* parentDecl, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionDecl_getName(SlangReflectionDecl* decl);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangDeclKind spReflectionDecl_getKind(SlangReflectionDecl* decl);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionFunction* spReflectionDecl_castToFunction(SlangReflectionDecl* decl);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariable* spReflectionDecl_castToVariable(SlangReflectionDecl* decl);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionGeneric* spReflectionDecl_castToGeneric(SlangReflectionDecl* decl);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflection_getTypeFromDecl(SlangReflectionDecl* decl);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionDecl* spReflectionDecl_getParent(SlangReflectionDecl* decl);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionModifier* spReflectionDecl_findModifier(SlangReflectionDecl* decl, SlangModifierID modifierID);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionDecl* spReflectionGeneric_asDecl(SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionGeneric_GetName(SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionGeneric_GetTypeParameterCount(SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariable* spReflectionGeneric_GetTypeParameter(SlangReflectionGeneric* generic, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionGeneric_GetValueParameterCount(SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariable* spReflectionGeneric_GetValueParameter(SlangReflectionGeneric* generic, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionGeneric_GetTypeParameterConstraintCount(SlangReflectionGeneric* generic, SlangReflectionVariable* typeParam);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionGeneric_GetTypeParameterConstraintType(SlangReflectionGeneric* generic, SlangReflectionVariable* typeParam, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangDeclKind spReflectionGeneric_GetInnerKind(SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionDecl* spReflectionGeneric_GetInnerDecl(SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionGeneric* spReflectionGeneric_GetOuterGenericContainer(SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionGeneric_GetConcreteType(SlangReflectionGeneric* generic, SlangReflectionVariable* typeParam);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int64_t")]
        public static extern long spReflectionGeneric_GetConcreteIntVal(SlangReflectionGeneric* generic, SlangReflectionVariable* valueParam);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionGeneric* spReflectionGeneric_applySpecializations(SlangReflectionGeneric* currGeneric, SlangReflectionGeneric* generic);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangStage spReflectionVariableLayout_getStage(SlangReflectionVariableLayout* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflectionVariableLayout_getPendingDataLayout(SlangReflectionVariableLayout* var);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionParameter_GetBindingIndex([NativeTypeName("SlangReflectionParameter *")] SlangReflectionVariableLayout* parameter);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionParameter_GetBindingSpace([NativeTypeName("SlangReflectionParameter *")] SlangReflectionVariableLayout* parameter);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spIsParameterLocationUsed([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("SlangInt")] long targetIndex, SlangParameterCategory category, [NativeTypeName("SlangUInt")] ulong spaceIndex, [NativeTypeName("SlangUInt")] ulong registerIndex, [NativeTypeName("bool &")] bool* outUsed);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionEntryPoint_getName([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionEntryPoint_getNameOverride([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionFunction* spReflectionEntryPoint_getFunction([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionEntryPoint_getParameterCount([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflectionEntryPoint_getParameterByIndex([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangStage spReflectionEntryPoint_getStage([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spReflectionEntryPoint_getComputeThreadGroupSize([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint, [NativeTypeName("SlangUInt")] ulong axisCount, [NativeTypeName("SlangUInt *")] ulong* outSizeAlongAxis);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void spReflectionEntryPoint_getComputeWaveSize([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint, [NativeTypeName("SlangUInt *")] ulong* outWaveSize);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int spReflectionEntryPoint_usesAnySampleRateInput([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflectionEntryPoint_getVarLayout([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflectionEntryPoint_getResultVarLayout([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int spReflectionEntryPoint_hasDefaultConstantBuffer([NativeTypeName("SlangReflectionEntryPoint *")] SlangEntryPointLayout* entryPoint);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflectionTypeParameter_GetName(SlangReflectionTypeParameter* typeParam);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionTypeParameter_GetIndex(SlangReflectionTypeParameter* typeParam);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflectionTypeParameter_GetConstraintCount(SlangReflectionTypeParameter* typeParam);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflectionTypeParameter_GetConstraintByIndex(SlangReflectionTypeParameter* typeParam, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spReflection_ToJson([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, [NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, ISlangBlob** outBlob);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflection_GetParameterCount([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangReflectionParameter *")]
        public static extern SlangReflectionVariableLayout* spReflection_GetParameterByIndex([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint spReflection_GetTypeParameterCount([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionTypeParameter* spReflection_GetTypeParameterByIndex([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, [NativeTypeName("unsigned int")] uint index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionTypeParameter* spReflection_FindTypeParameter([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflection_FindTypeByName([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionTypeLayout* spReflection_GetTypeLayout([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, SlangReflectionType* reflectionType, SlangLayoutRules rules);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionFunction* spReflection_FindFunctionByName([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionFunction* spReflection_FindFunctionByNameInType([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, SlangReflectionType* reflType, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariable* spReflection_FindVarByNameInType([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, SlangReflectionType* reflType, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionFunction* spReflection_TryResolveOverloadedFunction([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, [NativeTypeName("uint32_t")] uint candidateCount, SlangReflectionFunction** candidates);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangUInt")]
        public static extern ulong spReflection_getEntryPointCount([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangReflectionEntryPoint *")]
        public static extern SlangEntryPointLayout* spReflection_getEntryPointByIndex([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, [NativeTypeName("SlangUInt")] ulong index);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangReflectionEntryPoint *")]
        public static extern SlangEntryPointLayout* spReflection_findEntryPointByName([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangUInt")]
        public static extern ulong spReflection_getGlobalConstantBufferBinding([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint spReflection_getGlobalConstantBufferSize([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionType* spReflection_specializeType([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, SlangReflectionType* type, [NativeTypeName("SlangInt")] long specializationArgCount, [NativeTypeName("SlangReflectionType *const *")] SlangReflectionType** specializationArgs, ISlangBlob** outDiagnostics);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionGeneric* spReflection_specializeGeneric([NativeTypeName("SlangReflection *")] SlangProgramLayout* inProgramLayout, SlangReflectionGeneric* generic, [NativeTypeName("SlangInt")] long argCount, [NativeTypeName("const SlangReflectionGenericArgType *")] SlangReflectionGenericArgType* argTypes, [NativeTypeName("const SlangReflectionGenericArg *")] SlangReflectionGenericArg* args, ISlangBlob** outDiagnostics);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte spReflection_isSubType([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, SlangReflectionType* subType, SlangReflectionType* superType);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangUInt")]
        public static extern ulong spReflection_getHashedStringCount([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spReflection_getHashedString([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection, [NativeTypeName("SlangUInt")] ulong index, [NativeTypeName("size_t *")] nuint* outCount);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangUInt32")]
        public static extern uint spComputeStringHash([NativeTypeName("const char *")] sbyte* chars, [NativeTypeName("size_t")] nuint count);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionTypeLayout* spReflection_getGlobalParamsTypeLayout([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern SlangReflectionVariableLayout* spReflection_getGlobalParamsVarLayout([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* spGetTranslationUnitSource([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, int translationUnitIndex);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangInt")]
        public static extern long spReflection_getBindlessSpaceIndex([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?spReflection_GetSession@@YAPEAUISession@slang@@PEAUSlangProgramLayout@@@Z", ExactSpelling = true)]
        [return: NativeTypeName("slang::ISession *")]
        public static extern ISession* spReflection_GetSession([NativeTypeName("SlangReflection *")] SlangProgramLayout* reflection);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spCompileRequest_getProgram([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("slang::IComponentType **")] IComponentType** outProgram);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spCompileRequest_getProgramWithEntryPoints([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("slang::IComponentType **")] IComponentType** outProgram);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spCompileRequest_getEntryPoint([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("SlangInt")] long entryPointIndex, [NativeTypeName("slang::IComponentType **")] IComponentType** outEntryPoint);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spCompileRequest_getModule([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("SlangInt")] long translationUnitIndex, [NativeTypeName("slang::IModule **")] IModule** outModule);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int spCompileRequest_getSession([NativeTypeName("SlangCompileRequest *")] ICompileRequest* request, [NativeTypeName("slang::ISession **")] ISession** outSession);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern ISlangBlob* slang_createBlob([NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint size);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("slang::IModule *")]
        public static extern IModule* slang_loadModuleFromSource([NativeTypeName("slang::ISession *")] ISession* session, [NativeTypeName("const char *")] sbyte* moduleName, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("const char *")] sbyte* source, [NativeTypeName("size_t")] nuint sourceSize, ISlangBlob** outDiagnostics = null);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("slang::IModule *")]
        public static extern IModule* slang_loadModuleFromIRBlob([NativeTypeName("slang::ISession *")] ISession* session, [NativeTypeName("const char *")] sbyte* moduleName, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("const void *")] void* source, [NativeTypeName("size_t")] nuint sourceSize, ISlangBlob** outDiagnostics = null);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int slang_loadModuleInfoFromIRBlob([NativeTypeName("slang::ISession *")] ISession* session, [NativeTypeName("const void *")] void* source, [NativeTypeName("size_t")] nuint sourceSize, [NativeTypeName("SlangInt &")] long* outModuleVersion, [NativeTypeName("const char *&")] sbyte** outModuleCompilerVersion, [NativeTypeName("const char *&")] sbyte** outModuleName);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int slang_createGlobalSession([NativeTypeName("SlangInt")] long apiVersion, [NativeTypeName("slang::IGlobalSession **")] IGlobalSession** outGlobalSession);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int slang_createGlobalSession2([NativeTypeName("const SlangGlobalSessionDesc *")] SlangGlobalSessionDesc* desc, [NativeTypeName("slang::IGlobalSession **")] IGlobalSession** outGlobalSession);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int slang_createGlobalSessionWithoutCoreModule([NativeTypeName("SlangInt")] long apiVersion, [NativeTypeName("slang::IGlobalSession **")] IGlobalSession** outGlobalSession);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?slang_getEmbeddedCoreModule@@YAPEAUISlangBlob@@XZ", ExactSpelling = true)]
        public static extern ISlangBlob* slang_getEmbeddedCoreModule();

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void slang_shutdown();

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* slang_getLastInternalErrorMessage();

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int slang_createByteCodeRunner([NativeTypeName("const slang::ByteCodeRunnerDesc *")] ByteCodeRunnerDesc* desc, [NativeTypeName("slang::IByteCodeRunner **")] IByteCodeRunner** outByteCodeRunner);

        [DllImport("", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("SlangResult")]
        public static extern int slang_disassembleByteCode([NativeTypeName("slang::IBlob *")] ISlangBlob* moduleBlob, [NativeTypeName("slang::IBlob **")] ISlangBlob** outDisassemblyBlob);

        [return: NativeTypeName("SlangResult")]
        public static int createGlobalSession([NativeTypeName("slang::IGlobalSession **")] IGlobalSession** outGlobalSession)
        {
            SlangGlobalSessionDesc defaultDesc = new SlangGlobalSessionDesc
            {
                structureSize = ,
                apiVersion = ,
                minLanguageVersion = ,
                enableGLSL = ,
                reserved = ,
            };

            return slang_createGlobalSession2(&defaultDesc, outGlobalSession);
        }

        [return: NativeTypeName("SlangResult")]
        public static int createGlobalSession([NativeTypeName("const SlangGlobalSessionDesc *")] SlangGlobalSessionDesc* desc, [NativeTypeName("slang::IGlobalSession **")] IGlobalSession** outGlobalSession)
        {
            return slang_createGlobalSession2(desc, outGlobalSession);
        }

        public static void shutdown()
        {
            slang_shutdown();
        }

        [return: NativeTypeName("const char *")]
        public static sbyte* getLastInternalErrorMessage()
        {
            return slang_getLastInternalErrorMessage();
        }
    }
}
