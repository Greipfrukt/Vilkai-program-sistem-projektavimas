using Synq.Common;
using Synq.Common.Events;
using Synq.Common.Health;

// A-08 – Notification Service (FR-023, SHOULD)
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.AddSynqDefaults("notification");
builder.Services.AddSynqRabbitMq(config, "notification");
builder.Services.AddHealthChecks().AddPostgresCheck(config).AddRabbitMqCheck();

var app = builder.Build();

app.UseSynqDefaults();

app.Run();
