package org.codebench.composewebapp.auth

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import org.codebench.composewebapp.navigation.LocalNavBackStack
import org.codebench.composewebapp.navigation.SharedRoute
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.resources.*
import org.codebench.composewebapp.shared.submitOnEnter
import org.jetbrains.compose.resources.DrawableResource
import org.jetbrains.compose.resources.painterResource
import org.koin.compose.viewmodel.koinViewModel

@Composable
internal fun LoginScreen() {
    val viewModel = koinViewModel<AuthViewModel>()
    val state by viewModel.state.collectAsStateWithLifecycle()
    val backstack = LocalNavBackStack.current
    NavigateAfterAuth(state.authenticated)
    AuthLayout("Вход", state) {
        AuthInput(
            "Электронный адрес",
            AuthField.Email,
            state,
            viewModel,
            KeyboardType.Email,
            viewModel::login,
        )
        AuthInput(
            "Пароль",
            AuthField.Password,
            state,
            viewModel,
            KeyboardType.Password,
            viewModel::login,
        )
        Row {
            TextButton(
                enabled = !state.submitting,
                onClick = { backstack.add(SharedRoute.Registration) },
            ) {
                Text("Регистрация")
            }
            Button(enabled = !state.submitting, onClick = viewModel::login) { Text("Вход") }
        }
    }
}

@Composable
internal fun RegistrationScreen() {
    val viewModel = koinViewModel<AuthViewModel>()
    val state by viewModel.state.collectAsStateWithLifecycle()
    val backstack = LocalNavBackStack.current
    fun continueRegistration() {
        if (backstack.lastOrNull() == SharedRoute.Registration && viewModel.validateProfile()) {
            val current = viewModel.state.value
            backstack.add(
                SharedRoute.RegistrationPassword(
                    current[AuthField.FirstName].trim(),
                    current[AuthField.LastName].trim(),
                    current[AuthField.Email].trim(),
                )
            )
        }
    }
    AuthLayout("Регистрация", state) {
        AuthInput(
            "Фамилия",
            AuthField.LastName,
            state,
            viewModel,
            onSubmit = ::continueRegistration,
        )
        AuthInput("Имя", AuthField.FirstName, state, viewModel, onSubmit = ::continueRegistration)
        AuthInput(
            "Электронный адрес",
            AuthField.Email,
            state,
            viewModel,
            KeyboardType.Email,
            ::continueRegistration,
        )
        Button(onClick = ::continueRegistration) { Text("Далее") }
        TextButton(onClick = { backstack.removeAt(backstack.lastIndex) }) { Text("Назад") }
    }
}

@Composable
internal fun RegistrationPasswordScreen(profile: SharedRoute.RegistrationPassword) {
    val viewModel = koinViewModel<AuthViewModel>()
    val state by viewModel.state.collectAsStateWithLifecycle()
    val backstack = LocalNavBackStack.current
    NavigateAfterAuth(state.authenticated)
    val submit = { viewModel.register(profile) }
    AuthLayout("Создайте пароль", state) {
        AuthInput("Пароль", AuthField.Password, state, viewModel, KeyboardType.Password, submit)
        AuthInput(
            "Повторите пароль",
            AuthField.Confirmation,
            state,
            viewModel,
            KeyboardType.Password,
            submit,
        )
        Button(enabled = !state.submitting, onClick = submit) {
            Text("Далее")
        }
        TextButton(
            enabled = !state.submitting,
            onClick = { backstack.removeAt(backstack.lastIndex) },
        ) {
            Text("Назад")
        }
    }
}

@Composable
private fun NavigateAfterAuth(authenticated: Boolean) {
    val backstack = LocalNavBackStack.current
    LaunchedEffect(authenticated) {
        if (authenticated) {
            backstack.add(StudentRoute.CompetitionSelection)
            while (backstack.size > 1) backstack.removeAt(0)
        }
    }
}

@Composable
private fun AuthInput(
    label: String,
    field: AuthField,
    state: AuthState,
    viewModel: AuthViewModel,
    keyboardType: KeyboardType = KeyboardType.Text,
    onSubmit: () -> Unit,
) {
    val error = state.errors[field]
    OutlinedTextField(
        value = state[field],
        onValueChange = { viewModel.edit(field, it) },
        modifier = Modifier.widthIn(max = 344.dp).fillMaxWidth().submitOnEnter(onSubmit),
        label = { Text(label) },
        singleLine = true,
        enabled = !state.submitting,
        isError = error != null,
        supportingText = error?.let { message -> { Text(message) } },
        visualTransformation =
            when (field) {
                AuthField.Password,
                AuthField.Confirmation -> PasswordVisualTransformation()
                else -> VisualTransformation.None
            },
        keyboardOptions = KeyboardOptions(keyboardType = keyboardType, imeAction = ImeAction.Done),
        keyboardActions = KeyboardActions(onDone = { onSubmit() }),
    )
}

