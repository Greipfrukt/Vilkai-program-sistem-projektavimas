using Synq.Common;
using Synq.Common.Events;
using Synq.Common.Health;

// A-07 – Search Service (FR-022, SHOULD)
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.AddSynqDefaults("search");
builder.Services.AddSynqRabbitMq(config, "search");
builder.Services.AddHealthChecks().AddPostgresCheck(config).AddRabbitMqCheck();

var app = builder.Build();

app.UseSynqDefaults();

app.Run();
