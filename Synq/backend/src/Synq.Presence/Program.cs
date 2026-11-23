using Synq.Common.Health;

// R-09 – Presence Service (FR-020)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSynqHealth();

var app = builder.Build();

app.MapSynqHealth();

app.Run();
