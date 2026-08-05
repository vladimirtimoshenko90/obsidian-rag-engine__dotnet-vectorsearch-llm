using ObsidianRagEngine.Contracts;
using ObsidianRagEngine.Llm.Exceptions;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace ObsidianRagEngine.Llm.OpenRouter;

/// <summary>
/// Thin OpenRouter chat client (OpenAI-compatible). Uses default provider routing;
/// cost is read from <c>usage.cost</c> in the response.
/// </summary>
public sealed class OpenRouterService(OpenAIClient openAIClient, OpenRouterAiModel model) : ILlmProvider
{
    public string ModelName => $"openrouter__{model.ToApiModelId()}";

    public Task<LlmCallResult> Complete(string prompt, CancellationToken ct) =>
        CompleteChatCore([new UserChatMessage(prompt)], ct, thinkingMode: false);

    public Task<LlmCallResult> CompleteChat(IReadOnlyList<ChatMessage> messages, CancellationToken ct, bool thinkingMode = false) =>
        CompleteChatCore(messages, ct, thinkingMode);

    private async Task<LlmCallResult> CompleteChatCore(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct,
        bool thinkingMode)
    {
        var chatClient = openAIClient.GetChatClient(model.ToApiModelId());
        var chatOptions = new ChatCompletionOptions { MaxOutputTokenCount = LlmDefaults.MaxOutputTokenCount };

        // OpenRouter reasoning is not first-class on the OpenAI SDK; set via JsonPatch.
        // Per-model contracts from OpenRouter (reasoning.mandatory / supported_efforts):
        // - Claude Fable 5: mandatory — cannot disable; map off → lowest effort.
        // - Claude Opus/Sonnet 5: toggleable, but no "none" effort — use enabled.
        // - GPT-5.6 *: supports effort "none".
        // - DeepSeek V4 *: only xhigh/high efforts — use enabled to turn off.
#pragma warning disable SCME0001 // JsonPatch is evaluation-only in System.ClientModel
        switch (model)
        {
            case OpenRouterAiModel.ClaudeFable5:
                chatOptions.Patch.Set("$.reasoning.effort"u8, thinkingMode ? "max" : "low");
                break;

            case OpenRouterAiModel.ClaudeOpus5:
            case OpenRouterAiModel.ClaudeSonnet5:
                if (thinkingMode)
                    chatOptions.Patch.Set("$.reasoning.effort"u8, "max");
                else
                    chatOptions.Patch.Set("$.reasoning.enabled"u8, false);
                break;

            case OpenRouterAiModel.Gpt56Sol:
            case OpenRouterAiModel.Gpt56Terra:
            case OpenRouterAiModel.Gpt56Luna:
                chatOptions.Patch.Set("$.reasoning.effort"u8, thinkingMode ? "max" : "none");
                break;

            case OpenRouterAiModel.DeepSeekV4Pro:
            case OpenRouterAiModel.DeepSeekV4Flash:
                if (thinkingMode)
                    chatOptions.Patch.Set("$.reasoning.effort"u8, "xhigh");
                else
                    chatOptions.Patch.Set("$.reasoning.enabled"u8, false);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(model), model, "Unknown OpenRouter AI model.");
        }
#pragma warning restore SCME0001

        try
        {
            var result = await chatClient.CompleteChatAsync(messages, chatOptions, ct);
            return LlmCallResultFactory.FromOpenRouterChatCompletion(result.Value);
        }
        catch (ClientResultException ex)
        {
            throw LlmException.FromComplete("OpenRouter", ex);
        }
    }
}
