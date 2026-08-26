namespace ProgressHomeHeating.Contracts;

public record AgentStatusResponse(bool IsConfigured, bool RagConfigured, bool ObservabilityConfigured);

public record AgentChatRequest(string Message, string? ConversationId);

public record AgentChatResponse(string ConversationId, string Reply);
