public class KeyedTaskQueue
{
    private readonly ConcurrentDictionary<string, Channel<Func<Task>>> _channels = new();

    public async Task<T> EnqueueAsync<T>(string key, Func<Task<T>> work)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        var wrapper = new Func<Task>(async () =>
        {
            try
            {
                var result = await work();
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        var channel = _channels.GetOrAdd(key, _ =>
        {
            var ch = Channel.CreateUnbounded<Func<Task>>();
            Task.Run(() => Consume(key, ch));
            return ch;
        });

        await channel.Writer.WriteAsync(wrapper);

        return await tcs.Task;
    }

    private async Task Consume(string key, Channel<Func<Task>> channel)
    {
        try
        {
            await foreach (var work in channel.Reader.ReadAllAsync())
            {
                try { await work(); }
                catch { /* 로그가 필요할 경우 작성 */ }
            }
        }
        finally
        {
            _channels.TryRemove(key, out _);
        }
    }
}