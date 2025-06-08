using System;
using Logger = LabApi.Features.Console.Logger;

namespace AdvancedMERTools;

/// <summary>
/// I might be doing something wrong but it seems like LabAPI doesn't have a way to automatically
/// infer whether a debug log should be printed based on Config.Debug, rather it seems you have
/// to pass a bool to every Debug() call.
/// So this class automates it.
/// </summary>
public static class AutoDebugLogger
{
    public static void Raw(string message, ConsoleColor color) => Logger.Raw(message, color);
    public static void Debug(object message) => Logger.Debug(message, canBePrinted: AdvancedMERTools.Singleton.Config.Debug);
    public static void Info(object message) => Logger.Info(message);
    public static void Warn(object message) => Logger.Warn(message);
    public static void Error(object message) => Logger.Error(message);
}
