# .NET의 System.Threading.Channels 개념 및 활용 정리

## 📌 개요

`System.Threading.Channels`는 .NET에서 제공하는 고성능 비동기 메시징 라이브러리입니다. **생산자-소비자(Producer-Consumer)** 패턴을 구현할 때, 안전하고 효율적인 큐잉 처리를 지원합니다.

비동기 스트림(`IAsyncEnumerable`), 백그라운드 작업(`BackgroundService`) 등과 함께 사용하면, **비동기 작업 직렬화**, **작업 오프로드**, **순차 처리**, **병렬 분산 처리** 등 다양한 시나리오를 구현할 수 있습니다.

---

## 🧱 Channel 기본 구성 요소

| 구성요소                       | 설명                                                   |
| ------------------------------ | ------------------------------------------------------ |
| `Channel<T>`                   | 채널의 기본 형식, `Reader`와 `Writer`로 나뉨           |
| `ChannelWriter<T>`             | 생산자 측 API (`TryWrite`, `WriteAsync` 등)            |
| `ChannelReader<T>`             | 소비자 측 API (`ReadAsync`, `ReadAllAsync`, `TryRead`) |
| `Channel<T>.CreateUnbounded()` | 크기 제한 없는 채널 생성                               |
| `Channel<T>.CreateBounded()`   | 크기 제한된 채널 생성, 배압(Backpressure) 처리 가능    |

---

## 🔍 Channel 동작 방식 및 내부 구조

- `Channel<T>`는 생산자-소비자 패턴을 lock 없이 구현하기 위해 **lock-free queue** 기반으로 동작합니다.
- 내부적으로는 `AsyncOperation<T>` 기반으로 awaitable한 작업 대기열을 만들고, Reader가 없으면 쓰기가 대기하거나 거절될 수 있습니다.
- `Writer.Complete(Exception?)`를 호출하면 채널을 종료할 수 있고, 이후 Reader는 `Completion` Task를 통해 채널 종료 여부를 감지할 수 있습니다.
- `TryWrite`는 즉시 실패 여부를 반환하고, `WriteAsync`는 공간이 날 때까지 비동기로 대기합니다.

---

## 🛠️ BoundedChannelOptions 상세 설명

```csharp
var options = new BoundedChannelOptions(capacity: 100)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = false,
    SingleWriter = true
};
```

| 옵션           | 설명                                                                    |
| -------------- | ----------------------------------------------------------------------- |
| `Capacity`     | 채널에 저장할 수 있는 최대 항목 수                                      |
| `FullMode`     | 채널이 가득 찼을 때 처리 방식 (Wait, DropOldest, DropNewest, DropWrite) |
| `SingleReader` | 단일 소비자일 경우 true로 설정 시 성능 최적화 가능                      |
| `SingleWriter` | 단일 생산자일 경우 true로 설정 시 성능 최적화 가능                      |

---

## 🚦 사용 흐름

### 1. 채널 생성

```csharp
var channel = Channel.CreateUnbounded<MyMessage>();
```

또는

```csharp
var channel = Channel.CreateBounded<MyMessage>(options);
```

### 2. 생산자 (Writer)

```csharp
await channel.Writer.WriteAsync(new MyMessage { ... });
```

또는

```csharp
if (!channel.Writer.TryWrite(message))
{
    // 큐가 가득 찼거나 닫혔을 때의 예외 처리
}
```

### 3. 소비자 (Reader)

```csharp
await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
{
    await HandleMessageAsync(message);
}
```

---

## 🔄 Channel + BackgroundService 패턴

```csharp
public class MyBackgroundService : BackgroundService
{
    private readonly Channel<MyMessage> _channel;

    public MyBackgroundService(Channel<MyMessage> channel)
    {
        _channel = channel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessMessageAsync(message);
            }
            catch (Exception ex)
            {
                // 로깅 및 오류 복구
            }
        }
    }
}
```

---

## ✅ 응답 가능한 구조 (`TaskCompletionSource` 활용)

