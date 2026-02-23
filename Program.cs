using ApiOptimizerService;
using ApiOptimizerService.Data;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.EntityFrameworkCore;
using OpenAI;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Azure OpenAI (v2 SDK)
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();

    var endpoint = config["AzureOpenAI:Endpoint"];
    var apiKey = config["AzureOpenAI:ApiKey"];

    return new AzureOpenAIClient(
        new Uri(endpoint),
        new AzureKeyCredential(apiKey));
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
