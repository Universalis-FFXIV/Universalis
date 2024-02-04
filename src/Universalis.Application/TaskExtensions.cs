using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Universalis.Application;

public static class TaskExtensions
{
    public static void FireAndForget(this Task task, ILogger log)
    {
        _ = FireAndForgetCore(task, log);
    }

    private static async Task FireAndForgetCore(Task task, ILogger log)
    {
        try
        {
            await task;
        }
        catch (Exception e)
        {
            log.LogError(e, "Exception thrown in dispatched task");
        }
    }
}