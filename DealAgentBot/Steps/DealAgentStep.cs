using DealAgentBot.Events;
using DealAgentBot.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json.Schema.Generation;
using OpenAI.Chat;
using System.ComponentModel;
using System.Text.Json;

namespace DealAgentBot.Steps
{
    public class DealAgentStep : KernelProcessStep
    {
        public const string AgentServiceKey = $"{nameof(DealAgentStep)}:{nameof(AgentServiceKey)}";
        public const string ReducerServiceKey = $"{nameof(DealAgentStep)}:{nameof(ReducerServiceKey)}";

        public static class Functions
        {
            public const string InvokeAgent = nameof(InvokeAgent);
            public const string InvokeGroup = nameof(InvokeGroup);
            public const string ReceiveResponse = nameof(ReceiveResponse);
        }

        [KernelFunction(Functions.InvokeAgent)]
        public async Task InvokeAgentAsync(KernelProcessStepContext context, Kernel kernel, string userInput, ILogger logger)
        {
            // Get the chat history
            IChatHistoryProvider historyProvider = kernel.GetHistory();
            ChatHistory history = await historyProvider.GetHistoryAsync();

            // Add the user input to the chat history
            history.Add(new Microsoft.SemanticKernel.ChatMessageContent(AuthorRole.User, userInput));

            // Obtain the agent response
            ChatCompletionAgent agent = kernel.GetAgent<ChatCompletionAgent>(AgentServiceKey);
            await foreach (Microsoft.SemanticKernel.ChatMessageContent message in agent.InvokeAsync(history))
            {
                // Capture each response
                history.Add(message);

                // Emit event for each agent response
                //await context.EmitEventAsync(new() { Id = AgentOrchestrationEvents.AgentResponse, Data = message });
                HelperFuncs.PrintMessage(message.ToString(), "DealAgent");
                //Console.ForegroundColor = ConsoleColor.Blue;
                //Console.WriteLine($"DealAgent: {message} " );
                //Console.ResetColor();
            }

            // Commit any changes to the chat history
            await historyProvider.CommitAsync();

            // Evaluate current intent
            IntentResult intent = await IsRequestingUserInputAsync(kernel, history, logger);

            string intentEventId =
                intent.IsRequestingUserInput ?
                    AgentOrchestrationEvents.AgentResponded :
                    intent.IsWorking ?
                        AgentOrchestrationEvents.AgentWorking :
                        CommonEvents.UserInputComplete;

            await context.EmitEventAsync(new() { Id = intentEventId });
        }

        [KernelFunction(Functions.InvokeGroup)]
        public async Task InvokeGroupAsync(KernelProcessStepContext context, Kernel kernel)
        {
            // Get the chat history
            IChatHistoryProvider historyProvider = kernel.GetHistory();
            ChatHistory history = await historyProvider.GetHistoryAsync();

            // Summarize the conversation with the user to use as input to the agent group
            string summary = await kernel.SummarizeHistoryAsync(ReducerServiceKey, history);

            await context.EmitEventAsync(new() { Id = AgentOrchestrationEvents.GroupInput, Data = summary });
        }

        [KernelFunction(Functions.ReceiveResponse)]
        public async Task ReceiveResponseAsync(KernelProcessStepContext context, Kernel kernel, string response)
        {
            // Get the chat history
            IChatHistoryProvider historyProvider = kernel.GetHistory();
            ChatHistory history = await historyProvider.GetHistoryAsync();

            // Proxy the inner response
            ChatCompletionAgent agent = kernel.GetAgent<ChatCompletionAgent>(AgentServiceKey);
            Microsoft.SemanticKernel.ChatMessageContent message = new(AuthorRole.Assistant, response) { AuthorName = agent.Name };
            history.Add(message);

            //await context.EmitEventAsync(new() { Id = AgentOrchestrationEvents.AgentResponse, Data = message });
            HelperFuncs.PrintMessage(message.ToString(), "DealAgent");

            await context.EmitEventAsync(new() { Id = AgentOrchestrationEvents.AgentResponded });
        }

        private static async Task<IntentResult> IsRequestingUserInputAsync(Kernel kernel, ChatHistory history, ILogger logger)
        {
            ChatHistory localHistory =
            [
                new Microsoft.SemanticKernel.ChatMessageContent(AuthorRole.System, "Analyze the conversation and determine if user input is being solicited."),
            .. history.TakeLast(1)
            ];

            IChatCompletionService service = kernel.GetRequiredService<IChatCompletionService>();

            Microsoft.SemanticKernel.ChatMessageContent response = await service.GetChatMessageContentAsync(localHistory, new OpenAIPromptExecutionSettings { ResponseFormat = s_intentResponseFormat });
            IntentResult intent = JsonSerializer.Deserialize<IntentResult>(response.ToString())!;

            logger.LogTrace("{StepName} Response Intent - {IsRequestingUserInput}: {Rationale}", nameof(DealAgentStep), intent.IsRequestingUserInput, intent.Rationale);

            return intent;
        }

        private static readonly ChatResponseFormat s_intentResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "intent_result",
            jsonSchema: BinaryData.FromString(new JSchemaGenerator().Generate(typeof(IntentResult)).ToString()),
            jsonSchemaIsStrict: true);

        [DisplayName("IntentResult")]
        [Description("this is the result description")]
        public sealed record IntentResult(
        [property:Description("True if user input is requested or solicited.  Addressing the user with no specific request is False.  Asking a question to the user is True.")]
        bool IsRequestingUserInput,
        [property:Description("True if the user request is being worked on.")]
        bool IsWorking,
        [property:Description("Rationale for the value assigned to IsRequestingUserInput")]
        string Rationale);
    }
}
