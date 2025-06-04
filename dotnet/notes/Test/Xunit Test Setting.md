# .NET 테스트 가이드

이 문서는 .NET(Core 포함)에서 단위 테스트(Unit Test)를 수행하기 위한 기본 개념과 실습 환경 구성 방법을 설명합니다.

---

## 📌 테스트 프레임워크 종류

| 프레임워크 | 설명                                                | 비고                    |
| ---------- | --------------------------------------------------- | ----------------------- |
| **xUnit**  | .NET Core에서 가장 널리 쓰이며, Microsoft 공식 권장 | `dotnet new xunit` 지원 |
| NUnit      | 유연하고 풍부한 기능 제공. 구 버전 호환성 뛰어남    | `[TestCase]` 등 지원    |
| MSTest     | Microsoft 자체 테스트 프레임워크. 보수적인 스타일   | Visual Studio 기본 포함 |

> ✅ 대부분의 신규 프로젝트는 **xUnit** 사용 권장

---

## ⚙️ 테스트 프로젝트 구성

### 1. 테스트 프로젝트 생성

```bash
dotnet new xunit -n MyProject.Tests
dotnet add MyProject.Tests reference MyProject
```

### 2. 필수 NuGet 패키지

`.csproj` 파일에 다음 패키지를 포함해야 함:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2" />
</ItemGroup>
```

| 패키지                      | 역할                                        |
| --------------------------- | ------------------------------------------- |
| `Microsoft.NET.Test.Sdk`    | 테스트 실행 및 리포팅을 위한 핵심 SDK       |
| `xunit`                     | 테스트 프레임워크 본체 (어노테이션 등 제공) |
| `xunit.runner.visualstudio` | Visual Studio 및 CLI 테스트 실행 지원       |

---

## 🧪 테스트 예제 (xUnit)

```csharp
using Xunit;

public class CalculatorTests
{
    [Fact]
    public void Add_ReturnsCorrectSum()
    {
        var calc = new Calculator();
        var result = calc.Add(2, 3);

        Assert.Equal(5, result);
    }
}
```

---

## ▶️ 테스트 실행 방법

### Visual Studio

- 테스트 탐색기 열기 (Test → Test Explorer)
- `Ctrl + R, A` : 전체 테스트 실행

### CLI

```bash
dotnet test
```

---

## ✅ 권장 폴더 구조

```
MySolution/
├── MyProject/               # 실제 프로젝트
├── MyProject.Tests/         # 테스트 프로젝트
│   └── CalculatorTests.cs
├── MySolution.sln
```

---

## 🧠 팁

- 테스트 클래스는 `public class`, 메서드는 `public void` 여야 함
- 클래스/메서드에 `static` 붙이면 xUnit에서 인식하지 못함
- `Assert`를 통해 다양한 비교 제공 (`Equal`, `True`, `Throws`, 등)
- 공통 초기화가 필요하면 생성자 또는 `IClassFixture<T>` 사용
