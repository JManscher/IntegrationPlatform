var builder = DistributedApplication.CreateBuilder(args);


var redis = builder.AddRedis("redis");

builder.AddDapr((options) =>
{
    options.EnableTelemetry = true;
});


builder.Build().Run();
 