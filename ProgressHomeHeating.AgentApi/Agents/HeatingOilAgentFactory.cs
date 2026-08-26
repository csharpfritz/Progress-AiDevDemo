using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ProgressHomeHeating.AgentApi.Tools;

namespace ProgressHomeHeating.AgentApi.Agents;

public class HeatingOilAgentFactory
{
    private const string Instructions = """
        You are the customer service and dispatch assistant for Progress Home Heating Oil,
        a residential heating oil delivery company. You help staff answer questions about
        customers, oil tank levels, and delivery scheduling. Use the available tools to look
        up real data rather than guessing. When scheduling a delivery, confirm the customer
        and gallons requested back to the user. Keep responses concise and professional.
        """;

    private readonly IChatClient? _chatClient;

    public bool IsConfigured => _chatClient is not null;

    public HeatingOilAgentFactory(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var deployment = configuration["AzureOpenAI:DeploymentName"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deployment))
        {
            return;
        }

        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        IChatClient chatClient = azureClient.GetChatClient(deployment).AsIChatClient();

        var observabilityEndpoint = configuration["ProgressObservability:Endpoint"];
        var observabilityApiKey = configuration["ProgressObservability:ApiKey"];
        if (!string.IsNullOrWhiteSpace(observabilityEndpoint) && !string.IsNullOrWhiteSpace(observabilityApiKey))
        {
            chatClient = chatClient.AddObservability(o =>
            {
                o.AppName = configuration["ProgressObservability:AppName"] ?? "ProgressHomeHeating.AgentApi";
                o.Endpoint = observabilityEndpoint;
                o.ApiKey = observabilityApiKey;
            });
        }

        _chatClient = chatClient;
    }

    public AIAgent Create(OperationsApiTools tools, PolicyKnowledgeTools? policyTools)
    {
        if (_chatClient is null)
        {
            throw new InvalidOperationException("Azure OpenAI is not configured.");
        }

        List<AITool> aiTools =
        [
            AIFunctionFactory.Create(tools.GetLowOilCustomersAsync),
            AIFunctionFactory.Create(tools.GetCustomerByNameAsync),
            AIFunctionFactory.Create(tools.ScheduleDeliveryAsync),
        ];

        if (policyTools is not null)
        {
            aiTools.Add(AIFunctionFactory.Create(policyTools.SearchKnowledgeBaseAsync));
        }

        var instructions = $"{Instructions}\n\nToday's date is {DateOnly.FromDateTime(DateTime.Now):yyyy-MM-dd}. Use this when interpreting relative dates and when scheduling deliveries.";

        return new ChatClientAgent(
            _chatClient,
            instructions: instructions,
            name: "HeatingOilAgent",
            description: null,
            tools: aiTools);
    }
}
