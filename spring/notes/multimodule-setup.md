# Spring Boot + Kotlin 멀티모듈 실전 구성 가이드

## Spring Boot 3 + Kotlin 기반의 멀티모듈 프로젝트를 실전적으로 구성하는 방법을 설명합니다. 루트 `build.gradle.kts`에서 공통 설정을 정의하고, 하위 모듈에서는 별도의 plugin 및 dependency 설정 없이 유지합니다.

## 📁 디렉토리 구조 예시

```
springboot3-kotlin/
├── build.gradle.kts           # 루트 설정 (공통 플러그인 및 의존성)
├── settings.gradle.kts        # 하위 모듈 포함 선언
├── gateway-service/           # API Gateway
│   └── build.gradle.kts       # 최소 설정만 존재
├── auth-service/              # 인증 서비스
│   └── build.gradle.kts
└── user-service/              # 사용자 서비스
    └── build.gradle.kts
```

---

## 🔧 루트 `build.gradle.kts`

```kotlin
plugins {
    kotlin("jvm") version "2.1.20"
    kotlin("plugin.spring") version "2.1.20" apply false
    id("org.springframework.boot") version "3.5.0" apply false
    id("io.spring.dependency-management") version "1.1.7" apply false
}

allprojects {
    group = "com.example"
    version = "0.0.1-SNAPSHOT"

    repositories {
        mavenCentral()
    }
}

subprojects {
    apply(plugin = "org.jetbrains.kotlin.jvm")
    apply(plugin = "org.jetbrains.kotlin.plugin.spring")
    apply(plugin = "org.springframework.boot")
    apply(plugin = "io.spring.dependency-management")

    extensions.configure<org.jetbrains.kotlin.gradle.dsl.KotlinJvmProjectExtension> {
        jvmToolchain(21)
    }

    dependencies {
        implementation("com.fasterxml.jackson.module:jackson-module-kotlin")
        implementation("org.jetbrains.kotlin:kotlin-reflect")

        testImplementation("org.springframework.boot:spring-boot-starter-test")
        testImplementation("org.jetbrains.kotlin:kotlin-test-junit5")
        testRuntimeOnly("org.junit.platform:junit-platform-launcher")
    }

    tasks.withType<Test> {
        useJUnitPlatform()
    }
}
```

### 🔍 주요 문법 설명

- `apply false`: 루트에서는 선언만 하고 하위 모듈에서는 subprojects 블록으로 일괄 적용
- `subprojects`: 하위 모듈에 plugin, dependency, kotlin 설정을 일괄 적용하는 블록
- `jvmToolchain`: Kotlin 컴파일러가 사용할 JDK 버전 지정
- `useJUnitPlatform()`: JUnit5 플랫폼으로 테스트 설정

---

## ⚙️ `settings.gradle.kts`

```kotlin
rootProject.name = "example"
include("gateway-service", "auth-service", "user-service")
```

### 설명

- `include(...)`: 멀티모듈 프로젝트로 포함할 서브 프로젝트 선언
- `rootProject.name`: 전체 프로젝트의 이름 정의

---

## 🧩 하위 모듈의 `build.gradle.kts` 예시

```kotlin
dependencies {
    implementation("org.springframework.boot:spring-boot-starter-web")
}
```

### 설명

- 루트의 `subprojects` 블록에서 모든 설정을 내려주기 때문에 plugin이나 dependencies 선언이 필요 없음
- 단, 모듈별 특화 의존성이 필요한 경우에만 별도 작성

---

## 🧱 Monolithic vs Multi-module vs MSA 구조 차이

| 항목           | 모놀리식 (Monolith)              | 멀티모듈 (Multi-module)      | MSA (Microservices)                 |
| -------------- | -------------------------------- | ---------------------------- | ----------------------------------- |
| 구성           | 하나의 프로젝트로 모든 기능 구성 | 하나의 프로젝트 내 여러 모듈 | 각 서비스가 별도 프로젝트이자 앱    |
| 배포 단위      | 전체 통합 빌드/배포              | 루트에서 통합 빌드/배포      | 서비스별 독립 빌드/배포             |
| 목적           | 빠른 개발, 단순 구조             | 관심사 분리 및 테스트 용이   | 독립적 확장, 장애 격리, 복잡성 분산 |
| 기술 스택      | 동일                             | 동일                         | 서비스마다 자유롭게 선택 가능       |
| 독립 실행 여부 | 단일 실행 파일                   | 단일 실행 파일               | 각 서비스는 개별 실행               |
| 유지보수       | 어려워짐 (덩어리 커질수록)       | 상대적으로 쉬움              | 매우 쉬움 (단, 설계 난이도 높음)    |

### 💡 결론

- 멀티모듈은 모놀리식 기반 코드를 **구조적으로 분리**하여 유지보수와 확장성을 높이는 데 유리함
- MSA는 물리적으로도 **프로세스/네트워크 레벨까지 분리**되며, 실제 운영 환경에선 멀티모듈과 다름
- 단, **멀티모듈로 구성한 각 모듈을 개별 실행 및 배포**하면 실질적인 MSA와 동일하게 운영 가능함
- 멀티모듈은 **MSA로 전환하기 전의 과도기 구조**로도 자주 사용됨

---

## ✅ 실전 팁

- 공통 설정을 루트에 몰아주면 반복 없이 빠르게 멀티모듈 확장 가능
- 모듈마다 plugin 선언할 필요 없어 유지관리 쉬움
- 공통 모듈(common), 도메인 별 모듈(domain-user 등)도 같은 방식으로 포함 가능
