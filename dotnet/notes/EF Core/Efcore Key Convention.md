# EF Core의 기본 키(Primary Key) 자동 인식 규칙 가이드

이 문서는 Entity Framework Core에서 엔터티 모델에 명시적인 애트리뷰트나 Fluent API를 작성하지 않아도 기본 키(primary key)가 자동으로 인식되는 이유와 그 내부 규칙(convention)에 대해 정리한 가이드입니다.

---

## ✅ EF Core는 어떻게 기본 키를 자동으로 인식할까?

EF Core는 "컨벤션 기반 개발(convention-based configuration)"을 지향합니다. 즉, 특별한 설정 없이도 **일정한 규칙만 따르면 프레임워크가 자동으로 적절한 설정을 적용**해 줍니다.

그 중 대표적인 것이 기본 키 자동 인식입니다.

---

## 🔍 기본 키로 인식되는 필드 규칙

EF Core는 다음과 같은 이름 패턴을 가진 속성을 자동으로 기본 키로 인식합니다:

| 우선순위 | 속성명 예시                                 |
| -------- | ------------------------------------------- |
| 1️⃣       | `Id`                                        |
| 2️⃣       | `[엔터티이름]Id` (예: `RecordId`, `UserId`) |

두 경우 모두 EF Core는 해당 속성을 \*\*기본 키(PK)\*\*로 간주합니다.

> 예: `public int Id { get; set; }` 또는 `public int RecordId { get; set; }`

---

## 🛠 자동으로 설정되는 기타 기능

### 1. 정수형 키의 자동 증가

- 기본 키가 `int`, `long` 등의 정수형이고,
- DB 공급자가 이를 지원하면 → EF Core는 **자동 증가(IDENTITY / AUTOINCREMENT)** 로 처리함

### 2. SQLite 예외

- SQLite는 `INTEGER PRIMARY KEY`에 대해 자동으로 `AUTOINCREMENT` 처리함
- 애트리뷰트 없이도 `Id`가 있으면 PK + 자동 증가 필드로 작동

---

## 🧪 실전 예시

```csharp
public class Record
{
    public int Id { get; set; } // PK + AutoIncrement로 자동 인식
    public string Data { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

위와 같이 아무 애트리뷰트도 없지만, EF Core는 다음과 같이 해석합니다:

- `Id` → 기본 키
- `int` → 자동 증가 (SQLite에서는 `AUTOINCREMENT`)
- `string`, `DateTime` → 형식에 맞춰 자동 매핑

---

## ⚠️ 예외 및 명시가 필요한 경우

- **기본 키 명이 규칙과 다를 경우**: `[Key]` 애트리뷰트 또는 Fluent API 필요
- **복합 키 사용 시**: Fluent API의 `HasKey()` 메서드로 설정해야 함
- **기본 키 없이 마이그레이션 시도**: `The entity type 'X' requires a primary key to be defined.` 예외 발생 (특히 SQL Server에서)

---

## ✅ 요약 정리

| 항목               | 설명                                                    |
| ------------------ | ------------------------------------------------------- |
| 기본 키 자동 인식  | `Id`, `[EntityName]Id` 규칙 따를 경우                   |
| 자동 증가 적용     | `int`, `long` 등 정수형 기본 키일 때 가능               |
| SQLite 특이점      | `INTEGER PRIMARY KEY` = 자동 증가 적용됨                |
| 명시가 필요한 경우 | 복합 키, 다른 이름 사용 시 `[Key]` 또는 Fluent API 필요 |

---

EF Core는 관례 기반으로 많은 설정을 자동 처리하지만, **명확성을 위해 명시적으로 `[Key]`, `[Required]`, `HasKey()` 등을 사용하는 것이 권장되는 상황**도 존재합니다.

모델이 복잡해지기 전까지는 이 자동 규칙만으로도 충분히 설계하고 테스트할 수 있습니다.
