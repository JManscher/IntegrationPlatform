using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IntegrationPlatform.AppHost.Entity;

public static class EntityConfigurator
{
    public static IEnumerable<EntityConfiguration> GetConfigurations(string entityLocation)
    {
        var yamlFiles = Directory.GetFiles(entityLocation, "*.yaml");
        var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        foreach (var yamlFile in yamlFiles)
        {

            // TODO: Better handling of parsing and exceptions
            var yamlContent = File.ReadAllText(yamlFile);
            yield return deserializer.Deserialize<EntityConfiguration>(yamlContent);
        }
    }
}