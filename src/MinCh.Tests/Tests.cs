using MinCh.Tests.Data;

namespace MinCh.Tests;

public class Tests
{
    [Test]
    public void Basic()
    {

    }

    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(2, 3, 5)]
    public async Task DataDrivenArguments(int a, int b, int c)
    {


        var result = a + b;

        await Assert.That(result).IsEqualTo(c);
    }

    [Test]
    [MethodDataSource(nameof(DataSource))]
    public async Task MethodDataSource(int a, int b, int c)
    {


        var result = a + b;

        await Assert.That(result).IsEqualTo(c);
    }

    [Test]
    [ClassDataSource<DataClass>]
    [ClassDataSource<DataClass>(Shared = SharedType.PerClass)]
    [ClassDataSource<DataClass>(Shared = SharedType.PerAssembly)]
    [ClassDataSource<DataClass>(Shared = SharedType.PerTestSession)]
    public void ClassDataSource(DataClass dataClass)
    {



    }

    [Test]
    [DataGenerator]
    public async Task CustomDataGenerator(int a, int b, int c)
    {


        var result = a + b;

        await Assert.That(result).IsEqualTo(c);
    }

    public static IEnumerable<(int a, int b, int c)> DataSource()
    {
        yield return (1, 1, 2);
        yield return (2, 1, 3);
        yield return (3, 1, 4);
    }
}
