using Microsoft.EntityFrameworkCore;
using TeacherOS.Database.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TeacherOS.Services;
using Microsoft.OpenApi.Models;
using Stripe;
using Microsoft.Extensions.FileProviders;
using TeacherOS.Hubs; // أو اكتب اسم الفولدر اللي أنت كريت جواه كلاس الـ NotificationHub بالظبط
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")   // عنوان الأنجولار
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();   // لو بتستخدم cookies
    });
});
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name="Authorization",
            Type=SecuritySchemeType.Http,
            Scheme="Bearer",
            BearerFormat="JWT",
            In=ParameterLocation.Header
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference=
                        new OpenApiReference
                        {
                            Type=
                                ReferenceType
                                .SecurityScheme,

                            Id="Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

builder.Services.AddDbContext<AppDbContext>(
options =>
options.UseNpgsql(
builder.Configuration
.GetConnectionString(
"DefaultConnection")));

builder.Services
.AddAuthentication(
JwtBearerDefaults.AuthenticationScheme)

.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer=true,
            ValidateAudience=true,
            ValidateLifetime=true,
            ValidateIssuerSigningKey=true,

            ValidIssuer=
                builder.Configuration
                ["Jwt:Issuer"],

            ValidAudience=
                builder.Configuration
                ["Jwt:Audience"],

            IssuerSigningKey=
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        builder.Configuration
                        ["Jwt:Key"]!
                    ))
        };
});

builder.Services.AddScoped<TeacherOS.Services.TokenService>();

builder.Services.AddScoped<TeacherOS.Services.FileService>();
builder.Services
.AddSignalR();
var app = builder.Build();
app.UseCors("AllowAngular");
StripeConfiguration.ApiKey =
    builder.Configuration
    ["Stripe:SecretKey"];

if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(
new StaticFileOptions
{
    FileProvider =
        new PhysicalFileProvider(
            Path.Combine
            (
                Directory.GetCurrentDirectory(),
                "Uploads"
            )
        ),

    RequestPath="/Uploads"
});


app.UseAuthentication();

app.UseAuthorization();


app.MapControllers();
app.MapHub<
NotificationHub
>(
"/notificationHub"
);

app.Run();