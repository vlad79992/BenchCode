package org.codebench.composewebapp.student

import io.ktor.client.HttpClient
import io.ktor.client.request.*
import io.ktor.client.statement.bodyAsText
import io.ktor.http.*
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import org.codebench.composewebapp.auth.AuthRepository
import org.codebench.composewebapp.getBaseUrl

@Serializable
internal data class CompetitionSummary(
    val id: String,
    val name: String,
    val description: String? = null,
    val status: Int? = null,
    val deadline: String? = null,
)

@Serializable internal data class TaskSummary(val id: Int, val name: String, val status: String)

@Serializable
internal data class PublicExample(
    val inputText: String? = null,
    val expectedOutputText: String? = null,
)

@Serializable
internal data class TaskDetails(
    val name: String,
    val statementMarkdown: String,
    val timeLimitMs: Int,
    val memoryLimitMb: Int,
    val allowedLanguages: List<Int>,
    val publicTests: List<PublicExample> = emptyList(),
)

@Serializable
internal data class Submission(
    val id: Int,
    val status: Int,
    val verdict: Int? = null,
    val timeMs: Int? = null,
    val memoryKb: Int? = null,
    val compilerOutput: String? = null,
    val isLate: Boolean = false,
    val testResults: List<SubmissionTestResult> = emptyList(),
)

@Serializable internal data class SubmissionTestResult(val verdict: Int)

@Serializable private data class JoinRequest(val code: String)

@Serializable private data class SubmitRequest(val language: Int, val code: String)

@Serializable private data class ApiError(val message: String? = null)

internal class CompetitionRepository(
    private val client: HttpClient,
    private val auth: AuthRepository,
) {
    private val json = Json { ignoreUnknownKeys = true }

    suspend fun join(code: String): CompetitionSummary =
        request("/competitions/join", json.encodeToString(JoinRequest(code)))

    suspend fun competition(id: String): CompetitionSummary =
        request("/competitions/${id.encodeURLPathPart()}")

    suspend fun tasks(id: String): List<TaskSummary> = request(path(id))

    suspend fun task(id: String, taskId: Int): TaskDetails = request("${path(id)}/$taskId")

    suspend fun submit(id: String, taskId: Int, language: Int, code: String): Submission =
        request("${path(id)}/$taskId/submit", json.encodeToString(SubmitRequest(language, code)))

    suspend fun submission(id: String, taskId: Int, submissionId: Int): Submission =
        request("${path(id)}/$taskId/submissions/$submissionId")

    private fun path(id: String) = "/competitions/${id.encodeURLPathPart()}/tasks"

    private suspend inline fun <reified T> request(path: String, body: String? = null): T {
        val token = auth.session?.accessToken ?: error("Войдите в BenchCode заново")
        val response =
            client.request("${getBaseUrl()}/api$path") {
                method = if (body == null) HttpMethod.Get else HttpMethod.Post
                bearerAuth(token)
                if (body != null) {
                    contentType(ContentType.Application.Json)
                    setBody(body)
                }
            }
        val text = response.bodyAsText()
        if (!response.status.isSuccess()) {
            val message = runCatching { json.decodeFromString<ApiError>(text).message }.getOrNull()
            error(
                message
                    ?: when (response.status.value) {
                        401 -> "Сессия закончилась. Войдите заново"
                        403 -> "Нет доступа к соревнованию"
                        404 -> "Соревнование или задача не найдены"
                        429 -> "Слишком много запросов. Попробуйте позже"
                        else -> "Сервер не смог обработать запрос"
                    }
            )
        }
        return json.decodeFromString(text)
    }
}
