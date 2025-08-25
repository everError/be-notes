## 📦 `oneof`란 무엇인가?

`oneof`는 "여러 옵션 중 하나를 선택하는" 필드를 정의할 때 사용합니다. `oneof`로 묶인 필드들은 메모리 공간을 공유하며, 그중 하나의 필드에 값을 설정하면 다른 필드들은 자동으로 비워집니다.

이는 마치 하나의 상자에 여러 물건 중 하나만 담을 수 있는 것과 같습니다. '사과'를 넣으면 기존에 있던 '오렌지'는 없어지고, 다시 '오렌지'를 넣으면 '사과'가 사라집니다.

---

## ✨ 주요 특징 및 장점

1.  **상호 배타적 필드**: `oneof` 그룹 내에서는 한 번에 단 하나의 필드만 값을 가질 수 있습니다.
2.  **메모리 효율성**: 모든 필드가 메모리 공간을 공유하므로, 특히 크기가 큰 필드들이 많고 그중 하나만 사용될 때 메모리를 크게 절약할 수 있습니다.
3.  **명확한 상태 표현**: 메시지가 담고 있는 데이터의 종류나 상태를 명시적으로 나타낼 수 있어, 스트리밍과 같은 복잡한 통신 프로토콜 설계에 유용합니다.

---

## syntax 및 코드 생성

#### \#\#\# `.proto` 파일에서의 정의

`oneof` 키워드와 그룹 이름을 사용하여 여러 필드를 묶습니다.

```protobuf
syntax = "proto3";

message StreamMessage {
  oneof payload {
    string text_message = 1;  // 옵션 1: 텍스트 메시지
    bytes image_chunk = 2;    // 옵션 2: 이미지 데이터 조각
    bool is_finished = 3;     // 옵션 3: 종료 신호
  }
}
```

위 예시에서 `StreamMessage`는 텍스트, 이미지 조각, 또는 종료 신호 중 하나의 데이터만 가질 수 있습니다.

#### \#\#\# C\#에서의 사용

`.proto` 파일을 컴파일하면, `oneof` 필드는 `Case`라는 특별한 `enum` 속성과 함께 생성됩니다. 이를 통해 어떤 필드가 현재 설정되어 있는지 확인할 수 있습니다.

```csharp
var message = new StreamMessage();

// 1. 텍스트 메시지 설정
message.TextMessage = "Hello, World!";

// 현재 설정된 필드 확인
Console.WriteLine(message.PayloadCase); // 출력: TextMessage
Console.WriteLine(message.ImageChunk);  // 출력: null

// 2. 이미지 데이터 설정 (이전 TextMessage는 자동으로 null이 됨)
message.ImageChunk = ByteString.CopyFrom(new byte[] { 1, 2, 3 });

Console.WriteLine(message.PayloadCase); // 출력: ImageChunk
Console.WriteLine(message.TextMessage); // 출력: null (자동으로 비워짐)

// 3. 어떤 케이스인지 확인하여 분기 처리
switch (message.PayloadCase)
{
    case StreamMessage.PayloadOneofCase.TextMessage:
        Console.WriteLine($"Received Text: {message.TextMessage}");
        break;
    case StreamMessage.PayloadOneofCase.ImageChunk:
        Console.WriteLine($"Received Image Chunk: {message.ImageChunk.Length} bytes");
        break;
    case StreamMessage.PayloadOneofCase.None:
        Console.WriteLine("Payload is not set.");
        break;
}
```

---

## 📝 주요 사용 사례

- **서로 다른 종류의 데이터를 하나의 메시지로 표현할 때**: 응답 메시지가 성공 결과 또는 실패 정보 중 하나만 담아야 할 경우에 유용합니다.
- **gRPC 스트리밍**: 스트리밍 통신 시, 첫 번째 메시지로는 메타데이터를 보내고 이후 메시지들로는 데이터 조각을 보내는 등, 메시지의 역할을 구분하는 프로토콜을 설계할 때 매우 효과적입니다.
