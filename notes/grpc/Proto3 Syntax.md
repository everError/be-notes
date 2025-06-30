# Protocol Buffers v3 (proto3) 문법 정리

## 1. 기본 구조

```proto
syntax = "proto3";

package mypackage;

option csharp_namespace = "MyApp.Grpc";

import "google/protobuf/empty.proto";

message MyMessage {
  string name = 1;
  int32 age = 2;
}

service MyService {
  rpc MyMethod (MyMessage) returns (google.protobuf.Empty);
}
```

## 2. 기본 타입

| proto3 타입 | 설명                  | C# 매핑 타입 |
| ----------- | --------------------- | ------------ |
| double      | 64비트 실수           | double       |
| float       | 32비트 실수           | float        |
| int32       | 32비트 정수           | int          |
| int64       | 64비트 정수           | long         |
| uint32      | 부호 없는 32비트 정수 | uint         |
| uint64      | 부호 없는 64비트 정수 | ulong        |
| bool        | true/false            | bool         |
| string      | UTF-8 문자열          | string       |
| bytes       | 바이트 배열           | ByteString   |

## 3. 메시지 정의

```proto
message User {
  int32 id = 1;
  string name = 2;
  string email = 3;
}
```

- 각 필드는 **고유 번호**가 필요 (1\~2^29 - 1)
- `= 번호`는 필드의 식별자 (네트워크 직렬화 시 사용)

## 4. 반복 필드 (repeated)

```proto
message UserList {
  repeated User users = 1;
}
```

- `repeated` 키워드로 배열/리스트 표현
- C#에서는 `RepeatedField<T>` 로 매핑됨

## 5. 내장 타입 import

```proto
import "google/protobuf/empty.proto";           // Empty
import "google/protobuf/wrappers.proto";        // StringValue, Int32Value 등
```

## 6. 서비스 정의

```proto
service UserService {
  rpc AddUser (User) returns (User);
  rpc GetUser (google.protobuf.Int32Value) returns (User);
  rpc ListUsers (google.protobuf.Empty) returns (UserList);
}
```

- `rpc` 메서드는 항상 **message 타입**으로 주고받음
- `primitive 타입 단독 사용 불가` → `google.protobuf.XxxValue` 사용

## 7. 스트리밍

```proto
// 서버 스트리밍
rpc StreamUsers (google.protobuf.Empty) returns (stream User);

// 클라이언트 스트리밍
rpc UploadUsers (stream User) returns (google.protobuf.Empty);

// 양방향 스트리밍
rpc ChatUsers (stream User) returns (stream User);
```

## 8. 옵션

```proto
option csharp_namespace = "MyApp.Grpc";
```

- 생성되는 C# 클래스 네임스페이스 지정

## 9. 예약어 방지 (reserved)

```proto
message User {
  reserved 4, 5, 6;
  reserved "old_field";
}
```

- 추후 필드 번호나 이름 재사용 방지

## 10. 주석

```proto
// 한 줄 주석
/* 여러 줄 주석 */
```

---

필요 시: `.proto` → C# 파일 생성 명령어

```sh
protoc --csharp_out=. --grpc_out=. --plugin=protoc-gen-grpc=grpc_csharp_plugin *.proto
```

---

※ `proto3`에서는 `optional` 키워드가 일부 제한되었지만, 최근에는 다시 도입됨 (proto3 optional field).
