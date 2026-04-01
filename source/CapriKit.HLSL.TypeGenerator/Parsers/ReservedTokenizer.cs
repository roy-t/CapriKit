namespace CapriKit.HLSL.TypeGenerator.Parsers;

public static class ReservedTokenizer
{
    /// <summary>
    /// All HLSL keywords minus the numeric types that support expansions (like float, which has float4x4)
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-keywords"/>
    private static readonly Dictionary<string, TokenKind> Keywords = new()
    {
        ["AppendStructuredBuffer"] = TokenKind.AppendStructuredBuffer,
        ["asm"] = TokenKind.Asm,
        ["asm_fragment"] = TokenKind.AsmFragment,

        ["BlendState"] = TokenKind.BlendState,
        ["bool"] = TokenKind.Bool,
        ["break"] = TokenKind.Break,
        ["Buffer"] = TokenKind.Buffer,
        ["ByteAddressBuffer"] = TokenKind.ByteAddressBuffer,

        ["case"] = TokenKind.Case,
        ["cbuffer"] = TokenKind.CBuffer,
        ["centroid"] = TokenKind.Centroid,
        ["class"] = TokenKind.Class,
        ["column_major"] = TokenKind.ColumnMajor,
        ["compile"] = TokenKind.Compile,
        ["compile_fragment"] = TokenKind.CompileFragment,
        ["CompileShader"] = TokenKind.CompileShader,
        ["const"] = TokenKind.Const,
        ["continue"] = TokenKind.Continue,
        ["ComputeShader"] = TokenKind.ComputeShader,
        ["ConsumeStructuredBuffer"] = TokenKind.ConsumeStructuredBuffer,

        ["default"] = TokenKind.Default,
        ["DepthStencilState"] = TokenKind.DepthStencilState,
        ["DepthStencilView"] = TokenKind.DepthStencilView,
        ["discard"] = TokenKind.Discard,
        ["do"] = TokenKind.Do,
        ["double"] = TokenKind.Double,
        ["DomainShader"] = TokenKind.DomainShader,
        ["dword"] = TokenKind.Dword,

        ["else"] = TokenKind.Else,
        ["export"] = TokenKind.Export,
        ["extern"] = TokenKind.Extern,

        ["false"] = TokenKind.False,
        ["float"] = TokenKind.Float,
        ["for"] = TokenKind.For,
        ["fxgroup"] = TokenKind.FxGroup,

        ["GeometryShader"] = TokenKind.GeometryShader,
        ["groupshared"] = TokenKind.GroupShared,

        ["half"] = TokenKind.Half,
        ["Hullshader"] = TokenKind.HullShader,

        ["if"] = TokenKind.If,
        ["in"] = TokenKind.In,
        ["inline"] = TokenKind.Inline,
        ["inout"] = TokenKind.InOut,
        ["InputPatch"] = TokenKind.InputPatch,
        ["int"] = TokenKind.Int,
        ["interface"] = TokenKind.Interface,

        ["line"] = TokenKind.Line,
        ["lineadj"] = TokenKind.LineAdj,
        ["linear"] = TokenKind.Linear,
        ["LineStream"] = TokenKind.LineStream,

        ["matrix"] = TokenKind.Matrix,
        ["min16float"] = TokenKind.Min16Float,
        ["min10float"] = TokenKind.Min10Float,
        ["min16int"] = TokenKind.Min16Int,
        ["min12int"] = TokenKind.Min12Int,
        ["min16uint"] = TokenKind.Min16UInt,

        ["namespace"] = TokenKind.Namespace,
        ["nointerpolation"] = TokenKind.NoInterpolation,
        ["noperspective"] = TokenKind.NoPerspective,
        ["NULL"] = TokenKind.Null,

        ["out"] = TokenKind.Out,
        ["OutputPatch"] = TokenKind.OutputPatch,

        ["packoffset"] = TokenKind.PackOffset,
        ["pass"] = TokenKind.Pass,
        ["pixelfragment"] = TokenKind.PixelFragment,
        ["PixelShader"] = TokenKind.PixelShader,
        ["point"] = TokenKind.Point,
        ["PointStream"] = TokenKind.PointStream,
        ["precise"] = TokenKind.Precise,

        ["RasterizerState"] = TokenKind.RasterizerState,
        ["RenderTargetView"] = TokenKind.RenderTargetView,
        ["return"] = TokenKind.Return,
        ["register"] = TokenKind.Register,
        ["row_major"] = TokenKind.RowMajor,
        ["RWBuffer"] = TokenKind.RWBuffer,
        ["RWByteAddressBuffer"] = TokenKind.RWByteAddressBuffer,
        ["RWStructuredBuffer"] = TokenKind.RWStructuredBuffer,
        ["RWTexture1D"] = TokenKind.RWTexture1D,
        ["RWTexture1DArray"] = TokenKind.RWTexture1DArray,
        ["RWTexture2D"] = TokenKind.RWTexture2D,
        ["RWTexture2DArray"] = TokenKind.RWTexture2DArray,
        ["RWTexture3D"] = TokenKind.RWTexture3D,

        ["sample"] = TokenKind.Sample,
        ["sampler"] = TokenKind.Sampler,
        ["SamplerState"] = TokenKind.SamplerState,
        ["SamplerComparisonState"] = TokenKind.SamplerComparisonState,
        ["shared"] = TokenKind.Shared,
        ["snorm"] = TokenKind.SNorm,
        ["stateblock"] = TokenKind.StateBlock,
        ["stateblock_state"] = TokenKind.StateBlockState,
        ["static"] = TokenKind.Static,
        ["string"] = TokenKind.String,
        ["struct"] = TokenKind.Struct,
        ["switch"] = TokenKind.Switch,
        ["StructuredBuffer"] = TokenKind.StructuredBuffer,

        ["tbuffer"] = TokenKind.TBuffer,
        ["technique"] = TokenKind.Technique,
        ["technique10"] = TokenKind.Technique10,
        ["technique11"] = TokenKind.Technique11,
        ["texture"] = TokenKind.Texture,
        ["Texture1D"] = TokenKind.Texture1D,
        ["Texture1DArray"] = TokenKind.Texture1DArray,
        ["Texture2D"] = TokenKind.Texture2D,
        ["Texture2DArray"] = TokenKind.Texture2DArray,
        ["Texture2DMS"] = TokenKind.Texture2DMS,
        ["Texture2DMSArray"] = TokenKind.Texture2DMSArray,
        ["Texture3D"] = TokenKind.Texture3D,
        ["TextureCube"] = TokenKind.TextureCube,
        ["TextureCubeArray"] = TokenKind.TextureCubeArray,
        ["true"] = TokenKind.True,
        ["typedef"] = TokenKind.TypeDef,
        ["triangle"] = TokenKind.Triangle,
        ["triangleadj"] = TokenKind.TriangleAdj,
        ["TriangleStream"] = TokenKind.TriangleStream,

        ["uint"] = TokenKind.UInt,
        ["uniform"] = TokenKind.Uniform,
        ["unorm"] = TokenKind.UNorm,
        ["unsigned"] = TokenKind.Unsigned,

        ["vector"] = TokenKind.Vector,
        ["vertexfragment"] = TokenKind.VertexFragment,
        ["VertexShader"] = TokenKind.VertexShader,
        ["void"] = TokenKind.Void,
        ["volatile"] = TokenKind.Volatile,

        ["while"] = TokenKind.While
    };

