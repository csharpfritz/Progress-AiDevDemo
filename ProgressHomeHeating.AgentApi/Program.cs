using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using OpenTelemetry.Instrumentation.Http;
using Progress.Nuclia;
using Progress.Nuclia.Extensions;
using Progress.Observability.Extensions.AI;
using ProgressHomeHeating.AgentApi.Agents;
using ProgressHomeHeating.AgentApi.Clients;
using ProgressHomeHeating.AgentApi.Tools;
using ProgressHomeHeating.Contracts;

// Tag every trace this service produces — the incoming agent request and every outbound
// call it triggers (tools -> OperationsApi, LLM completions, RAG lookups) — with a
// consistent attribute so they're easy to find/filter in the Aspire dashboard's trace view.
const string AgenticWorkflowTagKey = "workflow";
const string AgenticWorkflowTagValue = "agentic-scheduling";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();

// Everything AgentApi calls out to is part of the agentic workflow, so tag every
// outgoing HTTP span (tool calls to OperationsApi, Azure OpenAI completions, Nuclia RAG).
builder.Services.Configure<HttpClientTraceInstrumentationOptions>(o =>
    o.EnrichWithHttpRequestMessage = (activity, _) =>
        activity.SetTag(AgenticWorkflowTagKey, AgenticWorkflowTagValue));

builder.Services.AddHttpClient<OperationsApiClient>(client =>
{
    client.BaseAddress = new Uri("http://operationsapi");
});

builder.Services.AddScoped<OperationsApiTools>();
builder.Services.AddSingleton<HeatingOilAgentFactory>();
builder.Services.AddSingleton<ConversationStore>();

var ragZoneId = builder.Configuration["ProgressRag:ZoneId"];
var ragKnowledgeBoxId = builder.Configuration["ProgressRag:KnowledgeBoxId"];
var ragApiKey = builder.Configuration["ProgressRag:ApiKey"];
var isRagConfigured = !string.IsNullOrWhiteSpace(ragZoneId)
    && !string.IsNullOrWhiteSpace(ragKnowledgeBoxId)
    && !string.IsNullOrWhiteSpace(ragApiKey);

if (isRagConfigured)
{
    builder.Services.AddNucliaDb(new NucliaDbConfig(ragZoneId!, ragKnowledgeBoxId!, ragApiKey!));
    builder.Services.AddScoped<PolicyKnowledgeTools>();
}

var isObservabilityConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["ProgressObservability:Endpoint"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["ProgressObservability:ApiKey"]);

var app = builder.Build();

var isAgentConfigured = app.Services.GetRequiredService<HeatingOilAgentFactory>().IsConfigured;
var configurationStatus = new AgentConfigurationStatus(isAgentConfigured, isRagConfigured, isObservabilityConfigured);

app.MapDefaultEndpoints();

if (isObservabilityConfigured)
{
    app.Lifetime.ApplicationStopping.Register(() => ObservabilityTracer.Shutdown());
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/agent/status", () =>
    Results.Ok(new AgentStatusResponse(
        configurationStatus.IsAgentConfigured,
        configurationStatus.IsRagConfigured,
        configurationStatus.IsObservabilityConfigured)));

app.MapPost("/api/agent/chat", async (
    AgentChatRequest request,
    HeatingOilAgentFactory agentFactory,
    ConversationStore conversations,
    IServiceProvider services) =>
{
    Activity.Current?.SetTag(AgenticWorkflowTagKey, AgenticWorkflowTagValue);

    if (!agentFactory.IsConfigured)
    {
        return Results.Problem(
            title: "Agent not configured",
            detail: "Azure OpenAI credentials are not set. Configure AzureOpenAI:Endpoint, AzureOpenAI:ApiKey, and AzureOpenAI:DeploymentName via user-secrets.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var opsTools = services.GetRequiredService<OperationsApiTools>();
    var policyTools = services.GetService<PolicyKnowledgeTools>();

    var conversationId = string.IsNullOrWhiteSpace(request.ConversationId) ? Guid.NewGuid().ToString() : request.ConversationId;
    var agent = agentFactory.Create(opsTools, policyTools);
    var session = await conversations.GetOrCreateAsync(conversationId, agent);

    var response = await agent.RunAsync(request.Message, session);
    var reply = response.Text ?? string.Empty;

    return Results.Ok(new AgentChatResponse(conversationId, reply));
});

app.MapGet("/api/agent/chat/stream", (
    string message,
    string conversationId,
    HeatingOilAgentFactory agentFactory,
    ConversationStore conversations,
    IServiceProvider services,
    CancellationToken ct) =>
{
    Activity.Current?.SetTag(AgenticWorkflowTagKey, AgenticWorkflowTagValue);

    if (!agentFactory.IsConfigured)
    {
        return Results.Problem(
            title: "Agent not configured",
            detail: "Azure OpenAI credentials are not set. Configure AzureOpenAI:Endpoint, AzureOpenAI:ApiKey, and AzureOpenAI:DeploymentName via user-secrets.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var opsTools = services.GetRequiredService<OperationsApiTools>();
    var policyTools = services.GetService<PolicyKnowledgeTools>();
    var agent = agentFactory.Create(opsTools, policyTools);

    return TypedResults.ServerSentEvents(StreamReplyAsync(agent, conversations, conversationId, message, ct));
});

app.Run();

static async IAsyncEnumerable<string> StreamReplyAsync(
    AIAgent agent,
    ConversationStore conversations,
    string conversationId,
    string message,
    [EnumeratorCancellation] CancellationToken ct)
{
    var session = await conversations.GetOrCreateAsync(conversationId, agent);

    await foreach (var update in agent.RunStreamingAsync(message, session, cancellationToken: ct))
    {
        if (!string.IsNullOrEmpty(update.Text))
        {
            yield return update.Text;
        }
    }
}
