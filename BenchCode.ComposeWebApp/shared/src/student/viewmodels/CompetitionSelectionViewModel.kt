package org.codebench.composewebapp.student.viewmodels

import androidx.compose.foundation.text.input.TextFieldState
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import org.codebench.composewebapp.student.CompetitionRepository

internal data class JoinState(
    val loading: Boolean = false,
    val error: String? = null,
    val competitionId: String? = null,
)

internal class CompetitionSelectionViewModel(private val repository: CompetitionRepository) :
    ViewModel() {
    val competitionId = TextFieldState()
    private val mutableState = MutableStateFlow(JoinState())
    val state = mutableState.asStateFlow()

    fun join() {
        if (state.value.loading) return
        val code = competitionId.text.toString().trim().uppercase()
        if (!code.matches(Regex("[A-Z0-9]{6}"))) {
            mutableState.value = JoinState(error = "Введите код из шести символов")
            return
        }
        mutableState.value = JoinState(loading = true)
        viewModelScope.launch {
            try {
                mutableState.value = JoinState(competitionId = repository.join(code).id)
            } catch (cancelled: CancellationException) {
                throw cancelled
            } catch (failure: Exception) {
                mutableState.value =
                    JoinState(error = failure.message ?: "Не удалось открыть соревнование")
            }
        }
    }

    fun consumeNavigation() {
        mutableState.value = JoinState()
    }
}
