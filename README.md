# IntegrationPlatform

## TODO

- Better handling of data on the BaseDomainObject, right now the TTL etc could be set by external callers. Should be more fool proof. Perhaps use a read and write model approach?
  - Could also be solved using cosmosDB triggers [documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-write-stored-procedures-triggers-udfs?tabs=javascript)
- Dynamic Changefeed publishers based on configs
- Expose APIs as GraphQL perhaps? Autogenerate somehow
- What about indexes? Allow for those through configs perhaps?
