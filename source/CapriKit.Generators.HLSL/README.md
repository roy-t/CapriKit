# CapriKit.Generators.HLSL

A source generator that analyzes HLSL shader files and generates strongly typed bindings for interacting with them. See the official HLSL documentation on [MSDN](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix) for information on the grammar and the [HLSL Specification Working Draft](https://microsoft.github.io/hlsl-specs/specs/1-Intro.html)


## Implementation Details & Quirks

- HLSL defines every unmatched single character as an operator. For example `-4.0f` matches as `<operator><floating-point-number>`. The tokenizer follows this rule, but it means that you need to be mindful in parsers when trying to match number literals.
- The parser recognizes function with specific preceding pragmas as entry points and marks them with specific kinds so that the generator can generate extra code for them.
  - `#pragma VertexShader`
  - `#pragma PixelShader`
  - `#pragma ComputeShader`
- Similarly, structs preceded by `#pragma Input` are recognized and marked as vertex shader inputs and the generator generates input element descriptions for them.
