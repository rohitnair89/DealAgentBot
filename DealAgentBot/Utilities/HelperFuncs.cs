using Microsoft.SemanticKernel;
using System;

namespace DealAgentBot.Utilities
{
    static class HelperFuncs
    {
        public static void PrintMessage(string message, string author)
        {
            // Set the color based on the author name
            switch (author.ToLower())
            {
                case "user":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "dealagent":
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            Console.WriteLine($"\n{author}: {message}\n");
            Console.ResetColor();
        }

        public static void PrintGroupMessage(ChatMessageContent message, bool indent)
        {

            string displayName = !string.IsNullOrWhiteSpace(message.AuthorName) ? $" - {message.AuthorName}" : string.Empty;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{(indent ? "\t" : string.Empty)}{message.Role.Label.ToUpperInvariant()}{displayName}: {message.Content}");
            Console.ResetColor();
        }

        public static string GetNextUserMessage()
        {
            var userMsg = string.Empty;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("User: ");
            userMsg = Console.ReadLine();
            Console.ResetColor();
            return userMsg;
        }
    }
}
