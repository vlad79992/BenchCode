package org.codebench.composewebapp.student.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import org.codebench.composewebapp.student.CompetitionRepository
import org.codebench.composewebapp.student.CompetitionSummary
import org.codebench.composewebapp.student.TaskSummary

internal data class CompetitionState(
    val competition: CompetitionSummary? = null,
    val tasks: List<TaskSummary> = emptyList(),
    val loading: Boolean = true,
    val error: String? = null,
)

internal class CompetitionMainScreenViewModel(
    private val competitionId: String,
    private val repository: CompetitionRepository,
) : ViewModel() {
    private val mutableState = MutableStateFlow(CompetitionState())
    val state = mutableState.asStateFlow()

    init {
        load()
    }

    fun load() {
        mutableState.value = CompetitionState()
        viewModelScope.launch {
            try {
                val competition = repository.competition(competitionId)
                mutableState.value =
                    CompetitionState(competition, repository.tasks(competitionId), loading = false)
            } catch (cancelled: CancellationException) {
                throw cancelled
            } catch (failure: Exception) {
                mutableState.value =
                    CompetitionState(
                        loading = false,
                        error = failure.message ?: "Не удалось загрузить задачи",
                    )
            }
        }
    }
}
