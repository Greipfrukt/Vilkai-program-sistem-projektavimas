using Synq.Common.Health;

// A-08 – Notification Service (FR-023, SHOULD)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSynqHealth();

var app = builder.Build();

app.MapSynqHealth();

app.Run();
