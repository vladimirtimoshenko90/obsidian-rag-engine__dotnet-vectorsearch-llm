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
public sealed class AlibabaService(OpenAIClient openAIClient, AlibabaAiModel model) 
    : ILlmProvider, IOcrProvider
{
    public string ModelName => model.ToApiModelId();

    public Task<LlmCallResult> Complete(string prompt, CancellationToken ct) =>
        CompleteChatCore([new UserChatMessage(prompt)], ct, jsonMode: false, thinkingMode: false);

    public Task<LlmCallResult> CompleteChat(IReadOnlyList<ChatMessage> messages, CancellationToken ct, bool thinkingMode = false) =>
        CompleteChatCore(messages, ct, jsonMode: false, thinkingMode);

    public async Task<T> AskJson<T>(string question, CancellationToken ct, bool thinkingMode = false)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(AskJsonPromptBuilder.BuildSystemPrompt<T>()),
            new UserChatMessage(question),
        ];

        var json = await CompleteChatCore(messages, ct, jsonMode: true, thinkingMode);

        try
        {
            return JsonSerializer.Deserialize<T>(json.Text, AskJsonPromptBuilder.JsonOptions)!;
        }
        catch
        {
            // Docs: empty/bad JSON can happen — nudge the model and retry once.
            messages.Add(new SystemChatMessage(AskJsonPromptBuilder.ClarificationPrompt));
            json = await CompleteChatCore(messages, ct, jsonMode: true, thinkingMode);
            return JsonSerializer.Deserialize<T>(json.Text, AskJsonPromptBuilder.JsonOptions)!;
        }
    }

    public Task<LlmCallResult> ExtractText(byte[] imageBytes, IReadOnlyList<OcrLanguage> languages, CancellationToken ct)
    {
        ChatMessage message = new UserChatMessage(
            ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), LlmDefaults.OcrMediaType),
            ChatMessageContentPart.CreateTextPart(ImageTextExtractPrompt.Build(languages)));

        return CompleteChatCore([message], ct, jsonMode: false, thinkingMode: false);
    }

    private async Task<LlmCallResult> CompleteChatCore(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        bool jsonMode,
        bool thinkingMode)
    {
        var chatClient = openAIClient.GetChatClient(ModelName);

        var chatOptions = new ChatCompletionOptions { MaxOutputTokenCount = LlmDefaults.MaxOutputTokenCount };

        if (jsonMode)
            chatOptions.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();

        // Alibaba thinking is not first-class on the OpenAI SDK; set via JsonPatch.
        // API defaults thinking to enabled — always send an explicit toggle.
#pragma warning disable SCME0001 // JsonPatch is evaluation-only in System.ClientModel
        chatOptions.Patch.Set("$.enable_thinking"u8, thinkingMode);
#pragma warning restore SCME0001

        try
        {
            var result = await chatClient.CompleteChatAsync(messages, chatOptions, ct);
            return LlmCallResultFactory.FromChatCompletion(result.Value, model);
        }
        catch (ClientResultException ex)
        {
            throw LlmException.FromComplete("Alibaba", ex);
        }
    }
}
