using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.Web.Services;

public class AgentApiClient(HttpClient http)
{
    public async Task<AgentStatusResponse> GetStatusAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<AgentStatusResponse>("/api/agent/status", ct)
            ?? new AgentStatusResponse(false, false, false);

    public async Task<AgentChatResponse?> SendMessageAsync(AgentChatRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/agent/chat", request, ct);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<AgentChatResponse>(ct)
            : null;
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(string message, string conversationId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var url = $"/api/agent/chat/stream?message={Uri.EscapeDataString(message)}&conversationId={Uri.EscapeDataString(conversationId)}";
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        await foreach (var item in SseParser.Create(stream).EnumerateAsync(ct))
        {
            yield return item.Data;
        }
    }
}
