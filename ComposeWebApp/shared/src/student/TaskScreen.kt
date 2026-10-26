package org.codebench.composewebapp.student

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.wrapContentSize
import androidx.compose.foundation.text.input.TextFieldState
import androidx.compose.foundation.text.selection.SelectionContainer
import androidx.compose.material3.Button
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MaterialTheme.typography
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import org.codebench.composewebapp.student.viewmodels.TaskScreenViewModel
import org.koin.compose.viewmodel.koinViewModel
import org.koin.core.parameter.parametersOf

@Composable
fun TaskScreen(taskId: String) {
    val viewmodel = koinViewModel<TaskScreenViewModel> {
        parametersOf(taskId)
    }

    val task = viewmodel.task
    var expanded by remember { mutableStateOf(false) }
    var selectedlanguage by remember { mutableStateOf("Выберите язык...") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp)
    ) {
        if (task != null) {
            Text(
                text = task.Name,
                fontSize = 32.sp,
                fontWeight = FontWeight.Bold,
                style = typography.headlineMedium
            )

            Spacer(modifier = Modifier.height(8.dp))

            Text(
                text = "ID задачи: ${task.Id}",
                fontSize = 14.sp,
                color = MaterialTheme.colorScheme.secondary
            )

            Spacer(modifier = Modifier.height(16.dp))

            SelectionContainer() {
                Text(
                    text = task.Description,
                    fontSize = 18.sp,
                    style = typography.bodyLarge,

                )
            }

            Spacer(modifier = Modifier.height(16.dp))

            val authentification by viewmodel.authentification.collectAsStateWithLifecycle()
            val name by viewmodel.name.collectAsStateWithLifecycle()

            if (!authentification) {
                val nameState = remember { TextFieldState(initialText = name) }

                Row(
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    OutlinedTextField(
                        state = nameState,
                        label = { Text("Имя пользователя") },
                        isError = nameState.text.isEmpty(),
                        supportingText = {
                            if (nameState.text.isEmpty()) {
                                Text("Обязательное поле")
                            }
                        },
                    )
                    Button(
                        onClick = { viewmodel.updateName(nameState.text.toString()) },
                        modifier = Modifier.width(200.dp).padding(start = 16.dp)
                    ) {
                        Text(
                            text = "Сохранить",
                            textAlign = TextAlign.Center
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            Box(modifier = Modifier.wrapContentSize()) {
                Button(onClick = { expanded = true }) {
                    Text(selectedlanguage)
                }
                DropdownMenu(
                    expanded = expanded,
                    onDismissRequest = { expanded = false }
                ) {
                    task.Languages.forEach { item ->
                        DropdownMenuItem(
                            text = { Text(item.toString()) },
                            onClick = {
                                selectedlanguage = item.toString()
                                expanded = false
                            }
                        )
                    }
                }
            }
//            Text(
//                text = "Доступные языки: ${task.Languages.joinToString(", ") { it.name }}",
//                fontSize = 14.sp,
//                fontWeight = FontWeight.W500,
//                style = typography.bodyMedium
//            )

            Spacer(modifier = Modifier.height(8.dp))

            OutlinedTextField(
                state = TextFieldState(),
                label = { Text("Код решения") },
                modifier = Modifier.weight(1f).fillMaxWidth()//.padding(horizontal = 8.dp)
            )

            Spacer(modifier = Modifier.height(8.dp))

            Row(
                verticalAlignment = Alignment.CenterVertically
            ) {
                var successText by remember { mutableStateOf("") }
                Button(
                    onClick = {
                        successText = "Решение успешно отправлено"
                    },
                    modifier = Modifier.width(200.dp).padding(top = 8.dp),
                    enabled = name.isNotEmpty()
                ) {
                    Text("Отправить решение")
                }
                Text(
                    text = successText,
                    color = Color.Green,
                    modifier = Modifier.padding(16.dp)
                )
            }
        } else {
            Text(
                text = "Задача с ID $taskId не найдена",
                fontSize = 20.sp,
                color = MaterialTheme.colorScheme.error,
                fontWeight = FontWeight.Medium
            )
        }
    }
}