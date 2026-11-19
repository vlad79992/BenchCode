package ru.benchcode.auth

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun BenchCodeApp(repository: AuthRepository = remember { MockAuthRepository() }) {
    var isAuthenticated by remember { mutableStateOf(false) }

    if (!isAuthenticated) {
        BenchCodeAuthScreen(
            repository = repository,
            onAuthenticated = { isAuthenticated = true }
        )
    } else {
        Column(
            modifier = Modifier.padding(32.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            Text("Добро пожаловать в BenchCode!")
            Button(onClick = { isAuthenticated = false }) {
                Text("Выйти")
            }
        }
    }
}
