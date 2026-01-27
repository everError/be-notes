## ValueTask<T> 정리

### 1. Task<T>의 문제점

`Task<T>`는 class라서 **항상 힙에 할당**됩니다.

```csharp
public Task<User> GetUserAsync(int id)
{
    if (_cache.TryGetValue(id, out var user))
        return Task.FromResult(user);  // 동기인데도 힙 할당 발생

    return _db.FindAsync(id);
}
```

캐시 히트가 많으면 불필요한 힙 할당 → GC 부담 증가

### 2. ValueTask<T>의 해결

`ValueTask<T>`는 struct라서 **동기 완료 시 힙 할당이 없습니다.**

```csharp
public struct ValueTask<T>
{
    private readonly T _result;        // 동기 완료 시 사용
    private readonly Task<T>? _task;   // 비동기 완료 시 사용
}
```

```csharp
public ValueTask<User> GetUserAsync(int id)
{
    if (_cache.TryGetValue(id, out var user))
        return new ValueTask<User>(user);          // 스택에서 처리, 힙 할당 없음

    return new ValueTask<User>(_db.FindAsync(id)); // 비동기면 Task 감싸서 처리
}
```

### 3. 사용 기준

| 상황                  | 선택           | 이유                       |
| --------------------- | -------------- | -------------------------- |
| 항상 동기             | `T`            | 비동기 타입 필요 없음      |
| 항상 비동기           | `Task<T>`      | ValueTask 이점 없음        |
| 동기/비동기 분기 공존 | `ValueTask<T>` | 동기 분기에서 힙 할당 절약 |

### 4. 주의사항

`await` 키워드를 쓰면 내부적으로 Task가 생성되어 **ValueTask의 이점이 사라집니다.**

```csharp
// ❌ 의미 없음 - await 쓰면 어차피 힙 할당
public async ValueTask<User> GetUserAsync(int id)
{
    return await _db.Users.FindAsync(id);
}

// ✅ 올바른 사용 - await 없이 분기 처리
public ValueTask<User> GetUserAsync(int id)
{
    if (_cache.TryGetValue(id, out var user))
        return new ValueTask<User>(user);

    return new ValueTask<User>(_db.FindAsync(id));
}
```

### 5. 결론

- **대부분의 비즈니스 로직**: `Task<T>` 사용
- **고성능 + 동기/비동기 분기**: `ValueTask<T>` 고려
- **gRPC 서비스**: 둘 다 가능, 취향 차이
