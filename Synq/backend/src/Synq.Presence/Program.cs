using Synq.Common;
using Synq.Common.Caching;
using Synq.Common.Events;
using Synq.Common.Health;

// R-09 – Presence Service (FR-020)
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.AddSynqDefaults("presence");
builder.Services.AddSynqRedis(config);
builder.Services.AddSynqRabbitMq(config, "presence");
builder.Services.AddHealthChecks().AddRedisCheck().AddRabbitMqCheck();

var app = builder.Build();

app.UseSynqDefaults();

app.Run();
