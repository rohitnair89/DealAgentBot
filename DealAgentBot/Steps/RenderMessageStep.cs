﻿using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace DealAgentBot.Steps
{
    /// <summary>
    /// Displays output to the user.  While in this case it is just writing to the console,
    /// in a real-world scenario this would be a more sophisticated rendering system.  Isolating this
    /// rendering logic from the internal logic of other process steps simplifies responsibility contract
    /// and simplifies testing and state management.
    /// </summary>
    public class RenderMessageStep : KernelProcessStep
    {
        public static class Functions
        {
            public const string RenderDone = nameof(RenderMessageStep.RenderDone);
            public const string RenderError = nameof(RenderMessageStep.RenderError);
            public const string RenderInnerMessage = nameof(RenderMessageStep.RenderInnerMessage);
            public const string RenderMessage = nameof(RenderMessageStep.RenderMessage);
            public const string RenderUserText = nameof(RenderMessageStep.RenderUserText);
            public const string RenderAssistantText = nameof(RenderMessageStep.RenderAssisstantText);
        }

        private readonly static Stopwatch s_timer = Stopwatch.StartNew();

        /// <summary>
        /// Render an explicit message to indicate the process has completed in the expected state.
        /// </summary>
        /// <remarks>
        /// If this message isn't rendered, the process is considered to have failed.
        /// </remarks>
        [KernelFunction]
        public void RenderDone()
        {
            Render("DONE!");
        }

        /// <summary>
        /// Render exception
        /// </summary>
        [KernelFunction]
        public void RenderError(KernelProcessError error, ILogger logger)
        {
            string message = string.IsNullOrWhiteSpace(error.Message) ? "Unexpected failure" : error.Message;
            Render($"ERROR: {message} [{error.GetType().Name}]{Environment.NewLine}{error.StackTrace}");
            logger.LogError("Unexpected failure: {ErrorMessage} [{ErrorType}]", error.Message, error.Type);
        }

        /// <summary>
        /// Render user input
        /// </summary>
        [KernelFunction]
        public void RenderUserText(string message)
        {
            Render($"{AuthorRole.User.Label.ToUpperInvariant()}: {message}");
        }

        /// <summary>
        /// Render assistant text
        /// </summary>
        [KernelFunction]
        public void RenderAssisstantText(string message)
        {
            Render($"{AuthorRole.Assistant.Label.ToUpperInvariant()}: {message}");
        }

        /// <summary>
        /// Render an assistant message from the primary chat
        /// </summary>
        [KernelFunction]
        public void RenderMessage(ChatMessageContent message)
        {
            Render(message);
        }

        /// <summary>
        /// Render an assistant message from the inner chat
        /// </summary>
        [KernelFunction]
        public void RenderInnerMessage(ChatMessageContent message)
        {
            Render(message, indent: true);
        }

        public static void Render(ChatMessageContent message, bool indent = false)
        {
            string displayName = !string.IsNullOrWhiteSpace(message.AuthorName) ? $" - {message.AuthorName}" : string.Empty;
            Render($"{(indent ? "\t" : string.Empty)}{message.Role.Label.ToUpperInvariant()}{displayName}: {message.Content}");
        }

        public static void Render(string message)
        {
            Console.WriteLine(message);
        }
    }
}
