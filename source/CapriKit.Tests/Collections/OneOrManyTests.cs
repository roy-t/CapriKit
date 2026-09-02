using System.Runtime.InteropServices;
using CapriKit.Collections;

namespace CapriKit.Tests.Collections;

internal class OneOrManyTests
{
    [Test]
    public async Task Add()
    {
        var values = new OneOrMany<string>("a");

        values.Add("b");
        values.Add("c");

        await Assert.That(values.Count).IsEqualTo(3);
        await Assert.That(values[0]).IsEqualTo("a");
        await Assert.That(values[1]).IsEqualTo("b");
        await Assert.That(values[2]).IsEqualTo("c");
    }

    [Test]
    public async Task Add_GrowsBeyondInitialTailCapacity()
    {
        var values = new OneOrMany<int>();

        // The tail array starts at two elements, so this forces it to grow twice
        for (var i = 0; i < 8; i++)
        {
            values.Add(i);
        }

        await Assert.That(values.Count).IsEqualTo(8);
        for (var i = 0; i < 8; i++)
        {
            await Assert.That(values[i]).IsEqualTo(i);
        }
    }

    [Test]
    public async Task Indexer_OutOfRange()
    {
        var values = new OneOrMany<string>("a");

        await Assert.That(() => values[1]).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => values[-1]).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Remove()
    {
        var values = new OneOrMany<string>("a");
        values.Add("b");
        values.Add("c");

        var removed = values.Remove("b");

        await Assert.That(removed).IsTrue();
        await Assert.That(values.Count).IsEqualTo(2);
        await Assert.That(values[0]).IsEqualTo("a");
        await Assert.That(values[1]).IsEqualTo("c");
        await Assert.That(values.Contains("b")).IsFalse();
    }

    [Test]
    public async Task Remove_Head()
    {
        var values = new OneOrMany<string>("a");
        values.Add("b");

        // Removing the inline value has to promote the first value of the tail into it
        var removed = values.Remove("a");

        await Assert.That(removed).IsTrue();
        await Assert.That(values.Count).IsEqualTo(1);
        await Assert.That(values[0]).IsEqualTo("b");
    }

    [Test]
    public async Task Clear()
    {
        var values = new OneOrMany<string>("a");
        values.Add("b");

        values.Clear();

        await Assert.That(values.Count).IsEqualTo(0);
        await Assert.That(values.Contains("a")).IsFalse();
    }

    [Test]
    public async Task GetEnumerator()
    {
        var values = new OneOrMany<string>("a");
        values.Add("b");
        values.Add("c");

        var enumerated = new List<string>();
        foreach (var value in values)
        {
            enumerated.Add(value);
        }

        await Assert.That(enumerated).IsEquivalentTo(new List<string> { "a", "b", "c" });
    }

    [Test]
    public async Task UsedAsDictionaryValue()
    {
        var map = new Dictionary<string, OneOrMany<int>>();

        // The pattern this type is meant for: mutate the value in place, without copying it out
        foreach (var (key, value) in new[] { ("odd", 1), ("even", 2), ("odd", 3) })
        {
            ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(map, key, out _);
            values.Add(value);
        }

        await Assert.That(map["odd"].Count).IsEqualTo(2);
        await Assert.That(map["odd"][0]).IsEqualTo(1);
        await Assert.That(map["odd"][1]).IsEqualTo(3);
        await Assert.That(map["even"].Count).IsEqualTo(1);
        await Assert.That(map["even"][0]).IsEqualTo(2);
    }
}
