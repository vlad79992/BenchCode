package ru.benchcode.auth

enum class RegistrationRole {
    STUDENT,
    TEACHER
}

data class RegisterRequest(
    val firstName: String,
    val lastName: String,
    val role: RegistrationRole,
    val studyGroup: String?,
    val email: String,
    val username: String,
    val password: String
)

data class LoginRequest(
    val email: String,
    val password: String
)

data class AuthSession(
    val accessToken: String,
    val refreshToken: String? = null
)

interface AuthRepository {
    suspend fun register(request: RegisterRequest): AuthSession
    suspend fun login(request: LoginRequest): AuthSession
}

class AuthApiException(
    val fieldErrors: Map<String, String> = emptyMap(),
    override val message: String = "Не удалось выполнить запрос. Попробуйте ещё раз."
) : Exception(message)
