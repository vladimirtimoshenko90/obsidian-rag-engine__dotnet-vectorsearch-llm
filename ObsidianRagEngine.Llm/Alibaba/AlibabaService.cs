using ObsidianRagEngine.Llm.Common;
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
public sealed class AlibabaService(OpenAIClient openAIClient) : ILlmService
{
    public Task<string> Generate(string prompt, CancellationToken ct)
    {
        return CompleteChatCore([new UserChatMessage(prompt)], ct, AlibabaAiModel.Qwen37Plus, jsonMode: false);
    }

    public Task<string> CompleteChat(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        AlibabaAiModel model = AlibabaAiModel.Qwen37Plus)
    {
        return CompleteChatCore(messages, ct, model, jsonMode: false);
    }

    public async Task<T> AskJson<T>(
        string question,
        CancellationToken ct,
        AlibabaAiModel model = AlibabaAiModel.Qwen37Plus)
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
            messages.Add(new SystemChatMessage(AskJsonPromptBuilder.ClarificationPrompt));
            json = await CompleteChatCore(messages, ct, model, jsonMode: true);
            return JsonSerializer.Deserialize<T>(json, AskJsonPromptBuilder.JsonOptions)!;
        }
    }

    public Task<string> ExtractTextFromImage(
        ReadOnlyMemory<byte> imageBytes,
        string mediaType,
        CancellationToken ct,
        AlibabaAiModel model = AlibabaAiModel.Qwen37Plus)
    {
        ChatMessage message = new UserChatMessage(
            ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), mediaType),
            ChatMessageContentPart.CreateTextPart(
                """
                Extract all readable text from the image in reading order (top to bottom, left to right).
                Preserve line breaks that separate messages or paragraphs.
                Copy text exactly: keep original language, spelling, punctuation, and casing.
                Do not describe the image, translate, summarize, or invent missing words.
                If there is no text, reply with an empty response.
                """));

        return CompleteChatCore([message], ct, model, jsonMode: false);
    }

    private async Task<string> CompleteChatCore(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        AlibabaAiModel model,
        bool jsonMode)
    {
        var chatClient = openAIClient.GetChatClient(model.ToApiModelId());

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
