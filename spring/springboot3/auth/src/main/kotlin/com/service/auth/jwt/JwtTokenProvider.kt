package com.service.auth.jwt

import io.jsonwebtoken.Claims
import io.jsonwebtoken.Jwts
import io.jsonwebtoken.io.Decoders
import io.jsonwebtoken.security.Keys
import org.springframework.beans.factory.annotation.Value
import org.springframework.stereotype.Component
import java.util.Date
import javax.crypto.SecretKey

@Component
class JwtTokenProvider(
    @Value("\${jwt.secret}") secretKey: String,
    @Value("\${jwt.access-expiration}") private val accessTokenValidity: Long,
    @Value("\${jwt.refresh-expiration}") private val refreshTokenValidity: Long
) {
    private val key: SecretKey = Keys.hmacShaKeyFor(Decoders.BASE64.decode(secretKey))

    fun generateAccessToken(subject: String): String {
        return Jwts.builder()
            .subject(subject)
            .issuedAt(Date())
            .expiration(Date(System.currentTimeMillis() + accessTokenValidity))
            .signWith(key, Jwts.SIG.HS256)
            .compact()
    }

    fun generateRefreshToken(): String {
        return Jwts.builder()
            .issuedAt(Date())
            .expiration(Date(System.currentTimeMillis() + refreshTokenValidity))
            .signWith(key, Jwts.SIG.HS256)
            .compact()
    }

    fun validateToken(token: String): Claims {
        return Jwts.parser()
            .verifyWith(key)
            .build()
            .parseSignedClaims(token)
            .payload
    }

    fun getSubject(token: String): String {
        return validateToken(token).subject
    }
}
