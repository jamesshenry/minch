This `agents.md` is optimized for Large Language Models (LLMs) and AI Agents. It focuses on syntax patterns, configuration requirements, and the unique architectural rules of TUnit.

---

# TUnit: Agent Development Guide

TUnit is a modern, source-generated C# testing framework built on the `Microsoft.Testing.Platform`. It supports Native AOT, Single File applications, and is parallel-by-default.

## 1. Project Configuration (Required)

Agents must ensure the `.csproj` follows these rules to enable source generation and discovery:

- **OutputType**: Must be `Exe`.
- **Target Framework**: `.net8.0` or higher recommended.
- **Dependencies**: Use `TUnit`, **NOT** `Microsoft.NET.Test.Sdk` (causes conflicts).
- **Polyfills**: Set `<EnableTUnitPolyfills>true</EnableTUnitPolyfills>` for .NET Framework targets.

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="TUnit" Version="x.x.x" />
</ItemGroup>
```

## 2. Core Test Syntax

Tests are defined by the `[Test]` attribute. Methods should generally be `async Task`.

### The "Must-Await" Rule

**Critical:** Assertions **must** be awaited. TUnit uses an analyzer to enforce this. Failure to await means the assertion is never executed.

```csharp
[Test]
public async Task BasicTest()
{
    var result = 1 + 1;
    await Assert.That(result).IsEqualTo(2);
}
```

## 3. Test Lifecycle and Isolation

- **Isolation:** By design, TUnit creates a **new instance of the class for every test**.
- **Static vs Instance Hooks:**
  - `[Before(Test)]` / `[After(Test)]`: Must be **instance** methods.
  - `[Before(Class/Assembly/TestSession)]`: Must be **static** methods.

### Ordering Hooks

- `Before` hooks run **bottom-up** (Base class first).
- `After` hooks run **top-down** (Derived class first).

## 4. Data-Driven Testing Patterns

TUnit offers several ways to inject data.

| Attribute | Use Case |
| :--- | :--- |
| `[Arguments(x, y)]` | Static/Constant data. |
| `[MethodDataSource(nameof(Func))]` | Dynamic data from a method (returns `IEnumerable<Func<T>>`). |
| `[ClassDataSource<T>]` | Injecting shared/complex objects (DI-like). |
| `[Matrix]` | Combinatorial testing across parameters. |

### Method Data Source Example (Best Practice)

Always return a `Func<T>` to ensure each test receives a fresh instance and avoids leaky state.

```csharp
public static IEnumerable<Func<int>> Data() => [() => 1, () => 2];

[Test]
[MethodDataSource(nameof(Data))]
public async Task DataTest(int value) => await Assert.That(value).IsPositive();
```

## 5. Integration Testing: ASP.NET Core

Use `WebApplicationFactory` with `[ClassDataSource]` for in-memory integration testing. Use `SharedType.PerTestSession` to optimize speed.

```csharp
public class IntegrationTests
{
    [ClassDataSource<WebApplicationFactory<Program>>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory<Program> Factory { get; init; }

    [Test]
    public async Task Get_Endpoint_ReturnsSuccess()
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/api/data");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
```

## 6. Parallelism Control

Parallelism is ON by default.

- **Disable for class:** `[NotInParallel]`
- **Constraint Keys:** `[NotInParallel("Database")]` (Tests with the same key won't run together).
- **Resource Limiting:** `[ParallelLimiter<MyLimit>]` where `MyLimit : IParallelLimit` defines a numeric `Limit`.

## 7. Dependencies (`[DependsOn]`)

Define execution order without disabling parallelism.

```csharp
[Test]
public async Task Step1() { ... }

[Test, DependsOn(nameof(Step1))]
public async Task Step2() { ... }
```

## 8. Assertion Cheat Sheet

The assertion engine supports chaining and "English-like" syntax.

### Common Patterns

- **Equality:** `await Assert.That(val).IsEqualTo(expected);`
- **Collections:** `await Assert.That(list).Contains(item).And.HasCount(5);`
- **Exceptions:** `await Assert.That(() => action()).Throws<InvalidOperationException>();`
- **Multiple:** `using var _ = Assert.Multiple();` (Groups errors without stopping).
- **Or/And Logic:** `await Assert.That(val).IsPositive().Or.IsEqualTo(-1);`

## 9. TestContext and Metadata

Inject or use `TestContext.Current` to access:

- `TestContext.Current.Result`: Status of the current test (useful in `After` hooks).
- `TestContext.Current.ObjectBag`: Shared storage for hooks.
- `TestContext.Configuration`: Access to `testconfig.json`.

## 10. CLI Filtering

Use `--treenode-filter` for granular selection:
`dotnet run --treenode-filter /*/Namespace/ClassName/*`

---
**Agent Warning:** Always check for `required` properties when using `Property Injection`. If a test class has `required` properties, they must be satisfied by a data attribute or `ClassDataSource`.
