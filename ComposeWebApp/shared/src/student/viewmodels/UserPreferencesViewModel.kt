package org.codebench.composewebapp.student.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import org.codebench.composewebapp.data.UserPreferencesRepository

class UserPreferencesViewModel(
    private val repository: UserPreferencesRepository
) : ViewModel() {

    val counter: StateFlow<Int> = repository.counterFlow.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5000),
        initialValue = 0
    )

    val userName: StateFlow<String> = repository.userNameFlow.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5000),
        initialValue = ""
    )

    fun incrementCounter() {
        viewModelScope.launch {
            repository.incrementCounter()
        }
    }

    fun saveUserName(name: String) {
        viewModelScope.launch {
            repository.saveUserName(name)
        }
    }
}
