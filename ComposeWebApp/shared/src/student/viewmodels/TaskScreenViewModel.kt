package org.codebench.composewebapp.student.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import org.codebench.composewebapp.data.Language
import org.codebench.composewebapp.data.Task
import org.codebench.composewebapp.data.UserPreferencesRepository

class TaskScreenViewModel(
    val taskId: String,
    private val repository: UserPreferencesRepository
) : ViewModel(){
    val task: Task? = getTask(taskId)

    private fun getTask(taskId: String): Task? {
        return when (taskId) {
            "1" -> Task(
                Id = "1",
                Name = "Вывод строки",
                Description = "Выведите строку \"Hello, world\".",
                Languages = Language.entries
            )
            "2" -> Task(
                Id = "2",
                Name = "Сложение",
                Description = "На ввод подаются два числа, программа должна вывести их сумму.",
                Languages = Language.entries
            )
            else -> null
        }
    }

    val name: StateFlow<String> = repository.userNameFlow.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5000),
        initialValue = ""
    )

    val authentification: StateFlow<Boolean> = repository.authentificationFlow.stateIn(
        scope = viewModelScope,
        started = SharingStarted.WhileSubscribed(5000),
        initialValue = false
    )

    fun updateName(newName: String) {
        viewModelScope.launch {
            repository.saveUserName(newName)
        }
    }
}