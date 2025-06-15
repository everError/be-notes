## 📌 Spring에서 자주 쓰이는 어노테이션 정리

Spring 프레임워크 및 Spring Boot 환경에서 **자주 사용되는 어노테이션**을 용도별로 구분하여 설명합니다. 각 어노테이션은 DI(의존성 주입), AOP(관점지향), Web 계층, 트랜잭션 등 다양한 레이어에서 핵심적인 역할을 합니다.

---

### 🔧 Bean 등록 및 구성 관련

| 어노테이션            | 설명                                                                            |
| ---------------- | ----------------------------------------------------------------------------- |
| `@Component`     | 스프링이 관리하는 일반적인 빈(Bean)으로 등록. 커스텀 클래스를 빈으로 만들 때 사용                             |
| `@Configuration` | 설정 클래스를 명시. 내부 메서드에 `@Bean` 선언 가능                                             |
| `@Bean`          | 개발자가 직접 Bean 객체 생성을 제어할 때 사용                                                  |
| `@ComponentScan` | 지정된 패키지 내 컴포넌트(`@Component`, `@Service`, `@Repository`, `@Controller`)를 자동 스캔 |
| `@Import`        | 다른 설정 클래스 혹은 빈 설정을 현재 설정에 포함시킴                                                |

---

### 💉 의존성 주입 (DI)

| 어노테이션                               | 설명                                  |
| ----------------------------------- | ----------------------------------- |
| `@Autowired`                        | 생성자/필드/메서드에 의존성 주입. 생성자 주입 방식 권장    |
| `@Value`                            | application.yml/properties 등 설정값 주입 |
| `@Qualifier`                        | 동일 타입의 Bean이 여러 개일 때 특정 Bean 선택     |
| `@RequiredArgsConstructor` (Lombok) | `final` 필드를 자동으로 생성자 주입하는 Lombok 기능 |

---

### 🌐 웹 계층 관련

| 어노테이션             | 설명                                                                   |
| ----------------- | -------------------------------------------------------------------- |
| `@Controller`     | 웹 요청을 처리하는 클래스 지정. 뷰 반환 중심                                           |
| `@RestController` | JSON 등 응답용 API 처리 컨트롤러. `@Controller + @ResponseBody` 조합             |
| `@RequestMapping` | HTTP 경로 및 메서드(GET, POST 등) 설정. `@GetMapping`, `@PostMapping`으로 분화 가능 |
| `@PathVariable`   | URL 경로의 일부를 변수로 받음                                                   |
| `@RequestParam`   | 쿼리 스트링 파라미터를 매핑                                                      |
| `@RequestBody`    | 요청 본문(JSON 등)을 객체로 바인딩                                               |
| `@ResponseBody`   | 컨트롤러의 리턴 값을 HTTP 응답 본문으로 직렬화                                         |
| `@CrossOrigin`    | CORS 설정을 위해 사용                                                       |

---

### 💼 서비스 및 DAO 계층

| 어노테이션         | 설명                                          |
| ------------- | ------------------------------------------- |
| `@Service`    | 비즈니스 로직을 처리하는 클래스에 부여. `@Component`의 특수화    |
| `@Repository` | DAO 클래스에 부여. 예외 변환 기능 포함. `@Component`의 특수화 |

---

### 🔐 Spring Security

| 어노테이션                | 설명                                                       |
| -------------------- | -------------------------------------------------------- |
| `@EnableWebSecurity` | Spring Security를 프로젝트에 활성화                               |
| `@PreAuthorize`      | 메서드 실행 전에 인가 검사. ex: `@PreAuthorize("hasRole('ADMIN')")` |
| `@Secured`           | 특정 역할 기반으로 접근 제한                                         |
| `@WithMockUser`      | 테스트용 SecurityContext 생성                                  |

---

### 🔄 트랜잭션 관리

| 어노테이션            | 설명                                     |
| ---------------- | -------------------------------------- |
| `@Transactional` | 트랜잭션 경계 지정. 성공 시 commit, 실패 시 rollback |

---

### 📦 기타 유용한 어노테이션

| 어노테이션             | 설명                                    |
| ----------------- | ------------------------------------- |
| `@Slf4j` (Lombok) | 자동으로 `private static final Logger` 생성 |
| `@PostConstruct`  | Bean 생성 직후 호출되는 초기화 메서드               |
| `@Scheduled`      | 주기적으로 메서드를 실행 (스케줄링)                  |
| `@Async`          | 비동기 처리를 위한 어노테이션                      |

---

### 📚 참고

* 모든 `@Component` 계열 어노테이션은 **빈 자동 등록** 대상입니다.
* 생성자 기반 주입은 **불변성 유지**와 **테스트 용이성** 측면에서 가장 권장됩니다.
* `@Transactional`, `@PreAuthorize` 등의 어노테이션은 내부적으로 AOP 기반입니다.