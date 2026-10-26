package org.codebench.composewebapp.student.viewmodels

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.launch
import org.codebench.composewebapp.data.UserPreferencesRepository

class AuthentificationViewModel(
    private val repository: UserPreferencesRepository
) : ViewModel() {
    var login: Boolean? by mutableStateOf(null)

    fun updateName(newName: String) {
        viewModelScope.launch {
            repository.saveUserName(newName)
        }
    }
}