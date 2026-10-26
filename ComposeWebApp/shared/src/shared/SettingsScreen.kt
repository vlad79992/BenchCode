package org.codebench.composewebapp.shared

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.text.input.rememberTextFieldState
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.Checkbox
import androidx.compose.material3.ExperimentalMaterial3ExpressiveApi
import androidx.compose.material3.LoadingIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import org.codebench.composewebapp.shared.viewmodels.SettingsViewModel
import org.koin.compose.viewmodel.koinViewModel

@OptIn(ExperimentalMaterial3ExpressiveApi::class)
@Composable
fun Settings() {
    val viewmodel = koinViewModel<SettingsViewModel>()
    val counterState by viewmodel.counter.collectAsStateWithLifecycle()
    val name by viewmodel.name.collectAsStateWithLifecycle()
    val checked by viewmodel.authentification.collectAsStateWithLifecycle()

    Column(
        modifier = Modifier.fillMaxSize(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        if (name.isEmpty() && counterState == 0) {
            LoadingIndicator(modifier = Modifier.height(150.dp).width(150.dp))
        } else {
            val nameState = rememberTextFieldState(initialText = name)

            Card(
                modifier = Modifier.padding(8.dp)
            ) {
                Column(
                    modifier = Modifier.padding(16.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text(
                        text = "Кликов сохранено в DataStore: $counterState",
                        style = MaterialTheme.typography.bodyMedium
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    Button(
                        onClick = { viewmodel.incrementCounter() },
                        modifier = Modifier.width(200.dp)
                    ) {
                        Text("Сохранить клик")
                    }
                }
            }

            // Spacer(modifier = Modifier.height(8.dp))

            Card(
                modifier = Modifier.padding(8.dp)
            ) {
                Column(
                    modifier = Modifier.padding(16.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    OutlinedTextField(
                        state = nameState,
                        label = { Text("Имя пользователя") }
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    Button(
                        onClick = { viewmodel.updateName(nameState.text.toString()) },
                        modifier = Modifier.width(200.dp)
                    ) {
                        Text(
                            text = "Сохранить",
                            textAlign = TextAlign.Center
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(8.dp))

            Row(
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text(
                    "Включить авторизацию"
                )
                Checkbox(
                    checked = checked,
                    onCheckedChange = {
                        viewmodel.updateAuthentification(!checked)
                    }
                )
            }
        }
    }
}