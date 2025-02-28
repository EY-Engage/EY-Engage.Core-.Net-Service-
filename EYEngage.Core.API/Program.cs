using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using EYEngage.Core.Infrastructure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using EYEngage.Core.Application;
using System.Text;


var builder = WebApplication.CreateBuilder(args);



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Ajout des services MVC et Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configuration de la pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Activation de CORS
app.UseCors("AllowAll");

// Ajout de l'authentification et de l'autorisation
app.UseAuthentication();
app.UseAuthorization();

// Mapping des contrôleurs
app.MapControllers();

// Lancer l'application
app.Run();
