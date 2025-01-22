# JWT (Json Web Token)

## 소개

JWT(Json Web Token)는 JSON 객체를 사용하여 정보를 안전하게 전송하기 위한 개방형 표준(RFC 7519)입니다. 인증과 정보 교환에 주로 사용되며, 클라이언트와 서버 간의 상태를 유지하지 않는 무상태성(stateless)을 제공합니다.

---

## 구조

JWT는 다음과 같은 세 부분으로 구성되며, 각 부분은 점(`.`)으로 구분됩니다:

1. **Header**: 토큰의 타입(JWT)과 서명에 사용된 알고리즘 정보를 포함합니다.
2. **Payload**: 사용자의 정보와 클레임(Claim)을 포함합니다.
3. **Signature**: Header와 Payload를 조합해 생성된 서명으로, 토큰의 무결성을 보장합니다.

### 예시

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ
.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

---

## Access Token과 Refresh Token

JWT는 일반적으로 **Access Token**과 **Refresh Token**으로 나뉘어 사용됩니다.

### Access Token

- **역할**: 사용자가 특정 리소스에 접근할 권한을 검증하는 데 사용됩니다.
- **특징**:
  - 비교적 짧은 만료 시간(TTL, Time-To-Live)을 가집니다.
  - 주로 Authorization 헤더에 포함되어 서버에 전달됩니다.
  - 사용 예: `Authorization: Bearer <Access Token>`

### Refresh Token

- **역할**: Access Token이 만료되었을 때 새로운 Access Token을 발급받는 데 사용됩니다.
- **특징**:
  - 만료 시간이 길거나 무제한으로 설정됩니다.
  - 보안을 위해 Refresh Token은 클라이언트에서 안전하게 저장해야 합니다(예: HttpOnly 쿠키).
  - Refresh Token이 유출되면 보안 위협이 될 수 있으므로 관리에 주의가 필요합니다.

### Access Token과 Refresh Token의 흐름

1. 클라이언트가 사용자 인증 정보를 통해 로그인 요청을 보냅니다.
2. 서버는 사용자 인증 후 Access Token과 Refresh Token을 생성하여 반환합니다.
3. 클라이언트는 Access Token을 사용해 인증된 요청을 서버에 보냅니다.
4. Access Token이 만료되면 Refresh Token을 서버에 보내 새로운 Access Token을 발급받습니다.

---

## 장점

1. **경량성**: 토큰 자체에 필요한 정보를 포함해 서버에서 상태를 저장할 필요가 없습니다.
2. **확장성**: 다양한 플랫폼과 언어에서 사용 가능.
3. **유연성**: 인증 외에도 정보 교환 목적으로 활용 가능.

---

## 주의사항

1. **암호화되지 않은 정보**: JWT는 기본적으로 인코딩만 되므로 민감한 정보를 포함하면 안 됩니다.
2. **HTTPS 사용**: 네트워크에서 안전하게 전송하기 위해 HTTPS를 사용해야 합니다.
3. **만료 시간 설정**: 적절한 만료 시간으로 토큰 남용을 방지해야 합니다.
4. **Token Revocation**: Refresh Token 유출을 방지하기 위해 블랙리스트 또는 토큰 관리 정책을 수립해야 합니다.
