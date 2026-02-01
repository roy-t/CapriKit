# CapriKit.Tests

_[Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro?tabs=dotnetcli) style test project based on [TUnit](https://tunit.dev). Contains unit tests for all CapriKit libraries._

You can run this project (it is a console application) directly or via `dotnet test` to generate the test report in `${SolutionDir}\.build\tst`.

## Guidelines

When in doubt, follow [Microsoft's Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices#best-practices).

Since CapriKit is a hobby project, the goal is not to write extensive tests that prove the code is production ready. Instead we want to have reasonable assurance that the happy path functionality works. In practice this means that tests should only cover functionality that has at least some complexity and should focus on the hapy path.

Test should follow the arrange, act, assert pattern. Assertions should follow `Assert.That(result).IsEqualTo(expectation)` for correct error messages.

## Naming
- Put tests for file `Foo.cs` in namespace `CapriKit.Bar` in `/Bar/FooTests.cs`.
- Put the most important test for method `Method` in a method called `Method`
- Put tests for specific corner cases in `Method_CornerCase`
