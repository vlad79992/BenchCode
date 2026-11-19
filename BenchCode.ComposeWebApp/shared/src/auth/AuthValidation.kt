package ru.benchcode.auth

internal fun validateRegistration(input: RegisterRequest): Map<String, String> = buildMap {
    if (input.firstName.isBlank()) put("firstName", "Введите имя")
    if (input.lastName.isBlank()) put("lastName", "Введите фамилию")
    if (input.role == RegistrationRole.STUDENT && input.studyGroup.isNullOrBlank()) {
        put("studyGroup", "Укажите учебную группу")
    }
    if (input.email.isBlank()) {
        put("email", "Введите email")
    } else if (!isValidEmail(input.email)) {
        put("email", "Введите корректный email")
    }
    if (input.username.isBlank()) put("username", "Введите логин")
    if (input.password.length < 8) put("password", "Минимум 8 символов")
}

internal fun validateLogin(input: LoginRequest): Map<String, String> = buildMap {
    if (input.email.isBlank()) {
        put("email", "Введите email")
    } else if (!isValidEmail(input.email)) {
        put("email", "Введите корректный email")
    }
    if (input.password.isBlank()) put("password", "Введите пароль")
}

private fun isValidEmail(email: String): Boolean =
    email.trim().matches(Regex("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$"))
