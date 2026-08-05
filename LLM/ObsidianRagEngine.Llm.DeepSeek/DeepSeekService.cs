using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.Exceptions;
using ObsidianRagEngine.Llm.Prompts;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace ObsidianRagEngine.Llm.DeepSeek;

/// <summary>
/// Thin DeepSeek API chat client (OpenAI-compatible). Prefer a long
/// <see cref="OpenAIClientOptions.NetworkTimeout"/> (e.g. 10 minutes) when constructing the
/// <see cref="OpenAIClient"/> for slower calls.
/// </summary>
public sealed class DeepSeekService(OpenAIClient openAIClient, DeepSeekAiModel model) 
    : ILlmProvider, ILlmJsonProvider
{
    public string ModelName => model.ToApiModelId();

    public Task<LlmCallResult> Complete(string prompt, CancellationToken ct, bool thinkingMode = false) =>
        CompleteChatCore([new UserChatMessage(prompt)], ct, jsonMode: false, thinkingMode);

    public Task<LlmCallResult> CompleteChat(IReadOnlyList<ChatMessage> messages, CancellationToken ct, bool thinkingMode = false) =>
        CompleteChatCore(messages, ct, jsonMode: false, thinkingMode);

    public async Task<LlmCallResult<T>> AskJson<T>(string question, CancellationToken ct, bool thinkingMode = false)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage(AskJsonPromptBuilder.BuildSystemPrompt<T>()),
            new UserChatMessage(question),
        ];

        try
        {
            var result = await CompleteChatCore(messages, ct, jsonMode: true, thinkingMode);
            return result.ToTyped(text => JsonSerializer.Deserialize<T>(text, AskJsonPromptBuilder.JsonOptions)!);
        }
        catch
        {
            // Docs: empty/bad JSON can happen — nudge the model and retry once.
            messages.Add(new SystemChatMessage(AskJsonPromptBuilder.ClarificationPrompt));
            var result = await CompleteChatCore(messages, ct, jsonMode: true, thinkingMode);
            return result.ToTyped(text => JsonSerializer.Deserialize<T>(text, AskJsonPromptBuilder.JsonOptions)!);
        }
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

        // DeepSeek thinking is not first-class on the OpenAI SDK; set via JsonPatch.
        // API defaults thinking to enabled — always send an explicit toggle.
#pragma warning disable SCME0001 // JsonPatch is evaluation-only in System.ClientModel
        if (thinkingMode)
        {
            chatOptions.Patch.Set("$.thinking.type"u8, "enabled");
            chatOptions.Patch.Set("$.reasoning_effort"u8, "max");
        }
        else
        {
            chatOptions.Patch.Set("$.thinking.type"u8, "disabled");
        }
#pragma warning restore SCME0001

        try
        {
            var result = await chatClient.CompleteChatAsync(messages, chatOptions, ct);
            return LlmCallResultFactory.FromChatCompletion(result.Value, model);
        }
        catch (ClientResultException ex)
        {
            throw LlmException.FromComplete("DeepSeek", ex);
        }
    }
}
