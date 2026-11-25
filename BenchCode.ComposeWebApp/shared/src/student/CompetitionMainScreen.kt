package org.codebench.composewebapp.student

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import org.codebench.composewebapp.navigation.LocalNavBackStack
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.student.viewmodels.CompetitionMainScreenViewModel
import org.koin.compose.viewmodel.koinViewModel
import org.koin.core.parameter.parametersOf

@Composable
fun CompetitionMainScreen(competitionId: String) {
    val viewModel = koinViewModel<CompetitionMainScreenViewModel> { parametersOf(competitionId) }
    val state by viewModel.state.collectAsStateWithLifecycle()
    val backstack = LocalNavBackStack.current
    CompetitionPage("Соревнования", "К вводу кода", { backstack.removeAt(backstack.lastIndex) }) {
        when {
            state.loading -> CircularProgressIndicator()
            state.error != null -> CompetitionError(state.error.orEmpty(), viewModel::load)
            else -> {
                state.competition?.let { competition ->
                    Text(
                        competition.name,
                        style = MaterialTheme.typography.headlineLarge,
                        fontWeight = FontWeight.Bold,
                    )
                    Card(
                        Modifier.fillMaxWidth(),
                        colors =
                            CardDefaults.cardColors(
                                containerColor = MaterialTheme.colorScheme.primaryContainer
                            ),
                    ) {
                        Row(
                            Modifier.padding(20.dp),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(16.dp),
                        ) {
                            Surface(
                                shape = MaterialTheme.shapes.medium,
                                color = MaterialTheme.colorScheme.surface,
                            ) {
                                Box(Modifier.size(50.dp), contentAlignment = Alignment.Center) {
                                    Text(
                                        competition.name.firstOrNull()?.uppercase() ?: "•",
                                        color = MaterialTheme.colorScheme.primary,
                                    )
                                }
                            }
                            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                                val status =
                                    when (competition.status) {
                                        0 -> "Черновик"
                                        1 -> "В процессе"
                                        2 -> "Завершено"
                                        else -> null
                                    }
                                status?.let {
                                    Text(
                                        it,
                                        style = MaterialTheme.typography.titleMedium,
                                        fontWeight = FontWeight.Bold,
                                    )
                                }
                                competition.deadline?.let {
                                    Text(
                                        "До ${it.replace('T', ' ').take(16)} UTC",
                                        style = MaterialTheme.typography.bodySmall,
                                    )
                                }
                                competition.description?.let { Text(it) }
                            }
                        }
                    }
                }
                Text(
                    "Задачи",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                )
                if (state.tasks.isEmpty()) Text("В соревновании пока нет задач")
                Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    state.tasks.forEachIndexed { index, task ->
                        Card(
                            onClick = {
                                backstack.add(StudentRoute.TaskDetails(competitionId, task.id))
                            },
                            modifier = Modifier.fillMaxWidth(),
                            colors =
                                CardDefaults.cardColors(
                                    containerColor = MaterialTheme.colorScheme.surfaceContainerLow
                                ),
                        ) {
                            Row(
                                Modifier.fillMaxWidth().padding(16.dp),
                                horizontalArrangement = Arrangement.spacedBy(12.dp),
                                verticalAlignment = Alignment.CenterVertically,
                            ) {
                                Surface(
                                    shape = MaterialTheme.shapes.medium,
                                    color = MaterialTheme.colorScheme.primaryContainer,
                                ) {
                                    Box(Modifier.size(40.dp), contentAlignment = Alignment.Center) {
                                        Text(
                                            (index + 1).toString(),
                                            color = MaterialTheme.colorScheme.primary,
                                        )
                                    }
                                }
                                Column(
                                    Modifier.weight(1f),
                                    verticalArrangement = Arrangement.spacedBy(4.dp),
                                ) {
                                    Text(
                                        task.name,
                                        style = MaterialTheme.typography.titleMedium,
                                        fontWeight = FontWeight.Bold,
                                    )
                                    val statusColor =
                                        if (task.status.startsWith("Ошибка")) {
                                            MaterialTheme.colorScheme.errorContainer
                                        } else {
                                            MaterialTheme.colorScheme.primaryContainer
                                        }
                                    Surface(
                                        shape = MaterialTheme.shapes.extraLarge,
                                        color = statusColor,
                                    ) {
                                        Text(
                                            task.status,
                                            Modifier.padding(horizontal = 10.dp, vertical = 4.dp),
                                            style = MaterialTheme.typography.labelMedium,
                                        )
                                    }
                                }
                                Button(
                                    onClick = {
                                        backstack.add(
                                            StudentRoute.TaskDetails(competitionId, task.id)
                                        )
                                    }
                                ) {
                                    Text("Открыть")
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
