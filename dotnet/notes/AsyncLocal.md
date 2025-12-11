# AsyncLocal\<T\> 정리

## 개요

`AsyncLocal<T>`는 **비동기 실행 흐름을 따라 값이 유지되는** 저장소입니다. .NET 4.6에서 도입되었으며, `System.Threading` 네임스페이스에 있습니다.

```csharp
private static readonly AsyncLocal<string> _value = new();
```

---

## 핵심 특성

### 1. 논리적 실행 컨텍스트 단위

스레드가 아닌 **ExecutionContext** 단위로 값이 관리됩니다.

```csharp
private static readonly AsyncLocal<string> _context = new();

async Task Example()
{
    _context.Value = "설정됨";

    await Task.Delay(100);  // 스레드가 바뀔 수 있음

    Console.WriteLine(_context.Value);  // "설정됨" ✅
}
```

### 2. 요청별 격리

ASP.NET Core에서 각 HTTP 요청은 독립된 ExecutionContext를 가집니다.

```
요청 A → ExecutionContext A → AsyncLocal.Value = "A"
요청 B → ExecutionContext B → AsyncLocal.Value = "B"
        ↑ 서로 간섭 없음
```

### 3. Copy-on-Write (자식 태스크)

자식 태스크로 값이 **복사**됩니다. 자식의 변경은 부모에 영향을 주지 않습니다.

```csharp
_context.Value = "부모";

await Task.Run(() =>
{
    Console.WriteLine(_context.Value);  // "부모" (복사됨)
    _context.Value = "자식";
    Console.WriteLine(_context.Value);  // "자식"
});

Console.WriteLine(_context.Value);  // "부모" (변경 안됨)
```

---

## 값 변경 감지

```csharp
private static readonly AsyncLocal<string> _context = new(OnValueChanged);

private static void OnValueChanged(AsyncLocalValueChangedArgs<string> args)
{
    Console.WriteLine($"{args.PreviousValue} → {args.CurrentValue}");
    Console.WriteLine($"컨텍스트 전환: {args.ThreadContextChanged}");
}
```

---

## 실무 패턴

### 요청 컨텍스트

```csharp
public static class RequestContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();
    private static readonly AsyncLocal<string?> _userId = new();

    public static string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }

    public static string? UserId
    {
        get => _userId.Value;
        set => _userId.Value = value;
    }
}

// 미들웨어에서 설정
public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        RequestContext.CorrelationId = context.TraceIdentifier;
        RequestContext.UserId = context.User?.Identity?.Name;

        await _next(context);
    }
}
```

### 스코프 패턴

```csharp
public class OperationScope : IDisposable
{
    private static readonly AsyncLocal<OperationScope?> _current = new();
    private readonly OperationScope? _parent;

    public static OperationScope? Current => _current.Value;
    public string Name { get; }

    public OperationScope(string name)
    {
        _parent = _current.Value;
        Name = name;
        _current.Value = this;
    }

    public void Dispose() => _current.Value = _parent;
}

// 사용
using (new OperationScope("Outer"))
{
    using (new OperationScope("Inner"))
    {
        Console.WriteLine(OperationScope.Current?.Name);  // "Inner"
    }
    Console.WriteLine(OperationScope.Current?.Name);  // "Outer"
}
```

---

## 주의사항

| 상황                              | 결과                                |
| --------------------------------- | ----------------------------------- |
| `await` 이후                      | ✅ 유지됨                           |
| `Task.Run`                        | ✅ 복사되어 유지됨                  |
| `new Thread()`                    | ❌ 전파 안됨                        |
| `ExecutionContext.SuppressFlow()` | ❌ 전파 안됨                        |
| `ConfigureAwait(false)`           | ✅ 유지됨 (ExecutionContext는 별개) |

```csharp
// ❌ 새 Thread는 전파 안됨
new Thread(() => Console.WriteLine(_context.Value)).Start();  // null

// ❌ SuppressFlow 사용 시
using (ExecutionContext.SuppressFlow())
{
    await Task.Run(() => Console.WriteLine(_context.Value));  // null
}
```

---

## ThreadLocal과 비교

|             | ThreadLocal\<T\> | AsyncLocal\<T\>  |
| ----------- | ---------------- | ---------------- |
| 범위        | 스레드           | ExecutionContext |
| async/await | ❌               | ✅               |
| 용도        | 동기 코드        | 비동기 코드      |

---

## 요약

- **비동기 코드에서 컨텍스트 유지**가 필요하면 `AsyncLocal<T>` 사용
- **요청별로 자동 격리**되므로 웹 애플리케이션에서 안전
- **자식 태스크에 복사**되며 자식의 변경은 부모에 영향 없음
- `new Thread()`나 `SuppressFlow()` 사용 시에는 전파되지 않음
