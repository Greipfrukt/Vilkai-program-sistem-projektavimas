using Synq.Common.Health;

// P-04 – API gateway (YARP). Maršrutai – appsettings.json pagal Synq.yaml „Gateway routing“.
// /internal/** ir /health/** neperduodami (jų maršrutų nėra).
// TODO P-04: SessionValidationMiddleware, OriginCheck (WS), Limits (3 sav.).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSynqHealth();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseWebSockets();
app.MapSynqHealth();
app.MapReverseProxy();

app.Run();
