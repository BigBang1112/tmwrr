﻿namespace TMWRR.Tests;

public class Tests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    [Skip("Sample test")]
    public async Task Test()
    {
        var client = WebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/ping");

        var stringContent = await response.Content.ReadAsStringAsync();

        await Assert.That(stringContent).IsEqualTo("Hello, World!");
    }
}