package org.codebench.composewebapp.shared.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import org.codebench.composewebapp.data.UserPreferencesRepository

class SettingsViewModel(
    private val repository: UserPreferencesRepository
) : ViewModel() {
    val name: StateFlow<String> = repository.userNameFlow.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5000),
        initialValue = ""
    )

    val counter: StateFlow<Int> = repository.counterFlow.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5000),
        initialValue = 0
    )

    val authentification: StateFlow<Boolean> = repository.authentificationFlow.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5000),
        initialValue = false
    )

    fun incrementCounter() {
        viewModelScope.launch {
            repository.incrementCounter()
        }
    }

    fun updateName(newName: String) {
        viewModelScope.launch {
            repository.saveUserName(newName)
        }
    }

    fun updateAuthentification(value: Boolean) {
        viewModelScope.launch {
            repository.setAuthentification(value)
        }
    }
}
