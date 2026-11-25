package org.codebench.composewebapp.student

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.text.input.TextFieldLineLimits
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.tooling.preview.Devices
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import org.codebench.composewebapp.navigation.LocalNavBackStack
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.shared.submitOnEnter
import org.codebench.composewebapp.student.viewmodels.CompetitionSelectionViewModel
import org.koin.compose.viewmodel.koinViewModel

@Composable
fun CompetitionSelection() {
    val viewModel = koinViewModel<CompetitionSelectionViewModel>()
    val backstack = LocalNavBackStack.current
    val state by viewModel.state.collectAsStateWithLifecycle()
    LaunchedEffect(state.competitionId) {
        state.competitionId?.let { id ->
            backstack.add(StudentRoute.CompetitionMainScreen(id))
            viewModel.consumeNavigation()
        }
    }

    Column(
        modifier = Modifier.fillMaxSize(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        OutlinedTextField(
            state = viewModel.competitionId,
            modifier = Modifier.submitOnEnter(viewModel::join),
            lineLimits = TextFieldLineLimits.SingleLine,
            keyboardOptions = KeyboardOptions(imeAction = ImeAction.Done),
            onKeyboardAction = { viewModel.join() },
            label = { Text("Код соревнования") },
        )
        Button(
            onClick = viewModel::join,
            enabled = !state.loading,
            modifier = Modifier.width(200.dp),
        ) {
            Text(if (state.loading) "Открываем…" else "Перейти")
        }
        state.error?.let { Text(it) }
    }
}

@Composable
@Preview(name = "Desktop", device = Devices.DESKTOP, showSystemUi = true)
@Preview(name = "Phone", device = Devices.PHONE, showSystemUi = true)
fun CompetitionSelectionPreview() {
    CompetitionSelection()
}
