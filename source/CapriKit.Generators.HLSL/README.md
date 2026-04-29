# CapriKit.Generators.HLSL

A source generator that analyzes HLSL shader files and generates strongly typed bindings for interacting with them. It includes a tokenizer that tokenizes HLSL files according to the grammar defined by [MSDN](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix).

## Implementation Details & Quircks
- HLSL defines every unmatched single character as an operator. For example `-4.0f` matches as `<operator><floating-point-number>`
