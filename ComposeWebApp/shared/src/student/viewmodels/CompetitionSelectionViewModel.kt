package org.codebench.composewebapp.student.viewmodels

import androidx.compose.foundation.text.input.TextFieldState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel

class CompetitionSelectionViewModel : ViewModel() {
    val competitionId = TextFieldState()
    var isError by mutableStateOf(false)
        private set

    fun validateAndGetId(): String? {
        val currentText = competitionId.text.toString().trim()
        val isValid = currentText == "12345"

        isError = !isValid
        return if (isValid) currentText else null
    }
}