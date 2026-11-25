package org.codebench.composewebapp.auth

import io.ktor.client.HttpClient
import io.ktor.client.request.forms.submitForm
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.client.statement.bodyAsText
import io.ktor.http.ContentType
import io.ktor.http.contentType
import io.ktor.http.isSuccess
import io.ktor.http.parameters
import kotlinx.serialization.json.Json
import org.codebench.composewebapp.getBaseUrl

internal class AuthFailure(message: String) : Exception(message)

internal class AuthRepository(private val client: HttpClient) {
    private val json = Json { ignoreUnknownKeys = true }
    var session: AuthSession? = null
        private set

    suspend fun login(email: String, password: String) {
        val response =
            client.submitForm(
                url = "${getBaseUrl()}/connect/token",
                formParameters =
                    parameters {
                        append("grant_type", "password")
                        append("client_id", "compose-web-app")
                        append("scope", "benchcode-api")
                        append("username", email)
                        append("login_by_email", "true")
                        append("password", password)
                    },
            )
        if (!response.status.isSuccess()) {
            val message =
                if (response.status.value in 400..401) {
                    "Неверный электронный адрес или пароль"
                } else {
                    "Не удалось войти. Попробуйте ещё раз"
                }
            throw AuthFailure(message)
        }
        session = json.decodeFromString<AuthSession>(response.bodyAsText())
    }

    suspend fun register(request: RegisterRequest) {
        val response =
            client.post("${getBaseUrl()}/api/users/register") {
                contentType(ContentType.Application.Json)
                setBody(json.encodeToString(request))
            }
        when (response.status.value) {
            201 ->
                try {
                    login(request.email, request.password)
                } catch (e: kotlinx.coroutines.CancellationException) {
                    throw e
                } catch (_: Exception) {
                    throw AuthFailure("Аккаунт создан. Вернитесь на экран входа и войдите")
                }
            409 -> throw AuthFailure("Этот электронный адрес уже используется")
            400 -> throw AuthFailure("Проверьте данные и требования к паролю")
            else -> throw AuthFailure("Не удалось зарегистрироваться. Попробуйте ещё раз")
        }
    }
}
