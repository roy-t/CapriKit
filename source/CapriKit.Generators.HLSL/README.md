# CapriKit.Generators.HLSL

A source generator that analyzes HLSL shader files and generates strongly typed bindings for interacting with them. See the official HLSL documentation on [MSDN](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix) for information on the grammar.

## Implementation Details & Quirks

- HLSL defines every unmatched single character as an operator. For example `-4.0f` matches as `<operator><floating-point-number>`. The tokenizer follows this rule, but it means that you need to be mindful in parsers when trying to match number literals.
- The parser assumes that methods preceded by one of the following pragma statements are entrypoints
  - `#pragma VertexShader`
  - `#pragma PixelShader`
  - `#pragma ComputeShader`
