using Synq.Common.Health;

// A-07 – Search Service (FR-022, SHOULD)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSynqHealth();

var app = builder.Build();

app.MapSynqHealth();

app.Run();
