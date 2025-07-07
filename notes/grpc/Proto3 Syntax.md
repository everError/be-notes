# 📘 Protocol Buffers v3 (proto3) 문법 정리

Protocol Buffers(이하 Protobuf)는 Google에서 만든 언어 중립, 플랫폼 중립, 확장 가능한 직렬화 포맷입니다. 이 문서는 `proto3` 문법을 `.proto` 파일 기준으로 정리하고, 각 키워드와 문법 요소의 의미를 C# 환경 중심으로 설명합니다.

---

## 1. 기본 구조

```proto
syntax = "proto3"; // 프로토콜 버전 지정 (반드시 첫 줄)

package mypackage;  // 네임스페이스 역할, C#에선 csharp_namespace와 병행 사용 가능

option csharp_namespace = "MyApp.Grpc"; // C#에서 생성되는 네임스페이스 강제 지정

import "google/protobuf/empty.proto"; // 다른 proto 파일 import (Empty 등 내장 타입 사용 시 필요)

message MyMessage {                 // 메시지 정의 블록 시작
  string name = 1;                 // 필드명, 타입(string), 태그 번호 (= 1)
  int32 age = 2;                   // 정수 타입
}

service MyService {                // gRPC 서비스 정의 시작
  rpc MyMethod (MyMessage) returns (google.protobuf.Empty); // 요청과 응답 메시지
}
```

---

## 2. 기본 타입

| proto3 타입 | 설명                            | C# 매핑 타입 |
| ----------- | ------------------------------- | ------------ |
| double      | 64비트 부동소수점 실수          | double       |
| float       | 32비트 부동소수점 실수          | float        |
| int32       | 부호 있는 32비트 정수           | int          |
| int64       | 부호 있는 64비트 정수           | long         |
| uint32      | 부호 없는 32비트 정수           | uint         |
| uint64      | 부호 없는 64비트 정수           | ulong        |
| sint32      | ZigZag 인코딩된 int32           | int          |
| sint64      | ZigZag 인코딩된 int64           | long         |
| fixed32     | 고정 길이 부호 있는 32비트 정수 | int          |
| fixed64     | 고정 길이 부호 있는 64비트 정수 | long         |
| bool        | true/false                      | bool         |
| string      | UTF-8 문자열                    | string       |
| bytes       | 바이트 배열                     | ByteString   |

---

## 3. 메시지 정의

```proto
message User {
  int32 id = 1;          // 고유 식별자 필드
  string name = 2;       // 사용자 이름
  string email = 3;      // 사용자 이메일
}
```

- `message` 키워드는 직렬화 가능한 데이터 구조를 정의
- 각 필드는 `= N` 태그 번호가 반드시 필요 (네트워크에서 순서 식별용)
- 태그 번호는 1~2^29-1 사이여야 하며, 19000~19999는 예약됨

---

## 4. 반복 필드 (repeated)

```proto
message UserList {
  repeated User users = 1;
}
```

- `repeated` 키워드는 배열, 리스트를 의미함
- C#에서는 `RepeatedField<T>`로 매핑되어 `IList<T>`처럼 동작

---

## 5. 내장 타입 import 및 사용

```proto
import "google/protobuf/empty.proto";     // Empty 메시지 (빈 응답 등)
import "google/protobuf/wrappers.proto";  // primitive 타입을 래핑한 메시지 사용 가능 (StringValue 등)
```

예:

```proto
rpc GetUser(google.protobuf.Int32Value) returns (User);
```

- 기본 타입만 넘기고 싶을 때는 `XxxValue`를 사용해야 함 (`int32`, `string` 단독 사용 불가)

---

## 6. 서비스 정의

```proto
service UserService {
  rpc AddUser (User) returns (User);
  rpc GetUser (google.protobuf.Int32Value) returns (User);
  rpc ListUsers (google.protobuf.Empty) returns (UserList);
}
```

- `service` 블록은 gRPC 서비스 하나를 정의
- `rpc`는 원격 프로시저 호출 (Remote Procedure Call)을 의미
- 반드시 요청과 응답이 `message` 타입이어야 함

---

## 7. 스트리밍

```proto
rpc StreamUsers (google.protobuf.Empty) returns (stream User);            // 서버 스트리밍
rpc UploadUsers (stream User) returns (google.protobuf.Empty);           // 클라이언트 스트리밍
rpc ChatUsers (stream User) returns (stream User);                       // 양방향 스트리밍
```

- `stream` 키워드를 붙이면 스트리밍 처리 가능
- gRPC에서는 서버, 클라이언트, 양방향 스트리밍 모두 지원

---

## 8. 옵션

```proto
option csharp_namespace = "MyApp.Grpc"; // 생성될 C# 코드의 네임스페이스 지정
```

- C# 외 다른 언어에서도 대응되는 옵션 존재 (`java_package`, `go_package` 등)
- 패키지와 분리된 언어별 네임스페이스 지정이 가능

---

## 9. 예약어 방지 (reserved)

```proto
message User {
  reserved 4, 5, 6;             // 필드 번호 예약
  reserved "old_field";        // 필드 이름 예약
}
```

- 향후 해당 번호/이름으로 필드를 다시 선언하지 못하도록 막는 용도
- 메시지 구조 변경 시 호환성 확보를 위해 중요

---

## 10. 주석

```proto
// 한 줄 주석
/* 여러 줄 주석 */
```

- Swagger UI에 표시되려면 `.csproj`에서 `GenerateDocumentationFile` + `IncludeGrpcXmlComments` 설정 필요

---

## ✅ C# 코드 생성 명령어 (protoc 사용 시)

```bash
protoc --csharp_out=. --grpc_out=. \
       --plugin=protoc-gen-grpc=grpc_csharp_plugin \
       *.proto
```

> Visual Studio에서는 위 설정이 `.csproj`에 포함되어 있으면 자동으로 빌드 시 생성됩니다.

---

## ⚠️ proto3의 optional 지원

- `proto3`는 초기에는 모든 필드를 required로 취급했으나,
- 최근에는 `optional` 키워드가 다시 도입되어 선택적 필드 지원

```proto
message Product {
  optional string description = 4;
}
```

- C#에서는 nullable 타입(`string?`, `int?`)으로 매핑됨

---

## 11. 자주 사용하는 내장 타입 정리 (google.protobuf)

| 내장 타입                      | 파일               | 설명                                           |
| ------------------------------ | ------------------ | ---------------------------------------------- |
| `Empty`                        | `empty.proto`      | 내용 없는 요청 또는 응답 메시지                |
| `Any`                          | `any.proto`        | 동적으로 메시지 타입을 포함할 수 있음 (다형성) |
| `Timestamp`                    | `timestamp.proto`  | 날짜/시간 표현 (`seconds`, `nanos` 조합)       |
| `Duration`                     | `duration.proto`   | 시간 간격 표현                                 |
| `Struct`                       | `struct.proto`     | 동적 JSON 객체 구조 표현 (key-value)           |
| `Value`, `ListValue`           | `struct.proto`     | JSON의 값/배열 표현                            |
| `FieldMask`                    | `field_mask.proto` | PATCH 시 특정 필드만 업데이트할 때 사용        |
| `StringValue`, `Int32Value` 등 | `wrappers.proto`   | primitive 타입을 nullable로 감싸는 래퍼 메시지 |
| `BoolValue`, `BytesValue` 등   | `wrappers.proto`   | 같은 방식의 boolean, bytes 타입 nullable 표현  |

> 💡 이 파일들은 Grpc.Tools 또는 protoc를 통해 자동 포함되며, `import "google/protobuf/*.proto"`로 사용합니다.
