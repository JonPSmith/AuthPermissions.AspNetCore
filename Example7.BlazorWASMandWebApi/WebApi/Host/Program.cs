using Example7.BlazorWASMandWebApi.Application;
using Example7.BlazorWASMandWebApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();


// app.UseHttpsRedirection();
app.UseInfrastructure(builder.Configuration);
app.MapEndpoints();

app.Run();

