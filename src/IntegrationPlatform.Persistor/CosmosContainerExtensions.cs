using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;


namespace IntegrationPlatform.Persistor;

public static class CosmosContainerExtensions
{
    public const string LastModifiedTriggerId = "lastModifiedTrigger";
    public static readonly string[] PreTriggers = [LastModifiedTriggerId];
    public static async Task SetupLastModifiedTrigger(this Container container, CancellationToken ct)
    {
        var trigger = new TriggerProperties
        {
            Id = LastModifiedTriggerId,
            Body = @"
                function lastModifiedTrigger() {
                    var context = getContext();
                    var request = context.getRequest();
                    var doc = request.getBody();
                    doc.lastModified = new Date();
                    request.setBody(doc);
                }",
            TriggerOperation = TriggerOperation.All,
            TriggerType = TriggerType.Pre,
        };

        var iterator = container.Scripts.GetTriggerQueryIterator<TriggerProperties>();

        while (iterator.HasMoreResults)
        {
            foreach (var existingTrigger in await iterator.ReadNextAsync(ct))
            {
                if (existingTrigger.Id == trigger.Id)
                {
                    await container.Scripts.ReplaceTriggerAsync(trigger, cancellationToken: ct);
                    return;
                }
            }
        }

        await container.Scripts.CreateTriggerAsync(trigger, cancellationToken: ct);

    }

    
}
