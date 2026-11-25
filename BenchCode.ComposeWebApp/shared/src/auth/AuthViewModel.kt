package org.codebench.composewebapp.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import org.codebench.composewebapp.navigation.SharedRoute

internal enum class AuthField {
    FirstName,
    LastName,
    Email,
    Password,
    Confirmation,
}

internal data class AuthState(
    val fields: Map<AuthField, String> = emptyMap(),
    val errors: Map<AuthField, String> = emptyMap(),
    val message: String? = null,
    val submitting: Boolean = false,
    val authenticated: Boolean = false,
) {
    operator fun get(field: AuthField): String = fields[field].orEmpty()
}

internal class AuthViewModel(private val repository: AuthRepository) : ViewModel() {
    private val mutableState = MutableStateFlow(AuthState())
    val state = mutableState.asStateFlow()

    fun edit(field: AuthField, value: String) {
        mutableState.update {
            it.copy(
                fields = it.fields + (field to value),
                errors = it.errors - field,
                message = null,
            )
        }
    }

    fun validateProfile(): Boolean {
        val current = state.value
        val errors = buildMap {
            if (current[AuthField.FirstName].isBlank()) put(AuthField.FirstName, "Введите имя")
            if (current[AuthField.LastName].isBlank()) put(AuthField.LastName, "Введите фамилию")
            emailError(current[AuthField.Email])?.let { put(AuthField.Email, it) }
        }
        mutableState.update { it.copy(errors = errors, message = null) }
        return errors.isEmpty()
    }

    fun login() {
        val current = state.value
        val email = current[AuthField.Email].trim()
        val password = current[AuthField.Password]
        val errors = buildMap {
            emailError(email)?.let { put(AuthField.Email, it) }
            passwordError(password)?.let { put(AuthField.Password, it) }
        }
        submit(errors) { repository.login(email, password) }
    }

    fun register(profile: SharedRoute.RegistrationPassword) {
        val current = state.value
        val password = current[AuthField.Password]
        val errors = buildMap {
            passwordError(password)?.let { put(AuthField.Password, it) }
            if (password != current[AuthField.Confirmation]) {
                put(AuthField.Confirmation, "Пароли не совпадают")
            }
        }
        submit(errors) {
            repository.register(
                RegisterRequest(profile.firstName, profile.lastName, profile.email, password)
            )
        }
    }

    private fun submit(errors: Map<AuthField, String>, action: suspend () -> Unit) {
        if (state.value.submitting || state.value.authenticated) return
        mutableState.update {
            it.copy(errors = errors, message = null, submitting = errors.isEmpty())
        }
        if (errors.isNotEmpty()) return
        viewModelScope.launch {
            try {
                action()
                mutableState.value = AuthState(authenticated = true)
            } catch (cancelled: CancellationException) {
                throw cancelled
            } catch (failure: AuthFailure) {
                mutableState.update { it.copy(message = failure.message) }
            } catch (_: Exception) {
                mutableState.update {
                    it.copy(message = "Не удалось связаться с сервером. Попробуйте ещё раз")
                }
            } finally {
                mutableState.update { it.copy(submitting = false) }
            }
        }
    }
}
