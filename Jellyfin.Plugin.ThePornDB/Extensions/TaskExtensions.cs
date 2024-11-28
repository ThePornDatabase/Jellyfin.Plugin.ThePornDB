using System;
using System.Threading.Tasks;

internal static class TaskExtensions
{
    public static async Task<bool> Timeout(this Task task, int timeoutMs)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));

        if (task.IsFaulted)
        {
            throw task.Exception.GetBaseException();
        }

        return completed == task && task.IsCompleted;
    }

    public static async Task<TResult> TimeoutWithResult<TResult>(this Task<TResult> task, int timeoutMs)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));

        if (task.IsFaulted)
        {
            throw task.Exception.GetBaseException();
        }

        if (completed == task && task.IsCompleted)
        {
            return task.Result;
        }

        throw new TimeoutException();
    }
}
