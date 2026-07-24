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
    // System prompt for JSON mode — includes "json" + example shape (DeepSeek API requirement).
    private const string JsonModeSystemPrompt = """
        User asks a question. 
        Find the correct answer. 
        Reply only in the JSON format specified below.

        EXAMPLE INPUT:
        Which is the highest mountain in the world?

        EXAMPLE JSON OUTPUT:
        {
            "question": "Which is the highest mountain in the world?",
            "answer": "Mount Everest"
        }
        """;

    public Task<string> CompleteChat(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        DeepSeekModel model = DeepSeekModel.Flash)
    {
        return CompleteChatCore(messages, ct, model, jsonMode: false);
    }

    /// <summary>
    /// JSON mode with a fixed system prompt (example schema) and a user question message.
    /// </summary>
    public Task<string> AskJson(
        string question,
        CancellationToken ct,
        DeepSeekModel model = DeepSeekModel.Flash)
    {
        return CompleteChatCore(
            [
                new SystemChatMessage(JsonModeSystemPrompt),
                new UserChatMessage(question),
            ],
            ct,
            model,
            jsonMode: true);
    }

    private async Task<string> CompleteChatCore(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        DeepSeekModel model,
        bool jsonMode)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count == 0)
            throw new ArgumentException("At least one message is required.", nameof(messages));

        var chatClient = openAIClient.GetChatClient(model.ToApiModelId());
        
        var chatOptions = new ChatCompletionOptions { MaxOutputTokenCount = 20_000 };
        if (jsonMode) chatOptions.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();

        try
        {
            ClientResult<ChatCompletion> result = await chatClient.CompleteChatAsync(messages, chatOptions, ct);

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
