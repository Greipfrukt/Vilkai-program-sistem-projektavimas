using Synq.Common.Health;

// R-01–R-08 – Messaging Service (FR-011–019, 021), 2 instancijos
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSynqHealth();

var app = builder.Build();

app.MapSynqHealth();

app.Run();
