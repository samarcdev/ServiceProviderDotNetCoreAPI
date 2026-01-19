using APIServiceManagement.API.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.ConfigureApiPipeline();

app.Run();
