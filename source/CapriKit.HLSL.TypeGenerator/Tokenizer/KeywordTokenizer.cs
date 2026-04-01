using CapriKit.HLSL.TypeGenerator.Parsers;

namespace CapriKit.HLSL.TypeGenerator.Tokenizer;

public static class KeywordTokenizer
{
    private static readonly Dictionary<string, TokenKind> Keywords = new()
    {
        ["AppendStructuredBuffer"] = TokenKind.Keyword,
        ["asm"] = TokenKind.Keyword,
        ["asm_fragment"] = TokenKind.Keyword,
        ["BlendState"] = TokenKind.Keyword,
        ["bool"] = TokenKind.Keyword,
        ["break"] = TokenKind.Keyword,
        ["Buffer"] = TokenKind.Keyword,
        ["ByteAddressBuffer"] = TokenKind.Keyword,
        ["case"] = TokenKind.Keyword,
        ["cbuffer"] = TokenKind.Keyword,
        ["centroid"] = TokenKind.Keyword,
        ["class"] = TokenKind.Keyword,
        ["column_major"] = TokenKind.Keyword,
        ["compile"] = TokenKind.Keyword,
        ["compile_fragment"] = TokenKind.Keyword,
        ["CompileShader"] = TokenKind.Keyword,
        ["const"] = TokenKind.Keyword,
        ["continue"] = TokenKind.Keyword,
        ["ComputeShader"] = TokenKind.Keyword,
        ["ConsumeStructuredBuffer"] = TokenKind.Keyword,
        ["default"] = TokenKind.Keyword,
        ["DepthStencilState"] = TokenKind.Keyword,
        ["DepthStencilView"] = TokenKind.Keyword,
        ["discard"] = TokenKind.Keyword,
        ["do"] = TokenKind.Keyword,
        ["double"] = TokenKind.Keyword,
        ["DomainShader"] = TokenKind.Keyword,
        ["dword"] = TokenKind.Keyword,
        ["else"] = TokenKind.Keyword,
        ["export"] = TokenKind.Keyword,
        ["extern"] = TokenKind.Keyword,
        ["false"] = TokenKind.Keyword,
        ["float"] = TokenKind.Keyword,
        ["for"] = TokenKind.Keyword,
        ["fxgroup"] = TokenKind.Keyword,
        ["GeometryShader"] = TokenKind.Keyword,
        ["groupshared"] = TokenKind.Keyword,
        ["half"] = TokenKind.Keyword,
        ["Hullshader"] = TokenKind.Keyword,
        ["if"] = TokenKind.Keyword,
        ["in"] = TokenKind.Keyword,
        ["inline"] = TokenKind.Keyword,
        ["inout"] = TokenKind.Keyword,
        ["InputPatch"] = TokenKind.Keyword,
        ["int"] = TokenKind.Keyword,
        ["interface"] = TokenKind.Keyword,
        ["line"] = TokenKind.Keyword,
        ["lineadj"] = TokenKind.Keyword,
        ["linear"] = TokenKind.Keyword,
        ["LineStream"] = TokenKind.Keyword,
        ["matrix"] = TokenKind.Keyword,
        ["min16float"] = TokenKind.Keyword,
        ["min10float"] = TokenKind.Keyword,
        ["min16int"] = TokenKind.Keyword,
        ["min12int"] = TokenKind.Keyword,
        ["min16uint"] = TokenKind.Keyword,
        ["namespace"] = TokenKind.Keyword,
        ["nointerpolation"] = TokenKind.Keyword,
        ["noperspective"] = TokenKind.Keyword,
        ["NULL"] = TokenKind.Keyword,
        ["out"] = TokenKind.Keyword,
        ["OutputPatch"] = TokenKind.Keyword,
        ["packoffset"] = TokenKind.Keyword,
        ["pass"] = TokenKind.Keyword,
        ["pixelfragment"] = TokenKind.Keyword,
        ["PixelShader"] = TokenKind.Keyword,
        ["point"] = TokenKind.Keyword,
        ["PointStream"] = TokenKind.Keyword,
        ["precise"] = TokenKind.Keyword,
        ["RasterizerState"] = TokenKind.Keyword,
        ["RenderTargetView"] = TokenKind.Keyword,
        ["return"] = TokenKind.Keyword,
        ["register"] = TokenKind.Keyword,
        ["row_major"] = TokenKind.Keyword,
        ["RWBuffer"] = TokenKind.Keyword,
        ["RWByteAddressBuffer"] = TokenKind.Keyword,
        ["RWStructuredBuffer"] = TokenKind.Keyword,
        ["RWTexture1D"] = TokenKind.Keyword,
        ["RWTexture1DArray"] = TokenKind.Keyword,
        ["RWTexture2D"] = TokenKind.Keyword,
        ["RWTexture2DArray"] = TokenKind.Keyword,
        ["RWTexture3D"] = TokenKind.Keyword,
        ["sample"] = TokenKind.Keyword,
        ["sampler"] = TokenKind.Keyword,
        ["SamplerState"] = TokenKind.Keyword,
        ["SamplerComparisonState"] = TokenKind.Keyword,
        ["shared"] = TokenKind.Keyword,
        ["snorm"] = TokenKind.Keyword,
        ["stateblock"] = TokenKind.Keyword,
        ["stateblock_state"] = TokenKind.Keyword,
        ["static"] = TokenKind.Keyword,
        ["string"] = TokenKind.Keyword,
        ["struct"] = TokenKind.Keyword,
        ["switch"] = TokenKind.Keyword,
        ["StructuredBuffer"] = TokenKind.Keyword,
        ["tbuffer"] = TokenKind.Keyword,
        ["technique"] = TokenKind.Keyword,
        ["technique10"] = TokenKind.Keyword,
        ["technique11"] = TokenKind.Keyword,
        ["texture"] = TokenKind.Keyword,
        ["Texture1D"] = TokenKind.Keyword,
        ["Texture1DArray"] = TokenKind.Keyword,
        ["Texture2D"] = TokenKind.Keyword,
        ["Texture2DArray"] = TokenKind.Keyword,
        ["Texture2DMS"] = TokenKind.Keyword,
        ["Texture2DMSArray"] = TokenKind.Keyword,
        ["Texture3D"] = TokenKind.Keyword,
        ["TextureCube"] = TokenKind.Keyword,
        ["TextureCubeArray"] = TokenKind.Keyword,
        ["true"] = TokenKind.Keyword,
        ["typedef"] = TokenKind.Keyword,
        ["triangle"] = TokenKind.Keyword,
        ["triangleadj"] = TokenKind.Keyword,
        ["TriangleStream"] = TokenKind.Keyword,
        ["uint"] = TokenKind.Keyword,
        ["uniform"] = TokenKind.Keyword,
        ["unorm"] = TokenKind.Keyword,
        ["unsigned"] = TokenKind.Keyword,
        ["vector"] = TokenKind.Keyword,
        ["vertexfragment"] = TokenKind.Keyword,
        ["VertexShader"] = TokenKind.Keyword,
        ["void"] = TokenKind.Keyword,
        ["volatile"] = TokenKind.Keyword,
        ["while"] = TokenKind.Keyword,
    };

