using Discord;
using System.Threading.Tasks;

namespace DNet_V3_Tutorial.Log
{
    public interface ILogger
    {
        // Establish required method for all Loggers to implement
        public Task Log(LogMessage message);
    }
}
