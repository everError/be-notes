## Code-First gRPC 규칙 정리

### 서비스 인터페이스

| 항목          | ✅ 가능                        | ❌ 불가능                    |
| ------------- | ------------------------------ | ---------------------------- |
| 반환 타입     | `Task<T>`, `ValueTask<T>`, `T` | `Task`, `ValueTask`, `void`  |
| 컬렉션 반환   | `Task<ListResponse>` (Wrapper) | `Task<List<T>>`, `Task<T[]>` |
| 스트리밍      | `IAsyncEnumerable<T>`          | `IEnumerable<T>`             |
| 파라미터      | 메시지 클래스                  | `string`, `int`, primitive   |
| 파라미터 개수 | 1개                            | 0개, 2개 이상                |

```csharp
[ServiceContract]
public interface IUserService
{
    // ✅ 가능
    Task<UserResponse> GetAsync(UserRequest request);
    ValueTask<UserResponse> GetAsync(UserRequest request);
    UserResponse Get(UserRequest request);  // 동기 (비추)
    IAsyncEnumerable<UserResponse> StreamAsync(UserQuery request);

    // ❌ 불가능
    Task<List<UserResponse>> GetAllAsync(EmptyRequest request);
    Task<string> GetNameAsync(UserRequest request);
    void Delete(UserRequest request);
    Task DeleteAsync(UserRequest request);
    Task<UserResponse> GetAsync(int id);
    Task<UserResponse> GetAsync(int id, string name);
    Task<UserResponse> GetAsync();
}
```

---

### 메시지 클래스

| 항목       | ✅ 가능                             | ❌ 불가능              |
| ---------- | ----------------------------------- | ---------------------- |
| 클래스     | `class`, `record`                   | `struct`               |
| 어트리뷰트 | `[ProtoContract]`, `[DataContract]` | 없음                   |
| 필드 번호  | 1 이상, 고유값                      | 0, 중복, 음수          |
| 생성자     | 기본 생성자 필수                    | 파라미터 있는 생성자만 |

```csharp
// ✅ 가능
[ProtoContract]
public class UserRequest
{
    [ProtoMember(1)]
    public int Id { get; set; }
}

[ProtoContract]
public record UserDto
{
    [ProtoMember(1)]
    public int Id { get; set; }
}

// ❌ 불가능
public class UserRequest  // 어트리뷰트 없음
{
    public int Id { get; set; }
}

[ProtoContract]
public class UserRequest(int id);  // 기본 생성자 없음
```

---

### 프로퍼티 타입

| ✅ 가능                                  | ❌ 불가능                          |
| ---------------------------------------- | ---------------------------------- |
| `int`, `long`, `float`, `double`, `bool` | `object`                           |
| `string`                                 | `dynamic`                          |
| `byte[]`                                 | `Stream`                           |
| `int?`, `string?` (Nullable)             |                                    |
| `DateTime`, `TimeSpan`                   | `DateTimeOffset`                   |
| `Guid`                                   |                                    |
| `enum` (0부터 시작)                      |                                    |
| `List<T>`, `T[]`                         | `IEnumerable<T>`, `ICollection<T>` |
| `Dictionary<K,V>`                        | `Hashtable`                        |
| 다른 `[ProtoContract]` 클래스            | 일반 클래스                        |

```csharp
[ProtoContract]
public class SampleDto
{
    // ✅ 가능
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string? Email { get; set; }
    [ProtoMember(4)] public DateTime CreatedAt { get; set; }
    [ProtoMember(5)] public List<string> Tags { get; set; } = [];
    [ProtoMember(6)] public AddressDto? Address { get; set; }
    [ProtoMember(7)] public UserStatus Status { get; set; }
    [ProtoMember(8)] public Dictionary<string, string> Meta { get; set; } = [];
    [ProtoMember(9)] public byte[] Data { get; set; } = [];
    [ProtoMember(10)] public Guid RefId { get; set; }

    // ❌ 불가능
    // public object Payload { get; set; }
    // public Stream File { get; set; }
    // public IEnumerable<string> Items { get; set; }
}

[ProtoContract]
public enum UserStatus
{
    Unknown = 0,  // 반드시 0부터
    Active = 1,
    Inactive = 2
}
```

---

### ProtoMember 규칙

| 규칙               | 설명                               |
| ------------------ | ---------------------------------- |
| 번호 시작          | 1부터                              |
| 번호 범위          | 1 ~ 536,870,911 (19000~19999 예약) |
| 중복               | ❌ 불가                            |
| 삭제된 번호 재사용 | ❌ 불가 (호환성)                   |
| 순서 건너뛰기      | ✅ 가능                            |

```csharp
[ProtoContract]
public class UserDto
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public string Name { get; set; }
    // [ProtoMember(3)] 삭제됨 - 재사용 금지
    [ProtoMember(4)] public string Email { get; set; }
    [ProtoMember(10)] public string Note { get; set; }  // 건너뛰기 OK
}
```

---

### 공통 패턴

```csharp
// 빈 요청/응답
[ProtoContract]
public class EmptyRequest { }

[ProtoContract]
public class EmptyResponse { }

// 컬렉션 Wrapper
[ProtoContract]
public class UserListResponse
{
    [ProtoMember(1)]
    public List<UserDto> Items { get; set; } = [];

    [ProtoMember(2)]
    public int TotalCount { get; set; }
}

// 단일 값 Wrapper
[ProtoContract]
public class StringResponse
{
    [ProtoMember(1)]
    public string Value { get; set; } = "";
}

[ProtoContract]
public class IntResponse
{
    [ProtoMember(1)]
    public int Value { get; set; }
}
```

---

### 전체 예시

```csharp
// ========== 메시지 ==========
[ProtoContract]
public class EmptyRequest { }

[ProtoContract]
public class EmptyResponse { }

[ProtoContract]
public class UserRequest
{
    [ProtoMember(1)]
    public int Id { get; set; }
}

[ProtoContract]
public class UserResponse
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = "";

    [ProtoMember(3)]
    public UserStatus Status { get; set; }
}

[ProtoContract]
public class UserListResponse
{
    [ProtoMember(1)]
    public List<UserResponse> Items { get; set; } = [];
}

[ProtoContract]
public enum UserStatus
{
    Unknown = 0,
    Active = 1,
    Inactive = 2
}

// ========== 서비스 ==========
[ServiceContract]
public interface IUserService
{
    Task<UserResponse> GetAsync(UserRequest request);
    Task<UserListResponse> GetAllAsync(EmptyRequest request);
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task<EmptyResponse> DeleteAsync(UserRequest request);
    IAsyncEnumerable<UserResponse> StreamAsync(EmptyRequest request);
}
```
