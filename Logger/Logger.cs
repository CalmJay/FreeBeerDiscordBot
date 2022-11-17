using Discord;
using System.Threading.Tasks;
using static System.Guid;

namespace DiscordbotLogging.Log
{
    public abstract class Logger : ILogger
    {
        public string _guid;
        public Logger()
        {
            // extra data to show individual logger instances
            _guid = NewGuid().ToString()[^4..];
        }

        public abstract Task Log(LogMessage message);
    }
}