package org.codebench.composewebapp.student

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import org.codebench.composewebapp.navigation.LocalNavBackStack
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.resources.Res
import org.codebench.composewebapp.resources.auth_benchcube
import org.codebench.composewebapp.student.viewmodels.TaskState
import org.codebench.composewebapp.student.viewmodels.TaskViewModel
import org.jetbrains.compose.resources.painterResource
import org.koin.compose.viewmodel.koinViewModel
import org.koin.core.parameter.parametersOf

@Composable
internal fun TaskScreen(route: StudentRoute.TaskDetails) {
    val viewModel = koinViewModel<TaskViewModel> { parametersOf(route) }
    val state by viewModel.state.collectAsStateWithLifecycle()
    val backstack = LocalNavBackStack.current
    CompetitionPage("Соревнования", "К задачам", { backstack.removeAt(backstack.lastIndex) }) {
        val task = state.task
        when {
            task != null -> {
                Text(
                    task.name,
                    style = MaterialTheme.typography.headlineLarge,
                    fontWeight = FontWeight.Bold,
                )
                BoxWithConstraints(Modifier.fillMaxWidth()) {
                    if (maxWidth >= 800.dp) {
                        Row(horizontalArrangement = Arrangement.spacedBy(24.dp)) {
                            TaskStatement(task, Modifier.weight(1f))
                            TaskEditor(
                                task,
                                state,
                                viewModel,
                                { backstack.removeAt(backstack.lastIndex) },
                                Modifier.weight(2.1f),
                            )
                        }
                    } else {
                        Column(verticalArrangement = Arrangement.spacedBy(16.dp)) {
                            TaskStatement(task, Modifier.fillMaxWidth())
                            TaskEditor(
                                task,
                                state,
                                viewModel,
                                { backstack.removeAt(backstack.lastIndex) },
                                Modifier.fillMaxWidth(),
                            )
                        }
                    }
                }
            }
            state.error != null -> CompetitionError(state.error.orEmpty(), viewModel::load)
            else -> CircularProgressIndicator()
        }
    }
}

@Composable
internal fun CompetitionPage(
    label: String,
    backLabel: String,
    onBack: () -> Unit,
    body: @Composable ColumnScope.() -> Unit,
) {
    Surface(Modifier.fillMaxSize(), color = MaterialTheme.colorScheme.background) {
        BoxWithConstraints(Modifier.fillMaxSize()) {
            Column(
                Modifier.fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(
                        horizontal = if (maxWidth >= 840.dp) 48.dp else 20.dp,
                        vertical = 16.dp,
                    ),
                verticalArrangement = Arrangement.spacedBy(20.dp),
            ) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(10.dp),
                ) {
                    Image(painterResource(Res.drawable.auth_benchcube), null, Modifier.size(38.dp))
                    Text(
                        "BenchCode",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold,
                    )
                    if (this@BoxWithConstraints.maxWidth >= 720.dp) {
                        Spacer(Modifier.width(48.dp))
                        FilledTonalButton(onClick = onBack) { Text("Соревнования") }
                    }
                    Spacer(Modifier.weight(1f))
                    TextButton(onClick = onBack) { Text(backLabel) }
                }
                HorizontalDivider()
                Text(label, style = MaterialTheme.typography.bodySmall)
                body()
            }
        }
    }
}

@Composable
internal fun CompetitionError(message: String, retry: () -> Unit) {
    Text(message, color = MaterialTheme.colorScheme.error)
    TextButton(onClick = retry) { Text("Повторить") }
}

