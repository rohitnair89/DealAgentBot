﻿using Azure.Identity;
using DealAgentBot.Events;
using DealAgentBot.Plugins;
using DealAgentBot.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace DealAgentBot
{
    internal class Program
    {
        const string ContractAgentName = "ContractAgent";
        const string ApprovalAgentName = "ApprovalAgent";
        const string RiskAgentName = "RiskAgent";

        private const string DealAgentInstructions =
        """
        Capture information provided by the user for their contract related request.
        Request confirmation without suggesting additional details.
        Once confirmed inform them you're working on the request.
        Never provide a direct answer to the user's request.
        """;

        //private const string DealAgentInstructions =
        //"""
        //You are a deal agent responsible for assisting the user with contract details.
        //You delegate the request from the user to agents who are responsible to provide the appropriate information.
        //You receive responses from the specialized agents and relay it back to the user.
        //You can proceed with the instruction if you have a contract id. The contract id is always a number and never words.
        //You can also proceed with the instruction if the user has provided the name of the contract, and in this case you don't need to confirm the id of the contract with the user.
        //If the user requests for information of all contracts, proceed with the instruction.
        //Never provide a direct answer to the user's request.
        //""";

        //private const string DealAgentInstructions =
        //"""
        //Capture information from the user about contract details.
        //If the user requests for information of all contracts, proceed with the instruction.
        //If the user requests for a specific contract's details, proceed with the instruction when you have the Id or name of the contract.
        //If the user is asking for approval details of a contract, make sure you have the id of the contract before proceeding. The id of the contract will be a number and never words.
        //Never provide a direct answer to the user's request.
        //""";

        private const string ContractAgentInstructions =
        $$$"""
        You are a contract agent, responsible for providing contract details.
        - Provide contract details only if a contract ID or contract name is provided.  
        - If only a contract name is provided, first retrieve the contract details to obtain the contract ID before proceeding.  

        - If asked for approval details of a contract, respond with: "Get approval details for EntityType contract and EntityID x", where x is the contract ID.
        - If asked for risk details of a contract, respond with: "Get risk details for EntityType contract and EntityID y", where y is the contract ID.
        - If asked for entity type, respond with: "Please x the approval y for EntityType contract and EntityID z" where x is the action to be taken, y is the name of the approval and z is the id of the contract.
         
        If asked for entity id:
        - If you have the contract id, respond with: "Please x the approval y for EntityType contract and EntityID z" where x is the action to be taken, y is the name of the approval and z is the id of the contract.
        - If you do not have the contract id and have the contract name, first retrieve the contract details to obtain the contract ID and respond with: "Please x the approval y for EntityType contract and EntityID z" where x is the action to be taken, y is the name of the approval and z is the id of the contract.

        If you are requested to take action (possible actions are approve or reject only) on an approval:
         - Make sure you evaluate any conditions for the approval before proceeding, if any are provided in the request. Get necessary details to evaluate the conditions, if any.
         - Once you determine that an action can be taken, respond with "Please x the approval y for EntityType contract and EntityID z" where x is the action to be taken, y is the name of the approval and z is the id of the contract.

        Follow these instructions strictly and do not offer additional responses beyond what is requested.
        
        """;

        //private const string ContractAgentInstructions =
        //$$$"""
        //You are a contract agent responsible for providing contract details.
        //You can provide details of all contracts if explicitly instructed to do so.

        //If you are asked for contract details of a specific contract:
        //- Proceed only if you have the contract ID (which must be a number, never words).
        //- If given a contract name, first retrieve the contract details and then proceed.

        //If asked for approval details of a contract, respond with:
        //"Get approval details for EntityType contract and EntityID x", where x is the contract ID.

        //If asked for risk details of a contract:
        //"Get risk details for EntityType contract and EntityID y", where y is the contract ID.

        //Respond exactly as instructed. Do not provide additional information beyond the requested response."
        //""";

        //private const string ContractAgentInstructions =
        //"""
        //Evaluate contract details in response to the current direction.
        //Be as concise as possible.
        //If approval details are needed for the contract you will need to pass on necessary details to the ApprovalAgent
        //Just respond as per current direction and do not offer more assistance.
        //""";

        private const string ApprovalAgentInstructions =
        """
        You are an approval agent that is responsible for providing details of approvals as well as approving or recjecting the approvals for an entity type and entity id.
        If you do not have the entity name and entity id, you must ask for it.
        If there is a request for approval information, provide approval information in response to the current direction.
        If there is an instruction to approve or reject an approval, proceed only if you have the entity name, entity id and the name of the approval.
        Just respond as per current direction. Do not offer more assistance in an additional message.
        """;

        //private const string ApprovalAgentInstructions =
        //"""
        //If there is a request for approval information, provide approval information in response to the current direction.
        //If there is an instruction to approve or reject an approval, proceed only if you have the entity name, entity id and the name of the approval.
        //""";

        private const string RiskAgentInstructions =
        """
        Your sole responsibility is to only provide details of risks for an entity type and entity id when asked.
        You must ignore any other instruction if it is not related to risk details, and you can only respond about risk details.
        If you do not have the entity name and entity id, you must ask for it.
        Just respond as per current direction. Do not offer more assistance in an additional message.
        """;

        private const string DealSummaryInstructions =
        """
        Summarize the most recent user request in first person command form. Make sure you don't exclude any important instructions.
        """;

        private const string SuggestionSummaryInstructions =
        """
        Address the user directly with a summary of the response.
        """;

        static async Task Main(string[] args)
        {
            KernelProcess process = SetupAgentProcess<BasicAgentChatUserInput>("DealAgentDelegation");

            await RunProcessAsync(process);
        }

        private static async Task RunProcessAsync(KernelProcess process)
        {
            // Init services
            ChatHistory chatHistory = [];
            Kernel kernel = SetupKernel(chatHistory);

            // Execute process
            using var runningProcess = await process.StartAsync(
                kernel,
                    new KernelProcessEvent()
                    {
                        Id = AgentOrchestrationEvents.StartProcess,
                        Data = null
                    });

            //foreach (ChatMessageContent message in chatHistory)
            //{
            //    RenderMessageStep.Render(message);
            //}
        }

        private static void SetupGroupChat(IKernelBuilder builder, Kernel kernel)
        {
            
            ChatCompletionAgent contractAgent = CreateAgent(ContractAgentName, ContractAgentInstructions, kernel.Clone());
            contractAgent.Kernel.Plugins.AddFromType<ContractPlugin>();

            ChatCompletionAgent approvalAgent = CreateAgent(ApprovalAgentName, ApprovalAgentInstructions, kernel.Clone());
            approvalAgent.Kernel.Plugins.AddFromType<ApprovalPlugin>();

            ChatCompletionAgent riskAgent = CreateAgent(RiskAgentName, RiskAgentInstructions, kernel.Clone());
            riskAgent.Kernel.Plugins.AddFromType<RiskPlugin>();

            KernelFunction selectionFunction =
                AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                Determine which participant should take the next turn in the conversation based on the most recent participant's instruction.

                Respond with only the name of the selected participant.

                Choose only from these participants:
                - {{{RiskAgentName}}}
                - {{{ContractAgentName}}}
                - {{{ApprovalAgentName}}}
                
                Follow these rules strictly when selecting the next participant:
                - If 
                - If this is the first instruction, always respond with {{{ContractAgentName}}}.
                - If the instruction requests approval details, respond with {{{ApprovalAgentName}}}.
                - If the instruction requests risk details, respond with {{{RiskAgentName}}}.
                - If the instruction requests contract details, respond with {{{ContractAgentName}}}.
                - If the instruction is to approve or reject an approval, respond with {{{ApprovalAgentName}}}.
                - If the instruction requests entity type or entity id details, respond with {{{ContractAgentName}}}
                - After {{{ApprovalAgentName}}} responds, respond with {{{ContractAgentName}}}
                - After {{{RiskAgentName}}} responds, respond with {{{ContractAgentName}}}
                                
                History:
                {{$history}}
                """,
                    safeParameterNames: "history");

    //        KernelFunction selectionFunction =
    //AgentGroupChat.CreatePromptFunctionForStrategy(
    //    $$$"""
    //            Determine which participant takes the next turn in a conversation based on the the most recent participant's instruction.
    //            State only the name of the participant to take the next turn.
                
    //            Choose only from these participants:
    //            - {{{ContractAgentName}}}
    //            - {{{ApprovalAgentName}}}
    //            - {{{RiskAgentName}}}
                
    //            Always follow these rules when selecting the next participant:
    //            - After user input, it is always {{{ContractAgentName}}}'s turn
    //            - If {{{ContractAgentName}}} is requesting approval details, it is {{{ApprovalAgentName}}}'s turn
    //            - If {{{ContractAgentName}}} is requesting risk details, it is {{{RiskAgentName}}}'s turn
    //            - If {{{ApprovalAgentName}}} is requesting an entity type and entity id, it is {{{ContractAgentName}}}'s turn
                                
    //            History:
    //            {{$history}}
    //            """,
    //    safeParameterNames: "history");

            KernelFunction terminationFunction =
                AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                Evaluate if the user's most recent request has received a final response.

                If all of these conditions are met, respond with a single word: yes
                History:
                {{$history}}
                """,
                    safeParameterNames: "history");

            AgentGroupChat chat =
                new(contractAgent, approvalAgent, riskAgent)
                {
                    // NOTE: Replace logger when using outside of sample.
                    // Use `this.LoggerFactory` to observe logging output as part of sample.
                    LoggerFactory = NullLoggerFactory.Instance,
                    ExecutionSettings = new()
                    {
                        SelectionStrategy =
                            new KernelFunctionSelectionStrategy(selectionFunction, kernel)
                            {
                                InitialAgent = contractAgent,
                                HistoryVariableName = "history",
                                HistoryReducer = new ChatHistoryTruncationReducer(1),
                                ResultParser = (result) => {
                                    // Check if it's a new group chat
                                    //if (string.IsNullOrEmpty(result.GetValue<string>()))
                                    //{
                                    //    return ContractAgentName;
                                    //}

                                    return result.GetValue<string>() ?? ContractAgentName;
                                }
                                //Arguments = new KernelArguments(new PromptExecutionSettings { ExtensionData = new Dictionary<string, object> { { "history", "history" } } }),
                                //Arguments = new KernelArguments
                                //{
                                //    {"resetInitialAgent", true }
                                //}
                            },
                        TerminationStrategy =
                            new KernelFunctionTerminationStrategy(terminationFunction, kernel)
                            {
                                HistoryVariableName = "history",
                                MaximumIterations = 10,
                                HistoryReducer = new ChatHistoryTruncationReducer(5),
                                ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                            }
                    }
                };
            builder.Services.AddScoped(provider => chat);
        }

        private static ChatCompletionAgent CreateAgent(string agentName, string agentInstructions, Kernel kernel) =>
        new()
        {
            Name = agentName,
            Instructions = agentInstructions,
            Kernel = kernel.Clone(),
            Arguments =
                new KernelArguments(
                    new OpenAIPromptExecutionSettings
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                        Temperature = 0,
                    }),
        };

        private static Kernel SetupKernel(ChatHistory chatHistory)
        {
            #region Keep Out
            var modelId = "<<your-llm-name>>";
            var endpoint = "<<your-llm-endpoint>>";
            var cred = "<<your-llm-endpoint>>"; // or new DefaultAzureCredential();
            #endregion

            IKernelBuilder builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, cred);

            // Inject agents into service collection
            SetupAgents(builder, builder.Build());
            // Inject history provider into service collection
            builder.Services.AddSingleton<IChatHistoryProvider>(new ChatHistoryProvider(chatHistory));

            // Configure logging to console
            var loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.SetMinimumLevel(LogLevel.Information);
            });

            // NOTE: Uncomment to see process logging
            builder.Services.AddSingleton(loggerFactory);

            return builder.Build();
        }

        private static void SetupAgents(IKernelBuilder builder, Kernel kernel)
        {
            // Create and inject primary agent into service collection
            ChatCompletionAgent dealAgent = CreateAgent("DealAgent", DealAgentInstructions, builder.Build());
            builder.Services.AddKeyedSingleton(DealAgentStep.AgentServiceKey, dealAgent);

            // Create and inject group chat into service collection
            SetupGroupChat(builder, kernel);

            // Create and inject reducers into service collection
            builder.Services.AddKeyedSingleton(DealAgentStep.ReducerServiceKey, SetupReducer(kernel, DealSummaryInstructions));
            builder.Services.AddKeyedSingleton(AgentGroupChatStep.ReducerServiceKey, SetupReducer(kernel, SuggestionSummaryInstructions));
        }

        private static ChatHistorySummarizationReducer SetupReducer(Kernel kernel, string instructions) =>
         new(kernel.GetRequiredService<IChatCompletionService>(), 1)
         {
             SummarizationInstructions = instructions
         };

        private static KernelProcess SetupAgentProcess<TUserInputStep>(string processName) where TUserInputStep : UserInputStep
        {
            ProcessBuilder process = new(processName);

            var userInputStep = process.AddStepFromType<TUserInputStep>();
            var renderMessageStep = process.AddStepFromType<RenderMessageStep>();
            var dealAgentStep = process.AddStepFromType<DealAgentStep>();
            var agentGroupStep = process.AddStepFromType<AgentGroupChatStep>();

            AttachErrorStep(
                userInputStep,
                UserInputStep.Functions.GetUserInput);

            AttachErrorStep(
                dealAgentStep,
                DealAgentStep.Functions.InvokeAgent,
                DealAgentStep.Functions.InvokeGroup,
                DealAgentStep.Functions.ReceiveResponse);

            AttachErrorStep(
                agentGroupStep,
                AgentGroupChatStep.Functions.InvokeAgentGroup);

            // Entry point
            process.OnInputEvent(AgentOrchestrationEvents.StartProcess)
                .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep));

            // Pass user input to primary agent
            userInputStep
                .OnEvent(CommonEvents.UserInputReceived)
                .SendEventTo(new ProcessFunctionTargetBuilder(dealAgentStep, DealAgentStep.Functions.InvokeAgent));
                //.SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderUserText, parameterName: "message"));

            // Process completed
            userInputStep
                .OnEvent(CommonEvents.UserInputComplete)
                .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderDone))
                .StopProcess();

            // Render response from primary agent
            //dealAgentStep
            //    .OnEvent(AgentOrchestrationEvents.AgentResponse)
            //    .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderAssistantText, parameterName: "message"));

            // Request is complete
            dealAgentStep
                .OnEvent(CommonEvents.UserInputComplete)
                .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderDone))
                .StopProcess();

            // Request more user input
            dealAgentStep
                .OnEvent(AgentOrchestrationEvents.AgentResponded)
                .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep));

            // Delegate to inner agents
            dealAgentStep
                .OnEvent(AgentOrchestrationEvents.AgentWorking)
                .SendEventTo(new ProcessFunctionTargetBuilder(dealAgentStep, DealAgentStep.Functions.InvokeGroup));

            // Provide input to inner agents
            dealAgentStep
                .OnEvent(AgentOrchestrationEvents.GroupInput)
                .SendEventTo(new ProcessFunctionTargetBuilder(agentGroupStep, parameterName: "input"));

            // Render response from inner chat (for visibility)
            //agentGroupStep
            //    .OnEvent(AgentOrchestrationEvents.GroupMessage)
            //    .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderInnerMessage, parameterName: "message"));

            // Provide inner response to primary agent
            agentGroupStep
                .OnEvent(AgentOrchestrationEvents.GroupCompleted)
                .SendEventTo(new ProcessFunctionTargetBuilder(dealAgentStep, DealAgentStep.Functions.ReceiveResponse, parameterName: "response"));

            KernelProcess kernelProcess = process.Build();

            return kernelProcess;

            void AttachErrorStep(ProcessStepBuilder step, params string[] functionNames)
            {
                foreach (string functionName in functionNames)
                {
                    step
                        .OnFunctionError(functionName)
                        .SendEventTo(new ProcessFunctionTargetBuilder(renderMessageStep, RenderMessageStep.Functions.RenderError, "error"))
                        .StopProcess();
                }
            }
        }

        private sealed class BasicAgentChatUserInput : UserInputStep
        {
            public BasicAgentChatUserInput()
            {
                this.SuppressOutput = true;
            }

            public override void PopulateUserInputs(UserInputState state)
            {
                //state.UserInputs.Add(Console.ReadLine());
                //state.UserInputs.Add(Console.ReadLine());
                //state.UserInputs.Add(Console.ReadLine());
                //state.UserInputs.Add(Console.ReadLine());
                //state.UserInputs.Add(Console.ReadLine());
                //state.UserInputs.Add(Console.ReadLine());
                //state.UserInputs.Add(Console.ReadLine());
                //state.UserInputs.Add(Console.ReadLine());
                //state.UserInputs.Add("Hi");
                //state.UserInputs.Add("Get me contract details of contract with id 12121");
                //state.UserInputs.Add("Correct");
                //state.UserInputs.Add("That's all, thank you");
                //state.UserInputs.Add("When is an open time to go camping near home for 4 days after the end of this week?");
                //state.UserInputs.Add("Yes, and I'd prefer nice weather.");
                //state.UserInputs.Add("Sounds good, add the soonest option without conflicts to my calendar");
                //state.UserInputs.Add("Correct");
                //state.UserInputs.Add("That's all, thank you");
            }
        }
    }
}
