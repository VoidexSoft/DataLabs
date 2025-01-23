using System;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    public interface ILogger
    {
        bool HasErrors { get; }
        void LogError(string error);
        void LogError(Exception error);
        void LogWarning(string warning);
    }
}