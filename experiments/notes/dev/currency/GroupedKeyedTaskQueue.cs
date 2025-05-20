public class KeyedTaskQueue
{
    private readonly ConcurrentDictionary<string, Channel<Func<Task>>> _groupChannels = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _groupKeyMap = new();

    public async Task<T> EnqueueAsync<T>(
        Dictionary<string, string> keyObject,
        Func<Task<T>> work,
        OverlapMode overlapMode = OverlapMode.ExactMatchWithSubset)
    {
        string effectiveKey = GenerateKey(keyObject);
        string? selectedGroupKey = null;

        if (overlapMode == OverlapMode.ExactMatchWithSubset)
        {
            // 이미 존재하는 그룹 중 겹치는 키가 있다면 해당 그룹에 포함시킴
            var matchedGroup = _groupKeyMap.FirstOrDefault(kvp =>
                kvp.Value.Any(existingKey => IsOverlapping(ParseKey(existingKey), keyObject)));

            if (!string.IsNullOrEmpty(matchedGroup.Key))
            {
                selectedGroupKey = matchedGroup.Key;
            }
        }

        // 겹치는 그룹이 없으면 새로운 그룹 생성
        if (string.IsNullOrEmpty(selectedGroupKey))
        {
            selectedGroupKey = Guid.NewGuid().ToString();
        }

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

        var channel = _groupChannels.GetOrAdd(selectedGroupKey, _ =>
        {
            var ch = Channel.CreateUnbounded<Func<Task>>();

            Task.Run(() => Consume(selectedGroupKey, ch));

            _groupKeyMap[selectedGroupKey] = new ConcurrentBag<string> { effectiveKey };
            return ch;
        });

        if (_groupKeyMap.TryGetValue(selectedGroupKey, out var keySet))
        {
            keySet.Add(effectiveKey);
        }

        await channel.Writer.WriteAsync(wrapper);

        return await tcs.Task;
    }

    private async Task Consume(string groupKey, Channel<Func<Task>> channel)
    {
        try
        {
            await foreach (var work in channel.Reader.ReadAllAsync())
            {
                try { await work(); } catch { /* Optional logging */ }
            }
        }
        finally
        {
            _groupChannels.TryRemove(groupKey, out _);
            _groupKeyMap.Remove(groupKey, out _);
        }
    }

    private static string GenerateKey(Dictionary<string, string> keyObj)
    {
        return string.Join("|", keyObj.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));
    }

    private static Dictionary<string, string> ParseKey(string key)
    {
        return key.Split('|')
                  .Select(part => part.Split('='))
                  .Where(parts => parts.Length == 2)
                  .ToDictionary(parts => parts[0], parts => parts[1]);
    }

    private static bool IsOverlapping(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        return a.Any(kv => b.TryGetValue(kv.Key, out var val) && val == kv.Value);
    }
}
