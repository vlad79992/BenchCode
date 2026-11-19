package ru.benchcode.auth

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch

private val Navy = Color(0xFF17243D)
private val Orange = Color(0xFFFF5A0A)
private val Cream = Color(0xFFF4F1E8)
private val Ink = Color(0xFF111111)
private val ErrorRed = Color(0xFFC93030)
private val Muted = Color(0xFF686868)
private val Border = Color(0xFFC8C4BE)

/** Role is a registration/profile attribute. Contest Owner/Participant is separate. */
private enum class AuthMode { LOGIN, REGISTER }

@Composable
fun BenchCodeAuthScreen(
    repository: AuthRepository,
    onAuthenticated: (AuthSession) -> Unit,
    modifier: Modifier = Modifier,
    initialRegistration: Boolean = false
) {
    var mode by remember { mutableStateOf(if (initialRegistration) AuthMode.REGISTER else AuthMode.LOGIN) }
    var firstName by remember { mutableStateOf("") }
    var lastName by remember { mutableStateOf("") }
    var role by remember { mutableStateOf(RegistrationRole.STUDENT) }
    var studyGroup by remember { mutableStateOf("") }
    var username by remember { mutableStateOf("") }
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var errors by remember { mutableStateOf<Map<String, String>>(emptyMap()) }
    var serverMessage by remember { mutableStateOf<String?>(null) }
    var submitting by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    fun submit() {
        if (submitting) return
        val registration = RegisterRequest(
            firstName = firstName.trim(),
            lastName = lastName.trim(),
            role = role,
            studyGroup = studyGroup.trim().takeIf { role == RegistrationRole.STUDENT },
            email = email.trim(),
            username = username.trim(),
            password = password
        )
        val login = LoginRequest(email = email.trim(), password = password)
        errors = if (mode == AuthMode.REGISTER) {
            validateRegistration(registration)
        } else {
            validateLogin(login)
        }
        serverMessage = null
        if (errors.isNotEmpty()) return
        submitting = true
        scope.launch {
            try {
                val session = if (mode == AuthMode.REGISTER) {
                    repository.register(registration)
                } else {
                    repository.login(login)
                }
                onAuthenticated(session)
            } catch (cancel: CancellationException) {
                throw cancel
            } catch (ex: AuthApiException) {
                errors = ex.fieldErrors
                serverMessage = ex.message
            } catch (_: Exception) {
                serverMessage = "Нет соединения с сервером. Попробуйте позже."
            } finally {
                submitting = false
            }
        }
    }

    BoxWithConstraints(modifier.fillMaxSize().background(Cream)) {
        val wide = maxWidth >= 900.dp
        Row(Modifier.fillMaxSize()) {
            if (wide) {
                Box(
                    Modifier.weight(1.05f).fillMaxHeight().background(Navy).padding(46.dp),
                    contentAlignment = Alignment.CenterStart
                ) {
                    Column(verticalArrangement = Arrangement.spacedBy(22.dp)) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Box(Modifier.background(Orange).padding(14.dp)) {
                                Text("</>", color = Ink, fontSize = 22.sp, fontWeight = FontWeight.Bold)
                            }
                            Spacer(Modifier.width(16.dp))
                            Text("Bench", color = Color.White, fontSize = 44.sp, fontWeight = FontWeight.Bold)
                            Text("Code", color = Orange, fontSize = 44.sp, fontWeight = FontWeight.Bold)
                        }
                        Text(
                            "Решай задачи, запускай код в единой среде и сравнивай эффективность алгоритмов без ручных таблиц.",
                            color = Color(0xFFCBD1DF), fontSize = 17.sp, lineHeight = 26.sp
                        )
                    }
                }
            }
            Column(
                modifier = Modifier.weight(1f).fillMaxHeight().verticalScroll(rememberScrollState())
                    .padding(horizontal = if (wide) 54.dp else 22.dp, vertical = 32.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                Column(Modifier.widthIn(max = 520.dp)) {
                    if (!wide) Text("BenchCode", color = Navy, fontSize = 26.sp, fontWeight = FontWeight.Bold)
                    Text(
                        if (mode == AuthMode.REGISTER) "Создать аккаунт" else "Войти",
                        color = Ink, fontSize = 36.sp, lineHeight = 44.sp, fontWeight = FontWeight.Bold
                    )
                    Spacer(Modifier.height(8.dp))
                    Text(
                        if (mode == AuthMode.REGISTER) "Заполните данные для регистрации в BenchCode"
                        else "Продолжите с того места, где остановились.",
                        color = Muted, fontSize = 13.sp
                    )
                    Spacer(Modifier.height(26.dp))
                    Row(Modifier.fillMaxWidth()) {
                        AuthTab("Вход", mode == AuthMode.LOGIN, Modifier.weight(1f)) {
                            if (!submitting) {
                                mode = AuthMode.LOGIN
                                errors = emptyMap()
                                serverMessage = null
                            }
                        }
                        AuthTab("Регистрация", mode == AuthMode.REGISTER, Modifier.weight(1f)) {
                            if (!submitting) {
                                mode = AuthMode.REGISTER
                                errors = emptyMap()
                                serverMessage = null
                            }
                        }
                    }
                    Spacer(Modifier.height(24.dp))
                    if (mode == AuthMode.REGISTER) {
                        Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                            Column(Modifier.weight(1f)) {
                                AuthField("Имя *", firstName, { firstName = it; errors -= "firstName" }, errors["firstName"], "Имя")
                            }
                            Column(Modifier.weight(1f)) {
                                AuthField("Фамилия *", lastName, { lastName = it; errors -= "lastName" }, errors["lastName"], "Фамилия")
                            }
                        }
                        Spacer(Modifier.height(12.dp))
                        Text("Роль *", color = Ink, fontSize = 12.sp, fontWeight = FontWeight.Bold)
                        Spacer(Modifier.height(7.dp))
                        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            RoleButton("Студент", role == RegistrationRole.STUDENT, Modifier.weight(1f)) {
                                role = RegistrationRole.STUDENT
                            }
                            RoleButton("Преподаватель", role == RegistrationRole.TEACHER, Modifier.weight(1f)) {
                                role = RegistrationRole.TEACHER
                                errors -= "studyGroup"
                            }
                        }
                        Spacer(Modifier.height(12.dp))
                        if (role == RegistrationRole.STUDENT) {
                            AuthField("Учебная группа *", studyGroup, { studyGroup = it; errors -= "studyGroup" }, errors["studyGroup"], "Например, ИВТ01/24б")
                            Spacer(Modifier.height(12.dp))
                        }
                    }
                    AuthField(
                        label = "Email *",
                        value = email,
                        onValueChange = { email = it; errors -= "email" },
                        error = errors["email"],
                        placeholder = "name@university.ru",
                        keyboardType = KeyboardType.Email
                    )
                    Spacer(Modifier.height(12.dp))
                    if (mode == AuthMode.REGISTER) {
                        AuthField("Логин *", username, { username = it; errors -= "username" }, errors["username"], "Например, alex_ivanova")
                        Spacer(Modifier.height(12.dp))
                    }
                    AuthField("Пароль *", password, { password = it; errors -= "password" }, errors["password"], "Не менее 8 символов", password = true)
                    if (mode == AuthMode.REGISTER) {
                        Spacer(Modifier.height(6.dp))
                        Text("Минимальная длина пароля — 8 символов.", color = Muted, fontSize = 11.sp)
                    }
                    serverMessage?.let { message ->
                        Spacer(Modifier.height(16.dp))
                        Text(
                            message, color = ErrorRed, fontSize = 13.sp,
                            modifier = Modifier.fillMaxWidth().background(Color(0xFFFCE8E6)).padding(14.dp)
                        )
                    }
                    Spacer(Modifier.height(24.dp))
                    Button(
                        onClick = { submit() }, enabled = !submitting,
                        modifier = Modifier.fillMaxWidth().height(54.dp),
                        shape = RoundedCornerShape(0.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = Orange, contentColor = Ink)
                    ) {
                        if (submitting) CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp, color = Ink)
                        else Text(if (mode == AuthMode.REGISTER) "Зарегистрироваться  →" else "Войти  →", fontWeight = FontWeight.Bold)
                    }
                }
            }
        }
    }
}

