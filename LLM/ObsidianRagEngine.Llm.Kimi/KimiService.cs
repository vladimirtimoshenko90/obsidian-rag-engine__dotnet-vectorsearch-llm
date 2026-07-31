using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.Exceptions;
using ObsidianRagEngine.Llm.Prompts;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace ObsidianRagEngine.Llm.Kimi;

/// <summary>
/// Thin Kimi (Moonshot) API chat client (OpenAI-compatible). Prefer a long
/// <see cref="OpenAIClientOptions.NetworkTimeout"/> (e.g. 10 minutes) when constructing the
/// <see cref="OpenAIClient"/> for slower calls.
/// </summary>
public sealed class KimiService(OpenAIClient openAIClient, KimiAiModel model) 
    : ILlmProvider, IOcrProvider
{
    public string ModelName => model.ToApiModelId();

    public Task<string> Complete(string prompt, CancellationToken ct) =>
        CompleteChat([new UserChatMessage(prompt)], ct);

    public Task<string> CompleteChat(IReadOnlyList<ChatMessage> messages, CancellationToken ct, bool thinkingMode = false) =>
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
            return JsonSerializer.Deserialize<T>(json, AskJsonPromptBuilder.JsonOptions)!;
        }
        catch
        {
            // Docs: empty/bad JSON can happen — nudge the model and retry once.
            messages.Add(new SystemChatMessage(AskJsonPromptBuilder.ClarificationPrompt));
            json = await CompleteChatCore(messages, ct, jsonMode: true, thinkingMode);
            return JsonSerializer.Deserialize<T>(json, AskJsonPromptBuilder.JsonOptions)!;
        }
    }

    public Task<string> ExtractText(byte[] imageBytes, IReadOnlyList<OcrLanguage> languages, CancellationToken ct)
    {
        ChatMessage message = new UserChatMessage(
            ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), LlmDefaults.OcrMediaType),
            ChatMessageContentPart.CreateTextPart(ImageTextExtractPrompt.Build(languages)));

        return CompleteChatCore([message], ct, jsonMode: false, thinkingMode: false);
    }

    private async Task<string> CompleteChatCore(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        bool jsonMode,
        bool thinkingMode)
    {
        var chatClient = openAIClient.GetChatClient(ModelName);

        var chatOptions = new ChatCompletionOptions { MaxOutputTokenCount = LlmDefaults.MaxOutputTokenCount };

        if (jsonMode)
            chatOptions.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();

        // Kimi thinking is not first-class on the OpenAI SDK; set via JsonPatch.
        // K2_6: hybrid — toggle thinking.type. K3: always thinks — map to reasoning_effort.
#pragma warning disable SCME0001 // JsonPatch is evaluation-only in System.ClientModel
        switch (model)
        {
            case KimiAiModel.K2_6:
                chatOptions.Patch.Set("$.thinking.type"u8, thinkingMode ? "enabled" : "disabled");
                break;
            case KimiAiModel.K3:
                chatOptions.Patch.Set("$.reasoning_effort"u8, thinkingMode ? "max" : "low");
                break;
        }
#pragma warning restore SCME0001

        try
        {
            var result = await chatClient.CompleteChatAsync(messages, chatOptions, ct);
            return result.Value.Content is { Count: > 0 }
                ? result.Value.Content[0].Text ?? string.Empty
                : string.Empty;
        }
        catch (ClientResultException ex)
        {
            throw LlmException.FromComplete("Kimi", ex);
        }
    }
}
