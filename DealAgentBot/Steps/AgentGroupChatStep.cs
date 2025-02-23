using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using DealAgentBot.Events;
using DealAgentBot.Utilities;
using Azure;

namespace DealAgentBot.Steps
{
    /// <summary>
    /// This steps defines actions for the group chat in which to agents collaborate in
    /// response to input from the primary agent.
    /// </summary>
    public class AgentGroupChatStep : KernelProcessStep
    {
        public const string ChatServiceKey = $"{nameof(AgentGroupChatStep)}:{nameof(ChatServiceKey)}";
        public const string ReducerServiceKey = $"{nameof(AgentGroupChatStep)}:{nameof(ReducerServiceKey)}";

        public static class Functions
        {
            public const string InvokeAgentGroup = nameof(InvokeAgentGroup);
        }

        [KernelFunction(Functions.InvokeAgentGroup)]
        public async Task InvokeAgentGroupAsync(KernelProcessStepContext context, Kernel kernel, string input)
        {
            AgentGroupChat chat = kernel.GetRequiredService<AgentGroupChat>();

            // Reset chat state from previous invocation
            await chat.ResetAsync();
            GlobalSettings.ResetFirstAgent = true;
            chat.IsComplete = false;

            ChatMessageContent message = new(AuthorRole.User, input);
            chat.AddChatMessage(message);
            HelperFuncs.PrintGroupMessage(message, true);
            await context.EmitEventAsync(new() { Id = AgentOrchestrationEvents.GroupMessage, Data = message });

            await foreach (ChatMessageContent response in chat.InvokeAsync())
            {
                GlobalSettings.ResetFirstAgent = false;
                HelperFuncs.PrintGroupMessage(response, true);
                await context.EmitEventAsync(new() { Id = AgentOrchestrationEvents.GroupMessage, Data = response });
            }

            ChatMessageContent[] history = await chat.GetChatMessagesAsync().Reverse().ToArrayAsync();

            // Summarize the group chat as a response to the primary agent
            string summary = await kernel.SummarizeHistoryAsync(ReducerServiceKey, history);

            await context.EmitEventAsync(new() { Id = AgentOrchestrationEvents.GroupCompleted, Data = summary });
        }
    }
}