@Composable
private fun TaskStatement(task: TaskDetails, modifier: Modifier) {
    OutlinedCard(
        modifier,
        shape = RoundedCornerShape(20.dp),
        border = BorderStroke(1.dp, verificationBorder),
    ) {
        Column(Modifier.padding(22.dp), verticalArrangement = Arrangement.spacedBy(14.dp)) {
            Text(
                "Условие задачи",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
            )
            Text(task.statementMarkdown)
            Text(
                "Ограничения",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
            )
            Surface(
                shape = MaterialTheme.shapes.medium,
                color = MaterialTheme.colorScheme.surfaceContainerLow,
            ) {
                Column(
                    Modifier.fillMaxWidth().padding(12.dp),
                    verticalArrangement = Arrangement.spacedBy(6.dp),
                ) {
                    Text("Ограничение времени   ${task.timeLimitMs} мс")
                    Text("Ограничение памяти   ${task.memoryLimitMb} МБ")
                }
            }
            task.publicTests.forEachIndexed { index, example ->
                Text(
                    "Пример ${index + 1}",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                )
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    ExampleBox("Ввод", example.inputText.orEmpty(), Modifier.weight(1f))
                    ExampleBox("Вывод", example.expectedOutputText.orEmpty(), Modifier.weight(1f))
                }
            }
        }
    }
}

@Composable
private fun ExampleBox(label: String, value: String, modifier: Modifier) {
    Card(
        modifier,
        colors =
            CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceContainerLow),
    ) {
        Column(Modifier.fillMaxWidth().padding(12.dp)) {
            Text(label, style = MaterialTheme.typography.bodySmall)
            Text(value, fontFamily = FontFamily.Monospace)
        }
    }
}

@Composable
private fun TaskEditor(
    task: TaskDetails,
    state: TaskState,
    viewModel: TaskViewModel,
    onBack: () -> Unit,
    modifier: Modifier,
) {
    var expanded by remember { mutableStateOf(false) }
    var fileError by remember { mutableStateOf<String?>(null) }
    val openFile =
        codeFilePicker(
            onRead = { code ->
                fileError = null
                viewModel.edit(code)
            },
            onError = { fileError = it },
        )
    val canSubmit = state.code.isNotBlank() && state.language != null && !state.busy
    OutlinedCard(
        modifier,
        shape = RoundedCornerShape(20.dp),
        border = BorderStroke(1.dp, verificationBorder),
        colors = CardDefaults.outlinedCardColors(containerColor = Color(0xFFF6F5FC)),
    ) {
        Column(Modifier.padding(22.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text(
                    "Редактор кода",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                )
                TaskEditorToolbar(onBack, openFile, viewModel::submit, canSubmit)
            }
            Box {
                Button(
                    onClick = { expanded = true },
                    enabled = task.allowedLanguages.isNotEmpty(),
                ) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(7.dp),
                    ) {
                        Text(state.language?.let(::languageName) ?: "Нет доступных языков")
                        val color = LocalContentColor.current
                        Canvas(Modifier.size(8.dp, 5.dp)) {
                            drawPath(
                                Path().apply {
                                    lineTo(size.width, 0f)
                                    lineTo(size.width / 2f, size.height)
                                    close()
                                },
                                color,
                            )
                        }
                    }
                }
                DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
                    task.allowedLanguages.forEach { language ->
                        DropdownMenuItem(
                            text = { Text(languageName(language)) },
                            onClick = {
                                viewModel.select(language)
                                expanded = false
                            },
                        )
                    }
                }
            }
            val result = state.result
            val overlap = if (result != null && result.status != 0) (-8).dp else 12.dp
            Column(verticalArrangement = Arrangement.spacedBy(overlap)) {
                OutlinedTextField(
                    value = state.code,
                    onValueChange = viewModel::edit,
                    modifier =
                        Modifier.fillMaxWidth()
                            .heightIn(
                                min =
                                    if (state.result?.status?.let { it != 0 } == true) 220.dp
                                    else 310.dp,
                                max = 560.dp,
                            ),
                    colors =
                        OutlinedTextFieldDefaults.colors(
                            focusedContainerColor = MaterialTheme.colorScheme.surface,
                            unfocusedContainerColor = MaterialTheme.colorScheme.surface,
                            unfocusedBorderColor = verificationBorder,
                        ),
                    textStyle =
                        MaterialTheme.typography.bodyMedium.copy(fontFamily = FontFamily.Monospace),
                    label = { Text("Код решения") },
                )
                when {
                    state.busy && result == null -> VerificationPending()
                    result?.status == 0 ->
                        VerificationPending(onRefresh = viewModel::refresh, refreshing = state.busy)
                    result != null -> VerificationResult(result, onBack)
                    else ->
                        Button(
                            onClick = viewModel::submit,
                            enabled = canSubmit,
                            modifier = Modifier.align(Alignment.End),
                        ) {
                            Text("Отправить решение")
                        }
                }
            }
            fileError?.let { Text(it, color = MaterialTheme.colorScheme.error) }
            state.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        }
    }
}

