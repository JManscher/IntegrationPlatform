using System.Net;
using System.Net.Http.Json;
using IntegrationPlatform.AppHost.Entity;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.Test.Tests;

public class Persistors_Should()
{

    private static IDistributedApplicationTestingBuilder? appHost;
    private static DistributedApplication? app;
    private const string PersistorTests = "PersistorTests";

    [Before(Class)]
    public static async Task Initialize()
    {
        appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.IntegrationPlatform_AppHost>();
        app = await appHost.BuildAsync();
        await app.StartAsync();
    }

    [After(Class)]
    public static async Task Cleanup()
    {
        if(app != null)
            await app.DisposeAsync();
    }

    public static IEnumerable<EntityConfiguration> GetConfigurations()
    {
        string projectDirectory = Directory.GetCurrentDirectory()[..Environment.CurrentDirectory.IndexOf("bin")];
        string slnRoot = Path.GetFullPath(Path.Combine(projectDirectory, "../.."));
        var entityLocation = Path.Combine(slnRoot, "configuration", "entities");

        return EntityConfigurator.GetConfigurations(entityLocation);;
    }

    [Test]
    [MethodDataSource(nameof(GetConfigurations))]
    [NotInParallel(PersistorTests)]
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    }
}

