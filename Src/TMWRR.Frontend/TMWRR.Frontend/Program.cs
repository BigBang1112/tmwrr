using TMWRR.Frontend.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebServices(builder.Configuration);

var app = builder.Build();

app.ConfigureMiddleware();

app.Run();