private val verificationBorder = Color(0xFFDFDDED)
private val pendingBackground = Color(0xFFECEBFF)
private val acceptedBackground = Color(0xFFEAF7EE)
private val acceptedTitle = Color(0xFF226548)

@OptIn(ExperimentalMaterial3ExpressiveApi::class)
@Composable
private fun VerificationPending(onRefresh: (() -> Unit)? = null, refreshing: Boolean = true) {
    Surface(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(20.dp),
        color = pendingBackground,
        border = BorderStroke(1.dp, verificationBorder),
    ) {
        Row(
            Modifier.padding(horizontal = 22.dp, vertical = 8.dp).heightIn(min = 56.dp),
            horizontalArrangement = Arrangement.spacedBy(16.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            LoadingIndicator(
                modifier = Modifier.size(32.dp),
                color = MaterialTheme.colorScheme.primary,
            )
            Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(
                    "Проверяем решение…",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                )
                Text(
                    "Результат появится после выполнения тестов.",
                    style = MaterialTheme.typography.bodySmall,
                )
            }
            if (onRefresh != null) {
                Button(onClick = onRefresh, enabled = !refreshing) { Text("Результат") }
            }
        }
    }
}

@Composable
private fun VerificationResult(result: Submission, onBack: () -> Unit) {
    val accepted = result.status == 1 && result.verdict == 0
    val metrics = buildList {
        if (result.testResults.isNotEmpty()) {
            add(
                "${result.testResults.count { it.verdict == 0 }} / ${result.testResults.size} тестов"
            )
        }
        result.timeMs?.let { add("$it мс") }
        result.memoryKb?.let {
            val tenthsOfMb = (it.toLong() * 10 + 512) / 1024
            add(if (tenthsOfMb < 10) "$it КБ" else "${tenthsOfMb / 10},${tenthsOfMb % 10} МБ")
        }
    }
    Surface(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(20.dp),
        color = if (accepted) acceptedBackground else MaterialTheme.colorScheme.errorContainer,
        border = BorderStroke(1.dp, verificationBorder),
    ) {
        Column(
            Modifier.heightIn(min = 160.dp).padding(22.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            Text(
                if (accepted) "Решение зачтено" else resultLabel(result),
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
                color = if (accepted) acceptedTitle else MaterialTheme.colorScheme.onErrorContainer,
            )
            if (metrics.isNotEmpty()) Text(metrics.joinToString("   ·   "))
            if (accepted) {
                Text(
                    if (result.isLate) "Все тесты пройдены. Решение отправлено после дедлайна."
                    else "Все тесты пройдены. Решение учтено в рейтинге.",
                    style = MaterialTheme.typography.bodySmall,
                )
            }
            result.compilerOutput
                ?.takeIf { it.isNotBlank() }
                ?.let { Text(it, fontFamily = FontFamily.Monospace) }
            FilledTonalButton(onClick = onBack, modifier = Modifier.align(Alignment.End)) {
                Text("К задачам")
            }
        }
    }
}

private fun languageName(language: Int): String =
    when (language) {
        0 -> "C++"
        1 -> "C#"
        2 -> "Java"
        3 -> "C"
        4 -> "Python"
        else -> "Язык $language"
    }

private fun resultLabel(result: Submission): String =
    when (result.status) {
        0 -> "Тестируется"
        1 ->
            when (result.verdict) {
                0 -> "Принято"
                1 -> "Неверный ответ"
                2 -> "Ошибка компиляции"
                3 -> "Ошибка выполнения"
                4 -> "Превышен лимит времени"
                5 -> "Превышен лимит памяти"
                else -> "Проверка завершена"
            }
        2 -> "Ошибка системы"
        else -> "Статус неизвестен"
    }
