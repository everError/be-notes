# xUnit 테스트 코드 문법 가이드 (.NET 10 / C# 14)

이 문서는 xUnit을 사용할 때 자주 쓰이는 테스트 문법, 특성(Attribute), Assert 메서드의 사용법을 정리한 참고용 문서입니다.

---

## ✅ 기본 테스트 단위: `[Fact]`

단순한 테스트 케이스를 작성할 때 사용합니다.

```csharp
using Xunit;

public class MathTests
{
    [Fact] // 매개변수 없는 단일 테스트 케이스
    public void Add_ShouldReturnCorrectSum()
    {
        // Arrange (테스트 준비)
        int a = 2;
        int b = 3;

        // Act (메서드 실행)
        int result = a + b;

        // Assert (결과 검증)
        Assert.Equal(5, result);
    }
}
```

---

## ✅ 매개변수 테스트: `[Theory]` + `[InlineData]`

입력값이 여러 개인 경우 반복 테스트 가능

```csharp
using Xunit;

public class MultiplyTests
{
    [Theory] // 여러 케이스를 한 번에 테스트
    [InlineData(2, 3, 6)]
    [InlineData(-1, 5, -5)]
    [InlineData(0, 10, 0)]
    public void Multiply_ShouldReturnCorrectResult(int a, int b, int expected)
    {
        int result = a * b;
        Assert.Equal(expected, result);
    }
}
```

---

## ✅ `Assert` 메서드 정리

| 메서드                                             | 설명                      |
| -------------------------------------------------- | ------------------------- |
| `Assert.Equal(expected, actual)`                   | 값이 같은지 확인          |
| `Assert.NotEqual(expected, actual)`                | 값이 다른지 확인          |
| `Assert.True(condition)`                           | 조건이 참인지 확인        |
| `Assert.False(condition)`                          | 조건이 거짓인지 확인      |
| `Assert.Null(object)`                              | null인지 확인             |
| `Assert.NotNull(object)`                           | null이 아닌지 확인        |
| `Assert.Contains(expectedSubstring, actualString)` | 문자열 포함 확인          |
| `Assert.Empty(collection)`                         | 컬렉션이 비어있는지 확인  |
| `Assert.Single(collection)`                        | 하나의 요소만 있는지 확인 |
| `Assert.Throws<T>(code)`                           | 예외 발생 여부 확인       |

---

## ✅ 예외 검사: `Assert.Throws<T>()`

예외가 발생하는 코드의 테스트:

```csharp
[Fact]
public void Divide_ByZero_ShouldThrowException()
{
    // Assert.Throws 로 예외 검증
    Assert.Throws<DivideByZeroException>(() =>
    {
        int x = 10 / 0;
    });
}
```

---

## ✅ 공통 테스트 클래스 구조

```csharp
public class SampleServiceTests
{
    private readonly SampleService _service;

    public SampleServiceTests()
    {
        _service = new SampleService(); // 테스트 대상 클래스 초기화
    }

    [Fact]
    public void Method_Should_DoSomething()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

---

## ✅ 여러 입력을 외부에서 불러오기: `MemberData`, `ClassData`

> 복잡한 테스트 케이스를 분리된 데이터로 구성할 때 유용

### `MemberData`

```csharp
public static IEnumerable<object[]> TestData =>
    new List<object[]> {
        new object[] { 1, 2, 3 },
        new object[] { 10, 20, 30 }
    };

[Theory]
[MemberData(nameof(TestData))]
public void Add_WithMemberData(int a, int b, int expected)
{
    Assert.Equal(expected, a + b);
}
```

### `ClassData`

```csharp
public class MyTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { 1, 2, 3 };
        yield return new object[] { 5, 6, 11 };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(MyTestData))]
public void Add_WithClassData(int a, int b, int expected)
{
    Assert.Equal(expected, a + b);
}
```

---

## ✅ 기타

- xUnit은 생성자를 `setup` 용도로 사용하며 `[SetUp]`, `[TearDown]`이 없습니다. (NUnit과 차이점)
- `IDisposable`을 구현하면 테스트 종료 후 자원 정리에 사용됩니다.