    /// <summary>
    /// Numeric types that support scalar, vector and matrix expansion.
    /// For example not only float is a keyword, but so is float2 and float3x2
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-keywords#remarks"/>
    private static readonly HashSet<string> SupportsExpansion =
    [
        "float",
        "int",
        "uint",
        "bool",
        "min16float",
        "min10float",
        "min16int",
        "min12int",
        "min16uint",
    ];

    private static readonly IReadOnlyList<string> Expansions =
    [
        "1", "2", "3", "4",
        "1x1", "1x2", "1x3", "1x4",
        "2x1", "2x2", "2x3", "2x4",
        "3x1", "3x2", "3x3", "3x4",
        "4x1", "4x2", "4x3", "4x4",
    ];

    /// <summary>
    /// Adds rules for keywords to the trie.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-keywords"/>
    public static void AddRulesToTrie(Trie trie)
    {
        foreach (var kv in Keywords)
        {
            trie.AddString(kv.Key, kv.Value);
        }

        var expansionTrie = new Trie();
        foreach (var expansion in Expansions)
        {
            expansionTrie.AddString(expansion, TokenKind.Keyword);
        }

        foreach (var expandable in SupportsExpansion)
        {
            trie.AddSubTrie(expandable, expansionTrie);
        }
    }
}
