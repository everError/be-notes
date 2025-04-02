# Redlock란?

**Redlock**은 Redis의 창시자 Antirez(살바토레 산필리포)가 제안한 **분산 락 알고리즘**으로, 다중 인스턴스 환경에서 **데이터 정합성 보장과 중복 처리 방지**를 위한 안전한 락 구현 방식입니다.

---

## 1. 왜 Redlock이 필요한가?

단일 Redis 인스턴스에서의 락(`SET NX`)은 다음과 같은 한계가 있습니다:

- Redis 인스턴스가 다운되면 락도 사라짐
- 다중 서버 환경에서 락 상태를 공유하지 못함

Redlock은 **여러 Redis 인스턴스에 동시에 락을 걸고 과반수 이상 획득 시 유효한 락으로 간주**하여 안정성과 확장성을 높입니다.

---

## 2. Redlock 작동 원리

1. 클라이언트는 전역 고유 키(예: `lock:order:123`)를 사용해 Redis 노드들에 순차적으로 락 요청을 보냅니다.
2. 각 Redis 노드에는 같은 UUID와 TTL(Time-To-Live, 만료시간)으로 락을 요청합니다.
3. 전체 Redis 노드 중 과반수(예: 5개 중 3개 이상)에게 락을 획득하면 **성공**으로 간주합니다.
4. 클라이언트는 TTL 내에 작업을 완료하고 락을 해제합니다.

> 실패하거나 TTL을 초과할 경우 재시도하거나 오류로 처리합니다.

---

## 3. 락 획득 조건

- N개의 Redis 인스턴스 중 (N/2 + 1)개 이상에서 락을 성공적으로 획득
- 전체 과정이 TTL 내에 수행되어야 함 (보통 수십~수백 ms)

---

## 4. 장점

- **고가용성**: 일부 Redis 노드가 죽어도 락이 작동함
- **속도**: Redis는 메모리 기반이므로 빠름
- **확장성**: 여러 서비스가 동시에 자원 접근 시 제어 가능

---

## 5. 단점 및 주의사항

- TTL 설정이 너무 짧으면 작업 도중 락이 풀릴 수 있음
- 락 해제 실패 시 **좀비 락** 문제가 발생할 수 있음
- 락 자체는 **트랜잭션과 무관하므로** 데이터 정합성을 따로 관리해야 함

---

## 6. 주요 구현 라이브러리

| 언어    | 라이브러리               |
| ------- | ------------------------ |
| Node.js | `redlock`                |
| Python  | `aioredlock`, `redis-py` |
| C#      | `RedLock.net`            |
| Java    | `Redisson`               |

---

## 7. C# 예시 (RedLock.net)

```csharp
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

var redlockFactory = RedLockFactory.Create(new[] {
    ConnectionMultiplexer.Connect("redis1:6379"),
    ConnectionMultiplexer.Connect("redis2:6379"),
    ConnectionMultiplexer.Connect("redis3:6379")
});

using (var redLock = await redlockFactory.CreateLockAsync("lock:order:123", TimeSpan.FromSeconds(30)))
{
    if (redLock.IsAcquired)
    {
        // 안전하게 처리 가능
        ProcessOrder();
    }
    else
    {
        // 락 획득 실패 처리
        HandleFailure();
    }
}
```

---

## 8. 언제 Redlock을 사용할까?

| 상황                   | 사용 여부                |
| ---------------------- | ------------------------ |
| 단일 서버 + 단일 Redis | ❌ 필요 없음             |
| 다중 서버 + 단일 Redis | ⚠️ 위험 (단일 장애 지점) |
| 다중 서버 + 다중 Redis | ✅ Redlock 사용 권장     |
