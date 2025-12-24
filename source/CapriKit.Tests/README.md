# CapriKit.Tests

_[Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro?tabs=dotnetcli) style test project based on [TUnit](https://tunit.dev). Contains unit tests for all CapriKit libraries._

You can run this project (it is a console application) directly or via `dotnet test` to generate the test report in `${SolutionDir}\.build\tst`.

## Guidelines

When in doubt, follow [Microsoft's Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices#best-practices).

Since CapriKit is a hobby project, the goal is not to write extensive tests that prove the code is production ready. Instead we want to have reasonable assurance that the happy path functionality works. In practice this means that tests should only cover functionality that has at least some complexity and should focus on the hapy path.

Test names should consist of three parts:

- Name of the method being tested
- Scenario under which the method is being tested
- Expected behavior when the scenario is involved

For example a good test name for a method testing `Math.Floor` would be: `Floor_NegativeNumber_RoundsDown()`


Test should follow the arrange, act, assert pattern.
