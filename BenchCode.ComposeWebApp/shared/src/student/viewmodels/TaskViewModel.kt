package org.codebench.composewebapp.student.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import org.codebench.composewebapp.student.CompetitionRepository
import org.codebench.composewebapp.student.Submission
import org.codebench.composewebapp.student.TaskDetails

internal data class TaskState(
    val task: TaskDetails? = null,
    val language: Int? = null,
    val code: String = "",
    val result: Submission? = null,
    val busy: Boolean = false,
    val error: String? = null,
)

internal class TaskViewModel(
    private val competitionId: String,
    private val taskId: Int,
    private val repository: CompetitionRepository,
) : ViewModel() {
    private val mutableState = MutableStateFlow(TaskState())
    val state = mutableState.asStateFlow()

    init {
        load()
    }

    fun load() = runRequest {
        val task = repository.task(competitionId, taskId)
        mutableState.update { it.copy(task = task, language = task.allowedLanguages.firstOrNull()) }
    }

    fun edit(code: String) {
        mutableState.update { it.copy(code = code) }
    }

    fun select(language: Int) {
        mutableState.update { it.copy(language = language) }
    }

    fun submit() {
        val current = state.value
        val language = current.language ?: return
        if (current.code.isBlank() || current.busy) return
        runRequest {
            val submitted = repository.submit(competitionId, taskId, language, current.code)
            mutableState.update { it.copy(result = submitted) }
            refreshUntilFinished(submitted)
        }
    }

    fun refresh() {
        val result = state.value.result ?: return
        runRequest { refreshUntilFinished(result) }
    }

    private suspend fun refreshUntilFinished(initial: Submission) {
        var result = initial
        repeat(15) {
            if (result.status != 0) return
            delay(2000)
            result = repository.submission(competitionId, taskId, result.id)
            mutableState.update { it.copy(result = result) }
        }
    }

    private fun runRequest(action: suspend () -> Unit) {
        if (state.value.busy) return
        mutableState.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            try {
                action()
            } catch (cancelled: CancellationException) {
                throw cancelled
            } catch (failure: Exception) {
                mutableState.update {
                    it.copy(error = failure.message ?: "Не удалось связаться с сервером")
                }
            } finally {
                mutableState.update { it.copy(busy = false) }
            }
        }
    }
}
