using System.ComponentModel;
using Progress.Nuclia;
using Progress.Nuclia.Model;

namespace ProgressHomeHeating.AgentApi.Tools;

public class PolicyKnowledgeTools(INucliaDbClient client)
{
    [Description("Searches Progress Home Heating Oil's policy, safety, FAQ, and pricing knowledge base to answer questions about company policies, tank safety, pricing tiers, service area, and cancellation policy.")]
    public async Task<string> SearchKnowledgeBaseAsync(
        [Description("The customer or staff question to search the knowledge base for.")] string question)
    {
        var response = await client.Search.AskAsync(new AskRequest(question));
        var answer = response.Data?.Answer;
        return string.IsNullOrWhiteSpace(answer)
            ? "No relevant policy information found."
            : answer;
    }
}
