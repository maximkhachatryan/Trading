using Trading.Worker;
using Trading.Worker.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

// Bind configuration
builder.Services.ConfigureMongoDb(builder.Configuration);

var host = builder.Build();
host.Run();


