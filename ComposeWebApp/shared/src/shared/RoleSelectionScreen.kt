package org.codebench.composewebapp.shared

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import org.codebench.composewebapp.navigation.LocalNavBackStack
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.student.viewmodels.UserPreferencesViewModel
import org.koin.compose.viewmodel.koinViewModel

@Composable
fun RoleSelection(
    viewModel: UserPreferencesViewModel = koinViewModel()
) {
    val backstack = LocalNavBackStack.current
    val counter by viewModel.counter.collectAsStateWithLifecycle()

    Column(
        modifier = Modifier.fillMaxSize(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Button(
            onClick = { backstack.add(StudentRoute.CompetitionSelection) },
            modifier = Modifier.width(200.dp)
        ) {
            Text("Я студент")
        }
        Button(
            onClick = {},
            modifier = Modifier.width(200.dp)
        ) {
            Text("Я преподаватель")
        }

        Spacer(modifier = Modifier.height(32.dp))

        // Простой пример работы с DataStore Preferences
        Text(
            text = "Кликов сохранено в DataStore: $counter",
            style = MaterialTheme.typography.bodyMedium
        )
        Spacer(modifier = Modifier.height(8.dp))
        Button(
            onClick = { viewModel.incrementCounter() },
            modifier = Modifier.width(200.dp)
        ) {
            Text("Сохранить клик")
        }
    }
}
