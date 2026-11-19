package ru.benchcode.auth

import kotlinx.coroutines.delay

class MockAuthRepository : AuthRepository {
    override suspend fun register(request: RegisterRequest): AuthSession {
        delay(650)
        if (request.email.equals("taken@benchcode.ru", ignoreCase = true)) {
            throw AuthApiException(
                fieldErrors = mapOf("email" to "Этот email уже зарегистрирован"),
                message = "Проверьте данные регистрации"
            )
        }
        if (request.username.equals("taken", ignoreCase = true)) {
            throw AuthApiException(
                fieldErrors = mapOf("username" to "Этот логин уже занят"),
                message = "Проверьте данные регистрации"
            )
        }
        if (request.email.equals("server@benchcode.ru", ignoreCase = true)) {
            error("Server is unavailable")
        }
        return AuthSession(accessToken = "local-access-token", refreshToken = "local-refresh-token")
    }

    override suspend fun login(request: LoginRequest): AuthSession {
        delay(650)
        if (request.email.equals("server@benchcode.ru", ignoreCase = true)) {
            error("Server is unavailable")
        }
        if (!request.email.equals("demo@benchcode.ru", ignoreCase = true) || request.password != "12345678") {
            throw AuthApiException(message = "Неверный email или пароль")
        }
        return AuthSession(accessToken = "local-access-token", refreshToken = "local-refresh-token")
    }
}
