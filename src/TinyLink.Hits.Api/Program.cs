using Microsoft.AspNetCore.Authentication.JwtBearer;
using TinyLink.Core.ExtensionMethods;
using TinyLink.Hits.TableStorage.ExtensionMethods;

var corsPolicyName = "DefaultCors";


var builder = WebApplication.CreateBuilder(args);
// Add services to the container.

builder.Services.AddTinyLinksCore(builder.Configuration)
    .AddTinyLinkHitsWithTableStorage();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicyName,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://app.tinylnk.nl")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});


builder.Services.AddControllers()
    .AddNewtonsoftJson();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = "https://urlshortner.eu.auth0.com/";
    options.Audience = "https://api.tinylnk.nl";
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(corsPolicyName);
app.UseAuthorization();
app.MapControllers();

app.Run();