using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.Exceptions;
using ObsidianRagEngine.Llm.Prompts;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace ObsidianRagEngine.Llm.Alibaba;

/// <summary>
/// Thin Alibaba Model Studio (DashScope) chat client (OpenAI-compatible). Prefer a long
/// <see cref="OpenAIClientOptions.NetworkTimeout"/> (e.g. 10 minutes) when constructing the
/// <see cref="OpenAIClient"/> for slower calls. Endpoint and API key must be from the same region.
/// </summary>
public sealed class AlibabaService(OpenAIClient openAIClient, AlibabaAiModel model) : ILlmService
{
    public string ModelName => model.ToApiModelId();

    public Task<string> Generate(string prompt, CancellationToken ct) =>
        CompleteChatCore([new UserChatMessage(prompt)], ct, jsonMode: false);

    public Task<string> CompleteChat(IReadOnlyList<ChatMessage> messages, CancellationToken ct) =>
        CompleteChatCore(messages, ct, jsonMode: false);

    public async Task<T> AskJson<T>(string question, CancellationToken ct)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(AskJsonPromptBuilder.BuildSystemPrompt<T>()),
            new UserChatMessage(question),
        ];

        var json = await CompleteChatCore(messages, ct, jsonMode: true);

        try
        {
            return JsonSerializer.Deserialize<T>(json, AskJsonPromptBuilder.JsonOptions)!;
        }
        catch
        {
            // Docs: empty/bad JSON can happen — nudge the model and retry once.
            messages.Add(new SystemChatMessage(AskJsonPromptBuilder.ClarificationPrompt));
            json = await CompleteChatCore(messages, ct, jsonMode: true);
            return JsonSerializer.Deserialize<T>(json, AskJsonPromptBuilder.JsonOptions)!;
        }
    }

    public Task<string> ExtractTextFromImage(
        ReadOnlyMemory<byte> imageBytes,
        string mediaType,
        CancellationToken ct,
        IReadOnlyList<OcrLanguage>? languages = null)
    {
        ChatMessage message = new UserChatMessage(
            ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), mediaType),
            ChatMessageContentPart.CreateTextPart(ImageTextExtractPrompt.Build(languages)));

        return CompleteChatCore([message], ct, jsonMode: false);
    }

    private async Task<string> CompleteChatCore(
        IReadOnlyList<ChatMessage> messages, 
        CancellationToken ct, 
        bool jsonMode)
    {
        var chatClient = openAIClient.GetChatClient(ModelName);

        var chatOptions = new ChatCompletionOptions { MaxOutputTokenCount = 20_000 };

        if (jsonMode)
            chatOptions.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();

        try
        {
            var result = await chatClient.CompleteChatAsync(messages, chatOptions, ct);
            return result.Value.Content is { Count: > 0 }
                ? result.Value.Content[0].Text ?? string.Empty
                : string.Empty;
        }
        catch (ClientResultException ex)
        {
            throw LlmException.FromComplete("Alibaba", ex);
        }
    }
}