@Composable
private fun AuthTab(label: String, selected: Boolean, modifier: Modifier, onClick: () -> Unit) {
    Column(modifier.clickable(onClick = onClick), horizontalAlignment = Alignment.CenterHorizontally) {
        Text(label, fontSize = 13.sp, color = if (selected) Ink else Muted, fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(vertical = 12.dp))
        Box(Modifier.fillMaxWidth().height(if (selected) 3.dp else 1.dp)
            .background(if (selected) Orange else Border))
    }
}

@Composable
private fun RoleButton(label: String, selected: Boolean, modifier: Modifier, onClick: () -> Unit) {
    OutlinedButton(
        onClick = onClick, modifier = modifier.height(46.dp),
        shape = RoundedCornerShape(0.dp),
        border = BorderStroke(1.dp, if (selected) Ink else Border),
        colors = ButtonDefaults.outlinedButtonColors(containerColor = if (selected) Orange else Color.White, contentColor = Ink),
        contentPadding = PaddingValues(horizontal = 6.dp)
    ) { Text(label, fontSize = 12.sp, fontWeight = if (selected) FontWeight.Bold else FontWeight.Normal) }
}

@Composable
private fun AuthField(
    label: String,
    value: String,
    onValueChange: (String) -> Unit,
    error: String?,
    placeholder: String,
    keyboardType: KeyboardType = KeyboardType.Text,
    password: Boolean = false
) {
    Column {
        Text(label, fontSize = 12.sp, color = Ink, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        OutlinedTextField(
            value = value, onValueChange = onValueChange, modifier = Modifier.fillMaxWidth(),
            singleLine = true, isError = error != null,
            placeholder = { Text(placeholder, fontSize = 13.sp) },
            visualTransformation = if (password) PasswordVisualTransformation() else androidx.compose.ui.text.input.VisualTransformation.None,
            keyboardOptions = KeyboardOptions(keyboardType = if (password) KeyboardType.Password else keyboardType),
            colors = OutlinedTextFieldDefaults.colors(
                focusedBorderColor = Orange, unfocusedBorderColor = Border,
                errorBorderColor = ErrorRed, focusedContainerColor = Color.White,
                unfocusedContainerColor = Color.White, errorContainerColor = Color.White
            ),
            shape = RoundedCornerShape(0.dp)
        )
        if (error != null) {
            Spacer(Modifier.height(4.dp))
            Text(error, color = ErrorRed, fontSize = 11.sp)
        }
    }
}
