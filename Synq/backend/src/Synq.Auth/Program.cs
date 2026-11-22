using Synq.Common.Health;

// P-05 – Auth Service (FR-001–003)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSynqHealth();

var app = builder.Build();

app.MapSynqHealth();

app.Run();
