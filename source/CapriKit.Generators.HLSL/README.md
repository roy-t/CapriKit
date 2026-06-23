# CapriKit.Generators.HLSL

A source generator that analyzes HLSL shader files and generates structs, input element descriptions and information on entrypoints to make it easier to use HLSL shaders from C#. See the official HLSL documentation on [MSDN](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix) for information on the grammar and the [HLSL Specification Working Draft](https://microsoft.github.io/hlsl-specs/specs/1-Intro.html)

## Implementation Details & Quirks

- HLSL defines every unmatched single character as an operator. For example `-4.0f` matches as `<operator><floating-point-number>`. The tokenizer follows this rule, but it means that you need to be mindful in parsers when trying to match number literals.
- The parser recognizes function with specific preceding pragmas as entry points and marks them so that the generator can generate properties and documenatation for them.
  - `#pragma VertexShader`
  - `#pragma PixelShader`
  - `#pragma ComputeShader`
- Similarly, structs preceded by `#pragma Input` are recognized and marked as vertex shader inputs and the generator generates input element descriptions for them.
- Since structs need explict layouts to follow all HLSL layout rules and to be able to generate the correct offsets in input element descriptions the generator has to parse all files and will regenerate everything if only one file changes.
