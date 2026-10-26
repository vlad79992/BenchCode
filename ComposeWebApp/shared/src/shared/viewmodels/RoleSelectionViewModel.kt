package org.codebench.composewebapp.shared.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.stateIn
import org.codebench.composewebapp.data.UserPreferencesRepository

class RoleSelectionViewModel(
    private val repository: UserPreferencesRepository
) : ViewModel() {
    val authentification: StateFlow<Boolean> = repository.authentificationFlow.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5000),
        initialValue = false
    )

    val name: StateFlow<String> = repository.userNameFlow.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5000),
        initialValue = ""
    )
}