@Composable
private fun AuthLayout(
    title: String,
    state: AuthState,
    content: @Composable ColumnScope.() -> Unit,
) {
    Surface(Modifier.fillMaxSize(), color = MaterialTheme.colorScheme.background) {
        BoxWithConstraints(Modifier.fillMaxSize()) {
            val wide = maxWidth >= 900.dp
            val availableHeight = maxHeight
            val panelHeight =
                if (wide) (availableHeight - 112.dp).coerceIn(480.dp, 720.dp) else 480.dp
            AuthCloudBackground(Modifier.matchParentSize())
            Column(
                Modifier.fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(
                        horizontal = if (wide) 48.dp else 20.dp,
                        vertical = 16.dp,
                    ),
                verticalArrangement = Arrangement.spacedBy(24.dp),
            ) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(12.dp),
                ) {
                    Image(painterResource(Res.drawable.auth_benchcube), null, Modifier.size(44.dp))
                    Text(
                        "BenchCode",
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Bold,
                    )
                }
                Row(
                    Modifier.fillMaxWidth()
                        .heightIn(min = (availableHeight - 120.dp).coerceAtLeast(460.dp)),
                    horizontalArrangement = Arrangement.spacedBy(36.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    if (wide) {
                        AuthIllustration(
                            maxArtworkHeight = (availableHeight - 310.dp).coerceIn(180.dp, 520.dp),
                            modifier = Modifier.weight(1.25f).heightIn(min = panelHeight),
                        )
                    }
                    Box(Modifier.weight(1f), contentAlignment = Alignment.Center) {
                        ElevatedCard(
                            Modifier.widthIn(max = if (wide) 480.dp else 380.dp),
                            shape = RoundedCornerShape(if (wide) 36.dp else 24.dp),
                            colors =
                                CardDefaults.elevatedCardColors(
                                    containerColor = MaterialTheme.colorScheme.surfaceContainerLow
                                ),
                        ) {
                            Column(
                                Modifier.width(if (wide) 480.dp else 380.dp)
                                    .heightIn(min = panelHeight)
                                    .padding(24.dp),
                                horizontalAlignment = Alignment.CenterHorizontally,
                                verticalArrangement =
                                    Arrangement.spacedBy(12.dp, Alignment.CenterVertically),
                            ) {
                                Row(
                                    verticalAlignment = Alignment.CenterVertically,
                                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                                ) {
                                    Image(
                                        painterResource(Res.drawable.auth_benchcube),
                                        null,
                                        Modifier.size(26.dp),
                                    )
                                    Text(
                                        "BenchCode",
                                        style = MaterialTheme.typography.titleMedium,
                                        fontWeight = FontWeight.Bold,
                                    )
                                }
                                Text(
                                    title,
                                    style = MaterialTheme.typography.headlineLarge,
                                    fontWeight = FontWeight.Bold,
                                )
                                content()
                                state.message?.let {
                                    Text(it, color = MaterialTheme.colorScheme.error)
                                }
                                if (state.submitting)
                                    CircularProgressIndicator(Modifier.size(24.dp))
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun AuthIllustration(maxArtworkHeight: Dp, modifier: Modifier = Modifier) {
    Surface(
        modifier,
        shape = RoundedCornerShape(36.dp),
        color = MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.82f),
    ) {
        Column(
            Modifier.fillMaxWidth().padding(40.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(24.dp, Alignment.CenterVertically),
        ) {
            BoxWithConstraints(
                Modifier.widthIn(max = maxArtworkHeight * (486.4f / 520f))
                    .fillMaxWidth()
                    .aspectRatio(486.4f / 520f)
            ) {
                val width = maxWidth
                val height = maxHeight
                @Composable
                fun art(
                    resource: DrawableResource,
                    x: Float,
                    y: Float,
                    w: Float,
                    h: Float,
                    angle: Float = 0f,
                ) {
                    Image(
                        painterResource(resource),
                        null,
                        Modifier.offset(width * x, height * y)
                            .size(width * w, height * h)
                            .rotate(angle),
                    )
                }

                art(Res.drawable.auth_vector, .025f, .20f, .96f, .58f)
                art(Res.drawable.auth_group, .092f, .137f, .35f, .31f, -12f)
                art(Res.drawable.auth_group1, .514f, .09f, .33f, .31f, 15f)
                art(Res.drawable.auth_group2, .27f, .48f, .45f, .40f, 10f)
                art(Res.drawable.auth_group3, .65f, .52f, .31f, .25f, -18f)
                art(Res.drawable.auth_vector1, .035f, .508f, .12f, .11f)
                art(Res.drawable.auth_vector2, .826f, .415f, .092f, .086f)
                art(Res.drawable.auth_vector3, .107f, .877f, .124f, .085f)
                @Composable
                fun badge(
                    label: String,
                    x: Float,
                    y: Float,
                    colors:
                        Pair<
                            androidx.compose.ui.graphics.Color,
                            androidx.compose.ui.graphics.Color,
                        >,
                ) {
                    Text(
                        label,
                        Modifier.offset(width * x, height * y)
                            .background(colors.first, RoundedCornerShape(14.dp))
                            .padding(horizontal = 16.dp, vertical = 9.dp),
                        color = colors.second,
                        fontWeight = FontWeight.Bold,
                    )
                }

                badge("C++", .035f, .035f, AuthArtworkColors.peach)
                badge("#", .44f, .04f, AuthArtworkColors.rose)
                badge("P", .77f, .39f, AuthArtworkColors.green)
                badge("J", .08f, .70f, AuthArtworkColors.lavender)
                badge(">>", .70f, .83f, AuthArtworkColors.blue)
            }
            Column(Modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text(
                    "Практика программирования",
                    style = MaterialTheme.typography.headlineLarge,
                    fontWeight = FontWeight.Bold,
                )
                Text(
                    "Решайте задачи, участвуйте в соревнованиях и отслеживайте свои результаты.",
                    color = MaterialTheme.colorScheme.onPrimaryContainer,
                    style = MaterialTheme.typography.bodyLarge,
                    modifier = Modifier.widthIn(max = 401.dp),
                )
            }
        }
    }
}
