using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace ObsidianRagEngine.Llm.DeepSeek;

/// <summary>
/// Thin DeepSeek API chat client (OpenAI-compatible). Prefer a long
/// <see cref="OpenAIClientOptions.NetworkTimeout"/> (e.g. 10 minutes) when constructing the
/// <see cref="OpenAIClient"/> for slower calls.
/// </summary>
public sealed class DeepSeekService(OpenAIClient openAIClient)
{
    public async Task<string> Complete(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        DeepSeekModel model = DeepSeekModel.Flash)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count == 0)
            throw new ArgumentException("At least one message is required.", nameof(messages));

        var chatClient = openAIClient.GetChatClient(model.ToApiModelId());

        try
        {
            ClientResult<ChatCompletion> result = await chatClient
                .CompleteChatAsync(messages, cancellationToken: ct)
                .ConfigureAwait(false);

            ChatCompletion completion = result.Value;
            if (completion.Content is not { Count: > 0 })
                return string.Empty;

            return completion.Content[0].Text ?? string.Empty;
        }
        catch (ClientResultException ex)
        {
            throw DeepSeekException.FromComplete(ex);
        }
    }
}
