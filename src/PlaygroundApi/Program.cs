using Azure.Identity;
using Microsoft.IdentityModel.S2S.Configuration;
using Microsoft.IdentityModel.S2S.Extensions.AspNetCore;
using PlaygroundApi;
using PlaygroundApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//PostConfigure Auth
builder.Services.AddSingleton<IS2SAuthenticationManagerPostConfigure, ConfigureAuth>();
builder.Services.AddAuthentication()
     .AddMiseWithDefaultAuthentication(
                configuration: builder.Configuration,
                authenticationSectionName: "S2SAuthentication",
                
                configureS2SOptions: options =>
                {
                    options.ForwardDefaultSelector = c => "S2SAuthentication";
                    options.Events = new S2SAuthenticationEvents
                    {
                        OnAuthenticationMessageReceived = c =>
                        {
                            return Task.CompletedTask;
                        },
                        OnRequestValidated = c =>
                        {
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = c =>
                        {
                            return Task.CompletedTask;
                        },
                        OnForbidden = c =>
                        {
                            return Task.CompletedTask;
                        }
                    };
                });

//Certificate Store
builder.Services.AddSingleton<CertificateStore>();

//Background loader service
builder.Services.AddSingleton<DefaultAzureCredential>();
builder.Services.AddOptions<CertificateLoaderServiceOptions>()
               .Bind(builder.Configuration.GetSection(CertificateLoaderServiceOptions.SectionName));
builder.Services.AddHostedService<CertificateLoaderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