    /// <summary>
    /// Numeric types that support scalar, vector and matrix expansion, for example float which can expand to float3x1
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-preprocessor"/>
    private static readonly HashSet<TokenKind> SupportsExpansion = new()
    {
        TokenKind.Float,
        TokenKind.Int,
        TokenKind.UInt,
        TokenKind.Bool,
        TokenKind.Min16Float,
        TokenKind.Min10Float,
        TokenKind.Min16Int,
        TokenKind.Min12Int,
        TokenKind.Min16UInt,
    };

    private static readonly IReadOnlyList<string> Expansions =
    [
        "1", "2", "3", "4",
        "1x1", "1x2", "1x3", "1x4",
        "2x1", "2x2", "2x3", "2x4",
        "3x1", "3x2", "3x3", "3x4",
        "4x1", "4x2", "4x3", "4x4",
    ];

    private static readonly Dictionary<char, TokenKind> Punctuation = new()
    {
        ['{'] = TokenKind.LeftCurlyBracket,
        ['}'] = TokenKind.RightCurlyBracket,
        ['['] = TokenKind.LeftSquareBracket,
        [']'] = TokenKind.RightSquareBracket,
        ['<'] = TokenKind.LeftAngleBracket,
        ['>'] = TokenKind.RightAngleBracket,
        ['('] = TokenKind.LeftParenthesis,
        [')'] = TokenKind.RightParenthesis,

        [','] = TokenKind.Comma,
        [':'] = TokenKind.Colon,
        [';'] = TokenKind.SemiColon,
    };

