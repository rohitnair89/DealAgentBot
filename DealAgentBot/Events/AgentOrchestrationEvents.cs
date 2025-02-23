﻿namespace DealAgentBot.Events
{
    public static class AgentOrchestrationEvents
    {
        public static readonly string StartProcess = nameof(StartProcess);

        public static readonly string AgentResponse = nameof(AgentResponse);
        public static readonly string AgentResponded = nameof(AgentResponded);
        public static readonly string AgentWorking = nameof(AgentWorking);
        public static readonly string GroupInput = nameof(GroupInput);
        public static readonly string GroupMessage = nameof(GroupMessage);
        public static readonly string GroupCompleted = nameof(GroupCompleted);
    }
}
