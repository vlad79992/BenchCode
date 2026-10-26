package org.codebench.composewebapp.student

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.tooling.preview.Devices
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import org.codebench.composewebapp.navigation.LocalNavBackStack
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.student.viewmodels.CompetitionSelectionViewModel
import org.koin.compose.viewmodel.koinViewModel

@Composable
fun CompetitionSelection() {
    val viewModel = koinViewModel<CompetitionSelectionViewModel>()
    val backstack = LocalNavBackStack.current

    Column(
        modifier = Modifier.fillMaxSize(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        OutlinedTextField(
            state = viewModel.competitionId,
            label = { Text("Код соревнования") },
            isError = viewModel.isError,
            supportingText = {
                if (viewModel.isError) {
                    Text(
                        text = "Неверный код соревнования",
                        color = MaterialTheme.colorScheme.error
                    )
                }
            }
        )
        Button(
            onClick = {
                val validId = viewModel.validateAndGetId()
                if (validId != null) {
                    backstack.add(StudentRoute.CompetitionMainScreen(validId))
                }
            },
            modifier = Modifier.width(200.dp).padding(top = 8.dp)
        ) {
            Text("Перейти")
        }
    }
}

@Composable
@Preview(name = "Desktop", device = Devices.DESKTOP, showSystemUi = true)
@Preview(name = "Phone", device = Devices.PHONE, showSystemUi = true)
fun CompetitionSelectionPreview() {
    CompetitionSelection()
}