    private static readonly Dictionary<char, TokenKind> Operators = new()
    {
        ['='] = TokenKind.Equals,
        ['+'] = TokenKind.Plus,
        ['-'] = TokenKind.Minus,
        ['*'] = TokenKind.Multiply,
        ['/'] = TokenKind.Divide,
    };

    private static readonly Dictionary<string, TokenKind> MultiCharacterOperators = new()
    {
        ["##"] = TokenKind.TokenPaste,
        ["#@"] = TokenKind.StringizeWithAt,
        ["++"] = TokenKind.Increment,
        ["--"] = TokenKind.Decrement,
        ["&&"] = TokenKind.LogicalAnd,
        ["||"] = TokenKind.LogicalOr,
        ["=="] = TokenKind.Equals,
        ["!="] = TokenKind.NotEquals,
        ["<="] = TokenKind.LessThanEquals,
        [">="] = TokenKind.GreaterThanEquals,
        ["<<"] = TokenKind.LeftShift,
        ["<<="] = TokenKind.LeftShiftAssign,
        [">>"] = TokenKind.RightShift,
        [">>="] = TokenKind.RightShiftAssign,
        ["+="] = TokenKind.AddAssign,
        ["-="] = TokenKind.SubtractAssign,
        ["*="] = TokenKind.MultiplyAssign,
        ["/="] = TokenKind.DivideAssign,
        ["%="] = TokenKind.ModulusAssign,
        ["&="] = TokenKind.AndAssign,
        ["|="] = TokenKind.OrAssign,
        ["^="] = TokenKind.XorAssign,
        ["::"] = TokenKind.ScopeResolution,
        ["->"] = TokenKind.Arrow,
        ["..."] = TokenKind.Ellipsis
    };

    public static int ReadKeyword(string source, int offset, List<Token> tokens)
    {
        var result = ReadString(source, offset, tokens, Keywords);
        if (result > 0 && SupportsExpansion.Contains(tokens.Last().Kind))
        {
            var span = source.AsSpan(offset + result);
            foreach (var expansion in Expansions)
            {
                if (span.StartsWith(expansion, StringComparison.Ordinal))
                {
                    tokens.Add(new Token(source, offset + result, expansion.Length, TokenKind.NumericExpansion));
                    result += expansion.Length;
                }
            }
        }

        return result;
    }

    public static int ReadPunctuation(string source, int offset, List<Token> tokens)
    {
        return ReadCharacter(source, offset, tokens, Punctuation);
    }

    public static int ReadOperators(string source, int offset, List<Token> tokens)
    {
        var read = ReadString(source, offset, tokens, MultiCharacterOperators);
        if (read > 0)
        {
            return read;
        }

        return ReadCharacter(source, offset, tokens, Operators);
    }

    private static int ReadString(string source, int offset, List<Token> tokens, Dictionary<string, TokenKind> lookUp)
    {
        var span = source.AsSpan(offset);
        foreach (var kv in lookUp)
        {
            var key = kv.Key;
            if (span.StartsWith(key, StringComparison.Ordinal))
            {
                tokens.Add(new Token(source, offset, key.Length, kv.Value));
                return key.Length;
            }
        }

        return 0;
    }

    private static int ReadCharacter(string source, int offset, List<Token> tokens, Dictionary<char, TokenKind> lookUp)
    {
        if (offset >= source.Length)
        {
            return 0;
        }

        var c = source[offset];
        if (lookUp.TryGetValue(c, out var kind))
        {
            tokens.Add(new Token(source, offset, 1, kind));
            return 1;
        }

        return 0;
    }
}
