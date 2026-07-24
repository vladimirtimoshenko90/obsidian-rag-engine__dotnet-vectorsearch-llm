using System.ClientModel;
using System.Text.Json;
using OpenAI;
using OpenAI.Chat;
using ObsidianRagEngine.Llm.DeepSeek.Utility;

namespace ObsidianRagEngine.Llm.DeepSeek;

/// <summary>
/// Thin DeepSeek API chat client (OpenAI-compatible). Prefer a long
/// <see cref="OpenAIClientOptions.NetworkTimeout"/> (e.g. 10 minutes) when constructing the
/// <see cref="OpenAIClient"/> for slower calls.
/// </summary>
public sealed class DeepSeekService(OpenAIClient openAIClient)
{
    public Task<string> CompleteChat(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        DeepSeekAiModel model = DeepSeekAiModel.Flash)
    {
        return CompleteChatCore(messages, ct, model, jsonMode: false);
    }

    public async Task<T> AskJson<T>(
        string question,
        CancellationToken ct,
        DeepSeekAiModel model = DeepSeekAiModel.Flash)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(AskJsonPromptBuilder.BuildSystemPrompt<T>()),
            new UserChatMessage(question),
        ];

        var json = await CompleteChatCore(messages, ct, model, jsonMode: true);

        try
        {
            return JsonSerializer.Deserialize<T>(json, AskJsonPromptBuilder.JsonOptions)!;
        }
        catch
        {
            // Docs: empty/bad JSON can happen — nudge the model and retry once.
            messages.Add(new SystemChatMessage(
                "Think carefully and thoughtfully. Return a valid non-empty JSON object that matches the example format."));

            json = await CompleteChatCore(messages, ct, model, jsonMode: true);
            return JsonSerializer.Deserialize<T>(json, AskJsonPromptBuilder.JsonOptions)!;
        }
    }

    private async Task<string> CompleteChatCore(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        DeepSeekAiModel model,
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
