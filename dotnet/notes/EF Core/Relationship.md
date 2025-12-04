## EF Core 관계 정의 가이드

---

### 1. 기본 용어

| 용어                | 설명                        |
| ------------------- | --------------------------- |
| Principal Entity    | PK를 가진 엔티티 (부모)     |
| Dependent Entity    | FK를 가진 엔티티 (자식)     |
| Navigation Property | 관련 엔티티를 참조하는 속성 |

---

### 2. FK 선언 방법

#### 2.1 Convention (자동 인식)

```csharp
public class Order
{
    public long OrderId { get; set; }

    public long CustomerId { get; set; }      // 자동 인식: {Navigation명} + Id
    public Customer Customer { get; set; }
}
```

#### 2.2 [ForeignKey] 어트리뷰트

```csharp
// Navigation Property에 선언
public class Order
{
    public long BuyerId { get; set; }

    [ForeignKey(nameof(BuyerId))]
    public Customer Customer { get; set; }
}

// 또는 FK Property에 선언
public class Order
{
    [ForeignKey(nameof(Customer))]
    public long BuyerId { get; set; }

    public Customer Customer { get; set; }
}
```

#### 2.3 Fluent API

```csharp
modelBuilder.Entity<Order>()
    .HasOne(o => o.Customer)
    .WithMany(c => c.Orders)
    .HasForeignKey(o => o.BuyerId);
```

---

### 3. 관계 종류별 설정

#### 3.1 1:N (One-to-Many)

```csharp
public class Customer
{
    public long CustomerId { get; set; }
    public ICollection<Order> Orders { get; set; }
}

public class Order
{
    public long OrderId { get; set; }
    public long CustomerId { get; set; }
    public Customer Customer { get; set; }
}
```

#### 3.2 1:1 (One-to-One)

```csharp
public class User
{
    public long UserId { get; set; }
    public UserProfile Profile { get; set; }
}

public class UserProfile
{
    public long UserProfileId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; }
}
```

#### 3.3 N:N (Many-to-Many) - EF Core 5.0+

```csharp
public class Student
{
    public long StudentId { get; set; }
    public ICollection<Course> Courses { get; set; }
}

public class Course
{
    public long CourseId { get; set; }
    public ICollection<Student> Students { get; set; }
}
```

#### 3.4 Self-Referencing

```csharp
public class Employee
{
    public long EmployeeId { get; set; }

    public long? ManagerId { get; set; }
    public Employee Manager { get; set; }

    public ICollection<Employee> Subordinates { get; set; }
}
```

---

### 4. 다중 FK 관계 - [InverseProperty]

같은 테이블을 여러 FK로 참조할 때:

```csharp
public class Match
{
    public long MatchId { get; set; }

    [ForeignKey(nameof(HomeTeam))]
    public long HomeTeamId { get; set; }
    public Team HomeTeam { get; set; }

    [ForeignKey(nameof(AwayTeam))]
    public long AwayTeamId { get; set; }
    public Team AwayTeam { get; set; }
}

public class Team
{
    public long TeamId { get; set; }

    [InverseProperty(nameof(Match.HomeTeam))]
    public ICollection<Match> HomeMatches { get; set; }

    [InverseProperty(nameof(Match.AwayTeam))]
    public ICollection<Match> AwayMatches { get; set; }
}
```

---

### 5. 삭제 동작 (Fluent API만 가능)

```csharp
modelBuilder.Entity<Order>()
    .HasOne(o => o.Customer)
    .WithMany(c => c.Orders)
    .OnDelete(DeleteBehavior.Cascade);
```

| DeleteBehavior | 설명                       |
| -------------- | -------------------------- |
| Cascade        | 부모 삭제 시 자식도 삭제   |
| Restrict       | 자식 있으면 부모 삭제 불가 |
| SetNull        | 부모 삭제 시 FK를 null로   |
| NoAction       | DB에 위임                  |

---

### 6. 요약

| 상황             | 방법                |
| ---------------- | ------------------- |
| 규칙에 맞는 FK명 | Convention (자동)   |
| 다른 FK명        | `[ForeignKey]`      |
| 다중 FK 관계     | `[InverseProperty]` |
| 삭제 동작        | Fluent API          |
