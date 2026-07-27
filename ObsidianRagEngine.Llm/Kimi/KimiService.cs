using ObsidianRagEngine.Llm.Common;
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
public sealed class KimiService(OpenAIClient openAIClient) : ILlmService
{
    public Task<string> Generate(string prompt, CancellationToken ct)
    {
        return CompleteChat([new UserChatMessage(prompt)], ct);
    }

    public Task<string> CompleteChat(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        KimiAiModel model = KimiAiModel.K2_6)
    {
        return CompleteChatCore(messages, ct, model, jsonMode: false);
    }

    public async Task<T> AskJson<T>(
        string question,
        CancellationToken ct,
        KimiAiModel model = KimiAiModel.K2_6)
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

    private async Task<string> CompleteChatCore(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        KimiAiModel model,
        bool jsonMode)
    {
        var chatClient = openAIClient.GetChatClient(model.ToApiModelId());

        var chatOptions = new ChatCompletionOptions { MaxOutputTokenCount = 20_000 };

        if (jsonMode)
            chatOptions.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();

        // K2_6 defaults to thinking on — disable explicitly. K2_7 / K3 always think (API defaults).
#pragma warning disable SCME0001 // JsonPatch is evaluation-only in System.ClientModel
        if (model == KimiAiModel.K2_6)
            chatOptions.Patch.Set("$.thinking.type"u8, "disabled");
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
