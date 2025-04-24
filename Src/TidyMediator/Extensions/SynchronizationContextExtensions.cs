using System;
using System.Threading;
using System.Threading.Tasks;

namespace TidyMediator.Extensions
{
    // Derived from: https://devblogs.microsoft.com/pfxteam/implementing-a-synchronizationcontext-sendasync-method/
    //               Stephen Toub

    public static class SynchronizationContextExtensions
    {
        public static Task SendAsync(
            this SynchronizationContext context, SendOrPostCallback d, object state)
        {
            var tcs = new TaskCompletionSource<bool>();
            context.Post(delegate {
                try
                {
                    d(state);
                    tcs.SetResult(true);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }, null);
            return tcs.Task;
        }
    }
}
