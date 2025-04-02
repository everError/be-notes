# 대규모 트래픽 환경에서의 공유 자원 업데이트 처리 전략

대규모 트래픽 환경에서 여러 인스턴스 또는 클라이언트가 동일한 공유 자원(DB의 특정 row 등)에 대해 동시에 접근하고 CRUD 작업을 수행하는 경우, **데이터 정합성, 성능, 확장성**을 고려한 설계가 필요하다.

---

## 1. 데이터 정합성을 위한 기본 처리 전략

### ✅ 트랜잭션(Transaction)

- EF Core, Hibernate 등의 ORM 프레임워크는 트랜잭션 단위로 데이터 정합성을 보장.
- 단일 DB 인스턴스에서 동시 접근 시, 트랜잭션을 통해 충돌을 방지할 수 있음.
- 하지만 다중 인스턴스(MSA 구조)에서는 트랜잭션만으로는 완전한 충돌 방지 어려움.

### ✅ 낙관적/비관적 락

- 낙관적 락(Optimistic Lock): `RowVersion`, `Timestamp` 컬럼을 사용하여 충돌 감지 후 재시도.
- 비관적 락(Pessimistic Lock): `SELECT ... FOR UPDATE`, `WITH (UPDLOCK, ROWLOCK)` 등 사용하여 선점 제어.
- 단일 인스턴스에서는 효과적이지만, 병목이나 데드락 위험 존재.

---

## 2. 분산 환경에서의 동시성 제어

### ✅ Redlock (Redis 분산 락)

- Redis의 여러 노드에 동일한 락 키를 설정하여 과반수 이상이 성공하면 락 획득.
- 선점, 중복 요청 방지, 이벤트 선착순 처리 등에 유용.
- 락 획득 실패 시에는 재시도 또는 오류 처리 필요.

> 단점: 락 실패 시 처리 보장 어려움, 병렬성 감소 가능성

---

## 3. 락 없이 처리하는 병렬 안전한 Upsert 전략

### ✅ SQL MERGE / UPSERT / ON CONFLICT

- 데이터가 존재하면 UPDATE, 없으면 INSERT 하는 구조.
- 여러 인스턴스에서 동시에 접근해도 데이터가 유실되지 않도록 보장.

**예시 (PostgreSQL)**

```sql
INSERT INTO monthly_stock (id, count)
VALUES (123, 1)
ON CONFLICT (id)
DO UPDATE SET count = monthly_stock.count + EXCLUDED.count;
```

**예시 (SQL Server)**

```sql
MERGE INTO monthly_stock AS target
USING (SELECT 123 AS id, 1 AS delta) AS source
ON target.id = source.id
WHEN MATCHED THEN
    UPDATE SET count = target.count + source.delta
WHEN NOT MATCHED THEN
    INSERT (id, count) VALUES (source.id, source.delta);
```

> 조건: 순서에 민감하지 않고, 모든 작업이 누락 없이 처리되어야 할 때 적합

---

## 4. 내부 큐를 통한 순차 처리 전략 (서비스 레벨 동시성 제어)

### ✅ 키 기반 직렬 처리 구조

동일한 키(id)에 대해 병렬 접근이 발생하지 않도록, 서버 내에서 **Key 단위 큐**를 생성하여 순차적으로 작업 처리.

```plaintext
[API 요청]
   ↓
[Key별 Queue Dictionary]
   └─ id=123 → Queue<Job>
   └─ id=456 → Queue<Job>
   ↓
[Dispatcher 또는 Worker Task]
   → 각 Key에 대해 순차 처리
```

### ✅ 구현 방식 (C# 예시)

- `ConcurrentDictionary<string, Channel<Func<Task>>>` 기반 큐
- 새로운 요청이 들어오면 해당 키의 큐에 작업 등록
- 큐가 없으면 새로운 Dispatcher Task 실행

### ✅ 장점

- 병렬 충돌 최소화
- 락 없이 안정적으로 순차 처리 가능
- 확장성 확보 가능 (멀티 키는 병렬 처리)

### ✅ 단점

- 인스턴스가 재시작되면 큐 유실 가능 (보완: 중앙 큐 사용)
- 키 수가 많을 경우 메모리 관리 주의

---

## 5. 멀티 인스턴스 환경에서의 확장 전략

- 키별 처리를 위해 Redis, Kafka, SQS 등의 **중앙 메시지 큐** 사용
- **Hash(key) % N** 방식으로 특정 키가 특정 인스턴스에서만 처리되도록 설계
- Kafka의 경우 **Partition Key 기반 소비자 분산 처리** 구조가 이상적

---

## 6. 결론 및 전략 선택 기준

| 조건                          | 추천 전략                          |
| ----------------------------- | ---------------------------------- |
| 단순한 Upsert, 순서 상관 없음 | SQL MERGE / UPSERT 사용            |
| 동일 키에 대해 순차 처리 필요 | 내부 Key 기반 Queue 처리           |
| 중복 처리 방지, 선착순 등     | Redlock 또는 분산 락               |
| MSA 환경 + 확장성 필요        | 중앙 큐 + Partition 기반 분산 처리 |

모든 요청을 안정적으로 처리하되, 동시성 문제를 방지하고 싶다면 **락보다는 순차적 큐 처리나 병렬 안전한 DB 쿼리 전략을 활용**하는 것이 더 효율적일 수 있다.
