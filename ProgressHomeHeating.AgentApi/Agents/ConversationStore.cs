using System.Collections.Concurrent;
using Microsoft.Agents.AI;

namespace ProgressHomeHeating.AgentApi.Agents;

public class ConversationStore
{
    private readonly ConcurrentDictionary<string, Task<AgentSession>> _sessions = new();

    public Task<AgentSession> GetOrCreateAsync(string conversationId, AIAgent agent) =>
        _sessions.GetOrAdd(conversationId, _ => agent.CreateSessionAsync().AsTask());
}
