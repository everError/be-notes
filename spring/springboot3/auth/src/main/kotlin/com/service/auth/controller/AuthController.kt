package com.service.auth.controller

import com.service.auth.dto.LoginRequest
import com.service.auth.dto.TokenResponse
import com.service.auth.jwt.JwtTokenProvider
import jakarta.servlet.http.HttpServletResponse
import org.springframework.http.HttpStatus
import org.springframework.http.ResponseCookie
import org.springframework.http.ResponseEntity
import org.springframework.web.bind.annotation.PostMapping
import org.springframework.web.bind.annotation.RequestBody
import org.springframework.web.bind.annotation.RequestMapping
import org.springframework.web.bind.annotation.RestController
import java.time.Duration

@RestController
@RequestMapping("/auth")
class AuthController(
    private val jwtTokenProvider: JwtTokenProvider
) {

    @PostMapping("/login")
    fun login(
        @RequestBody request: LoginRequest,
        response: HttpServletResponse
    ): ResponseEntity<TokenResponse> {
        // 🔐 인증 로직 (임시로 username 고정)
        if (request.username != "admin" || request.password != "1234")
            return ResponseEntity.status(HttpStatus.UNAUTHORIZED).build()

        val accessToken = jwtTokenProvider.generateAccessToken(request.username)
        val refreshToken = jwtTokenProvider.generateRefreshToken()

        // 🍪 쿠키에 RefreshToken 저장
        val cookie = ResponseCookie.from("refreshToken", refreshToken)
            .httpOnly(true)
            .secure(true)
            .path("/")
            .maxAge(Duration.ofDays(7))
            .sameSite("Strict")
            .build()

        response.addHeader("Set-Cookie", cookie.toString())

        return ResponseEntity.ok(TokenResponse(accessToken))
    }
}