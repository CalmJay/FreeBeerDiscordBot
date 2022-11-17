using Discord;
using System.Threading.Tasks;

namespace DiscordbotLogging.Log
{
    public interface ILogger
    {
        // Establish required method for all Loggers to implement
        public Task Log(LogMessage message);
    }
}
