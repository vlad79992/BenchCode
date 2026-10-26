package org.codebench.composewebapp.student

import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Card
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.tooling.preview.Devices
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import org.codebench.composewebapp.navigation.LocalNavBackStack
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.student.viewmodels.CompetitionMainScreenViewModel
import org.koin.compose.viewmodel.koinViewModel
import org.koin.core.parameter.parametersOf

@Composable
fun CompetitionMainScreen(competitionId: String) {
    val viewModel = koinViewModel<CompetitionMainScreenViewModel> {
        parametersOf(competitionId)
    }
    val backstack = LocalNavBackStack.current

    Column(
        modifier = Modifier.fillMaxSize(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
//        Card {
//            Text(
//                "Код соревнования: ${viewModel.competitionId}",
//                fontSize = 48.sp,
//                fontStyle = FontStyle.Italic,
//                fontWeight = FontWeight.W600,
//                modifier = Modifier.padding(24.dp)
//            )
//        }

        Text(
            text = "Список задач:",
            style = MaterialTheme.typography.titleLarge,
            modifier = Modifier.align(Alignment.Start).padding(16.dp)
        )

        LazyColumn(
            modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            items(viewModel.tasks) { task ->
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable {
                            backstack.add(StudentRoute.TaskScreen(task.Id))
                        }
                ) {
                    ListItem(
                        headlineContent = {
                            Text(task.Name, fontWeight = FontWeight.Medium)
                        }
                    )
                }
            }
        }
    }
}

@Composable
@Preview(name = "Desktop", device = Devices.DESKTOP, showSystemUi = true)
@Preview(name = "Phone", device = Devices.PHONE, showSystemUi = true)
fun CompetitionPreview() {
    CompetitionMainScreen("123")
}