```csharp
public class UpsertRequest
{
    public string Key { get; set; }
    public int Increment { get; set; }
    public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

// Controller에서 요청
var request = new UpsertRequest { Key = "A", Increment = 1 };
await channel.Writer.WriteAsync(request);
var result = await request.Completion.Task; // 응답 대기
```

BackgroundService 쪽에서 작업 처리 후:

```csharp
await ProcessUpsert(request);
request.Completion.SetResult(true); // 응답 반환
```

---

## 🚀 고급 활용 예시

### 🔹 Task Dispatcher / 병렬 분산 처리

- Channel 여러 개 생성하여 Task를 Key 기반으로 라우팅 (Consistent Hashing)

### 🔹 주기적 집계 (Batching)

- Timer + Channel 조합으로 일정 시간마다 Channel에서 묶어서 처리 (예: 100개씩 or 1초마다 Flush)

### 🔹 Throttling / Rate Limiter

- Channel이 가득 찼을 때 Drop 또는 대기
- Queue 길이 감시로 동적 스로틀링 구현 가능

### 🔹 필터링, 조건 기반 처리

- `ReadAllAsync()` 내부에서 `if` 조건 또는 `await foreach (var item in FilteredReader(channel.Reader))` 같은 구성 가능

---

## ⚠️ 주의 사항

- **채널이 가득 찼을 때 (Bounded)**: `TryWrite` 실패 → 재시도 혹은 대기 필요
- **예외 처리**: Reader 루프에서 예외 발생 시 루프가 종료되지 않도록 try-catch로 감싸야 함
- **메모리 관리**: 키 기반 채널을 많이 생성할 경우, 사용되지 않는 채널은 `Writer.Complete()` 후 GC 대상이 되도록 관리 필요
- **배압 처리**: 처리 속도보다 메시지 생산 속도가 빠르면 채널이 버퍼링을 과도하게 차지할 수 있음 → `BoundedChannelOptions` 사용 권장
- **완료 시그널**: `channel.Writer.Complete()` 호출 후, Reader는 `ReadAllAsync` 종료 가능. Completion Task로 알림 받기 가능

---

## 🔄 다른 대안과 비교

| 대안                       | 특징                                             |
| -------------------------- | ------------------------------------------------ |
| `BlockingCollection<T>`    | 고전적 방식. 동기적. 기본적으로 락 기반이라 느림 |
| `TPL Dataflow`             | 고급 메시지 흐름 처리 가능. 유연하지만 복잡함    |
| `Queue<T> + SemaphoreSlim` | Channel보다 저수준. 직접 락 및 동기화 구현 필요  |

---

## 🧠 활용 사례

| 시나리오           | 설명                                                            |
| ------------------ | --------------------------------------------------------------- |
| 대량 요청 오프로드 | HTTP 요청을 받고 실제 처리를 채널에서 처리하여 응답 지연 방지   |
| 키 단위 직렬 처리  | 특정 ID(Key) 기준으로 작업을 순차 처리하여 DB 충돌 회피         |
| Background Queue   | 비동기 로그 기록, 이벤트 처리, 이메일 발송 등 백그라운드로 수행 |
| 멀티 채널 분산     | 채널 여러 개를 두고 Round Robin 또는 Key 기반 분산 처리         |
| 실시간 데이터 수집 | IoT 센서 데이터 수집 후 Channel로 전달해 실시간 분석 수행       |
| API Rate Limiter   | 채널을 통해 유입 속도를 조절하며 처리량 제한                    |

---

## 🔚 요약

- `System.Threading.Channels`는 비동기 처리와 직렬화, 분산 처리를 매우 쉽게 만들어줍니다.
- Controller와 BackgroundService 간 연결, 요청-응답 시나리오 구현도 `TaskCompletionSource` 조합으로 가능
- 적절한 예외 처리, 채널 관리 전략과 함께 사용하면 MSA/고성능 서비스에도 활용 가능
- Channel은 .NET의 비동기/고성능 메시지 처리의 핵심 도구 중 하나로, 다양한 시나리오에 적용할 수 있습니다.
