using Synq.Common;
using Synq.Common.Events;
using Synq.Common.Health;

// R-01–R-08 – Messaging Service (FR-011–019, 021), 2 instancijos
// Bendra biblioteka (P-02/P-03): AddSynqDbContext + AddOutboxRelay, DurableConsumer, LiveQueue – žr. Synq.Workspace/Program.cs pavyzdį.
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.AddSynqDefaults("messaging");
builder.Services.AddSynqRabbitMq(config, $"messaging:{config["Messaging:InstanceId"] ?? "local"}");
builder.Services.AddHealthChecks().AddPostgresCheck(config).AddRabbitMqCheck();

var app = builder.Build();

app.UseSynqDefaults();

app.Run();
