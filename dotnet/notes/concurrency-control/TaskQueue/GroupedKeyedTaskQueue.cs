public enum OverlapMode
{
    None,
    ExactMatchWithSubset
}

public class KeyedTaskQueue
{
    private readonly ConcurrentDictionary<string, Channel<Func<Task>>> _groupChannels = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _groupKeyMap = new();
    private readonly object _groupAssignmentLock = new();

    public async Task<T> EnqueueAsync<T>(
        Dictionary<string, string> keyObject,
        Func<Task<T>> work,
        OverlapMode overlapMode = OverlapMode.ExactMatchWithSubset)
    {
        string effectiveKey = GenerateKey(keyObject);
        string? selectedGroupKey = null;

        // 그룹 탐색 + 생성에 lock 적용
        lock (_groupAssignmentLock)
        {
            if (overlapMode == OverlapMode.ExactMatchWithSubset)
            {
                var matchedGroup = _groupKeyMap.FirstOrDefault(kvp =>
                    kvp.Value.Keys.Any(existingKey => IsOverlapping(ParseKey(existingKey), keyObject)));

                if (!string.IsNullOrEmpty(matchedGroup.Key))
                    selectedGroupKey = matchedGroup.Key;
            }

            if (string.IsNullOrEmpty(selectedGroupKey))
            {
                selectedGroupKey = Guid.NewGuid().ToString();
                _groupKeyMap[selectedGroupKey] = new ConcurrentDictionary<string, int>();
                _groupChannels.GetOrAdd(selectedGroupKey, _ =>
                {
                    var ch = Channel.CreateUnbounded<Func<Task>>();
                    Task.Run(() => Consume(ch));
                    return ch;
                });
            }

            // 키 등록 및 카운트 증가
            if (_groupKeyMap.TryGetValue(selectedGroupKey, out var keyCountMap))
            {
                keyCountMap.AddOrUpdate(effectiveKey, 1, (_, count) => count + 1);
            }
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
            finally
            {
                if (_groupKeyMap.TryGetValue(selectedGroupKey, out var keySet))
                {
                    // 참조 카운트 감소
                    if (keySet.AddOrUpdate(effectiveKey, 0, (_, count) => Math.Max(0, count - 1)) == 0)
                        keySet.TryRemove(effectiveKey, out _);

                    // 그룹 비었으면 제거
                    if (keySet.IsEmpty)
                    {
                        _groupKeyMap.TryRemove(selectedGroupKey, out _);

                        if (_groupChannels.TryRemove(selectedGroupKey, out var ch))
                        {
                            ch.Writer.Complete(); // 채널 종료
                        }
                    }
                }
            }
        });

        await _groupChannels[selectedGroupKey].Writer.WriteAsync(wrapper);
        return await tcs.Task;
    }

    private async Task Consume(Channel<Func<Task>> channel)
    {
        var reader = channel.Reader;

        await foreach (var work in reader.ReadAllAsync())
        {
            try { await work(); } catch { /* Optional logging */ }
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
