using Synq.Common.Health;

// P-06–P-09 – Workspace Service (FR-004–010)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSynqHealth();

var app = builder.Build();

app.MapSynqHealth();

app.Run();
