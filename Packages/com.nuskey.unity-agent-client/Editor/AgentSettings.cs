using System.Collections.Generic;

namespace UnityAgentClient
{
    public class AgentSettings
    {
        public string Command { get; set; } = "";
        public string Arguments { get; set; } = "";
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
        public bool VerboseLogging { get; set; }
    }
}
