package org.codebench.composewebapp.auth

private val emailPattern = Regex("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$")

internal fun emailError(email: String): String? =
    if (emailPattern.matches(email.trim())) null else "Введите корректный электронный адрес"

internal fun passwordError(password: String): String? =
    if (password.length >= 8) null else "Пароль должен содержать не менее 8 символов"
