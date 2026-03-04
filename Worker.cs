using ApiOptimizerService.Data;
using ApiOptimizerService.Models;
using Azure.AI.OpenAI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AzureOpenAIClient _azureClient;
    private readonly IConfiguration _config;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceScopeFactory scopeFactory,
        AzureOpenAIClient azureClient,
        IConfiguration config,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _azureClient = azureClient;
        _config = config;
        _logger = logger;
    }

    // Runs every 5 minutes to analyze API metrics, get AI suggestions, and generate reports
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Optimizer Service Started...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var last5Minutes = DateTime.UtcNow.AddMinutes(-5);

                var metrics = await db.ApiMetrics
                    .Where(x => x.Timestamp >= last5Minutes)
                    .ToListAsync(stoppingToken);

                if (!metrics.Any())
                {
                    _logger.LogInformation("No metrics found in last 5 minutes.");
                }
                else
                {
                    var summary = GenerateSummary(metrics);

                    var aiSuggestion = await CallAzureOpenAI(summary);

                    var report = new OptimizationReport
                    {
                        Summary = summary,
                        AISuggestion = aiSuggestion,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.OptimizationReports.Add(report);
                    await db.SaveChangesAsync(stoppingToken);

                    GeneratePdf(report);

                    _logger.LogInformation("Optimization report generated successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in AI Optimizer Service.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private string GenerateSummary(List<ApiMetric> metrics)
    {
        var totalRequests = metrics.Count;
        var avgResponse = metrics.Average(x => x.ResponseTimeMs);
        var maxResponse = metrics.Max(x => x.ResponseTimeMs);
        var errorCount = metrics.Count(x => x.StatusCode >= 500);
        var errorRate = totalRequests == 0 ? 0 : (errorCount * 100.0 / totalRequests);

        return $"""
        API Performance Summary (Last 5 Minutes):

        Total Requests: {totalRequests}
        Average Response Time: {avgResponse} ms
        Maximum Response Time: {maxResponse} ms
        Error Rate: {errorRate} %
        """;
    }

    private async Task<string> CallAzureOpenAI(string summary)
    {
        var deploymentName = _config["AzureOpenAI:DeploymentName"];

        var chatClient = _azureClient.GetChatClient(deploymentName);

        var messages = new List<ChatMessage>
    {
        new SystemChatMessage(
            "You are a senior .NET backend performance engineer. " +
            "Provide clear and structured optimization suggestions in bullet points."
        ),
        new UserChatMessage(
            $"Analyze the following API performance data and suggest improvements:\n{summary}"
        )
    };

        var options = new ChatCompletionOptions
        {
            Temperature = 0.2f,
            MaxOutputTokenCount = 800
        };

        var response = await chatClient.CompleteChatAsync(messages, options);

        return response.Value.Content[0].Text;
    }

    private void GeneratePdf(OptimizationReport report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Directory.CreateDirectory("Reports");

        var filePath = $"Reports/OptimizationReport_{report.Id}.pdf";

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text("AI API Optimization Report")
                        .FontSize(20)
                        .Bold();

                    col.Item().Text($"Generated On: {report.CreatedAt}");

                    col.Item().LineHorizontal(1);

                    col.Item().Text("Performance Summary:")
                        .Bold();

                    col.Item().Text(report.Summary);

                    col.Item().LineHorizontal(1);

                    col.Item().Text("AI Recommendations:")
                        .Bold();

                    col.Item().Text(report.AISuggestion);
                });
            });
        })
        .GeneratePdf(filePath);
    }
}