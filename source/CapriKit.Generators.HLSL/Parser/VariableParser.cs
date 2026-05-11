namespace CapriKit.Generators.HLSL.Parser;

internal class VariableParser
{
    //[Storage_Class] [Type_Modifier] Type Name[Index] [: Semantic] [: Packoffset] [: Register]; [Annotations] [= Initial_Value]

    /// <summary>
    /// Parses a HLSL variable (field)
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-variable-syntax"/>
    public static Variable Parse(ParseState state)
    {
        // TODO: since variables are so complex we should maybe try to write a sort of matcher that only advances if
        // we can parse a whole thing?

        throw new NotImplementedException();
    }
}
