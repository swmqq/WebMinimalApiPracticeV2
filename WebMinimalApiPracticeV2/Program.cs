using Microsoft.EntityFrameworkCore;
using WebMinimalApiPracticeV2.Data;
using WebMinimalApiPracticeV2.Data.Entities;
using WebMinimalApiPracticeV2.Dtos;
using WebMinimalApiPracticeV2.Endpoints;

var builder = WebApplication.CreateBuilder();

//регистрируем нашу бд
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi(); //регистрируем спецификацию опен апи

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.MapAuthorEndpoints();
app.Run();

