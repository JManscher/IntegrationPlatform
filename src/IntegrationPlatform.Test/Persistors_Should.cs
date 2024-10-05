using System.Net;
using System.Net.Http.Json;
using IntegrationPlatform.AppHost.Entity;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.Test;

public class Persistors_Should()
{

    private static DistributedApplication? app;
    private const string PersistorTests = "PersistorTests";
    private static readonly string Id = "Integration-Test-Id";

    [Before(Class)]
    public static async Task Initialize()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.IntegrationPlatform_AppHost>();
        app = await appHost.BuildAsync();
        await app.StartAsync();
    }

    [After(Class)]
    public static async Task Cleanup()
    {
        if (app != null) {
            await app.StopAsync();
            await app.DisposeAsync();
            app = null;
        }

    }

    public static IEnumerable<EntityConfiguration> GetConfigurations()
    {
        var index = Environment.CurrentDirectory.IndexOf("bin");
        if(index == -1){
            index = 0;
        }
        string projectDirectory = Directory.GetCurrentDirectory()[..index]; //Gotta handle this better
        string slnRoot = Path.GetFullPath(Path.Combine(projectDirectory, "../.."));
        var entityLocation = Path.Combine(slnRoot, "configuration", "entities");

        return EntityConfigurator.GetConfigurations(entityLocation); ;
    }

    [Test]
    [MethodDataSource(nameof(GetConfigurations))]
    [NotInParallel(PersistorTests, Order = 1)]
    public async Task Return_Ok_Create_Event(EntityConfiguration entityConfiguration)
    {
        if (app == null)
            throw new InvalidOperationException("App is not initialized");

        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
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
                Id,
                Name = "Test",
                CustomerId = Guid.NewGuid().ToString()
            }
        });

        var content = await response.Content.ReadAsStringAsync();

        TestContext.Current?.OutputWriter.WriteLine(content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    }

    [Test]
    [MethodDataSource(nameof(GetConfigurations))]
    [NotInParallel(PersistorTests, Order = 2)]
    public async Task Return_Ok_On_Delete_Event(EntityConfiguration entityConfiguration)
    {
        if (app == null)
            throw new InvalidOperationException("App is not initialized");

        // Act
        var httpClient = app.CreateHttpClient(entityConfiguration.Name);
        var response = await httpClient.PostAsJsonAsync($"/{entityConfiguration.Name}/persist", new
        {
            EventType = "Delete",
            Entity = new
            {
                Id,
                Name = "Test",
                CustomerId = Id
            }
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    }

}

