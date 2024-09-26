using System.Net;
using System.Net.Http.Json;
using IntegrationPlatform.AppHost.Entity;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace IntegrationPlatform.Test.Tests;

public class Persistors_Should(ITestOutputHelper output) : IAsyncLifetime
{

    private IDistributedApplicationTestingBuilder? appHost;
    private DistributedApplication? app;
    private readonly ITestOutputHelper _output = output;

    public async Task InitializeAsync()
    {
        appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.IntegrationPlatform_AppHost>();
        app = await appHost.BuildAsync();
        await app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if(app != null)
            await app.DisposeAsync();
    }

    public static TheoryData<EntityConfiguration> GetConfigurations()
    {
        string projectDirectory = Directory.GetCurrentDirectory()[..Environment.CurrentDirectory.IndexOf("bin")];
        string slnRoot = Path.GetFullPath(Path.Combine(projectDirectory, "../.."));
        var entityLocation = Path.Combine(slnRoot, "configuration", "entities");

        var entityConfigurations = EntityConfigurator.GetConfigurations(entityLocation);
        return new TheoryData<EntityConfiguration>(entityConfigurations);
    }

    [Theory]
    [MemberData(nameof(GetConfigurations))]
    public async Task Return_Ok_On_Post(EntityConfiguration entityConfiguration)
    {
        if(app == null)
            throw new InvalidOperationException("Application is not initialized");
        
        // Arrange
        var resourceNotificationService =
            app.Services.GetRequiredService<ResourceNotificationService>();
            
        await resourceNotificationService
            .WaitForResourceAsync(entityConfiguration.Name, KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));

        // Act
        _output.WriteLine($"Testing {entityConfiguration.Name}");

        var httpClient = app.CreateHttpClient(entityConfiguration.Name);
        var response = await httpClient.PostAsJsonAsync($"/{entityConfiguration.Name}/persist", new
        {
            EventType = "Create",
            Entity = new
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test",
                CustomerId = Guid.NewGuid().ToString()
            }
        });

        var content = await response.Content.ReadAsStringAsync();
        if(string.IsNullOrEmpty(content) is false)
            _output.WriteLine(content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    }
}

