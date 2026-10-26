package org.codebench.composewebapp.student

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.text.input.TextFieldState
import androidx.compose.foundation.text.input.rememberTextFieldState
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import org.codebench.composewebapp.navigation.LocalNavBackStack
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.student.viewmodels.AuthentificationViewModel
import org.koin.compose.viewmodel.koinViewModel

@Composable
fun AuthentificationScreen() {
    val viewModel = koinViewModel<AuthentificationViewModel>()
    val backstack = LocalNavBackStack.current

        when (viewModel.login) {
            null -> {
                Column(
                    modifier = Modifier.fillMaxSize(),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center
                ) {
                    Button(
                        onClick = {
                            viewModel.login = true
                        },
                        modifier = Modifier
                            .padding(horizontal = 8.dp)
                            .width(200.dp)
                    ) {
                        Text("Вход")
                    }
                    Button(
                        onClick = {
                            viewModel.login = false
                        },
                        modifier = Modifier
                            .padding(horizontal = 8.dp)
                            .width(200.dp)
                    ) {
                        Text("Регистрация")
                    }
                }
            }
            true -> {
                Button(
                    onClick = { viewModel.login = null }
                ) {
                    Text("<- Назад")
                }
                Column(
                    modifier = Modifier.fillMaxSize(),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center
                ) {
                    val nameState = remember { TextFieldState() }
                    val passwordState = remember { TextFieldState() }

                    OutlinedTextField(
                        state = nameState,
                        label = { Text("Имя пользователя") },
                        isError = nameState.text.isEmpty(),
                        supportingText = {
                            if (nameState.text.isEmpty()) {
                                Text("Обязательное поле")
                            }
                        }
                    )
                    OutlinedTextField(
                        state = passwordState,
                        label = { Text("Пароль") },
                        isError = passwordState.text.isEmpty(),
                        supportingText = {
                            if (passwordState.text.isEmpty()) {
                                Text("Обязательное поле")
                            }
                        }
                    )
                    Button(
                        modifier = Modifier.width(200.dp),
                        onClick = {
                            viewModel.updateName(nameState.text.toString())
                            backstack.add(StudentRoute.CompetitionSelectionScreen)
                        },
                        enabled = !(nameState.text.isEmpty() || passwordState.text.isEmpty())
                    ) {
                        Text("Войти")
                    }
                }
            }
            false -> {
                Button(
                    onClick = { viewModel.login = null }
                ) {
                    Text("<- Назад")
                }
                Column(
                    modifier = Modifier.fillMaxSize(),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center
                ) {
                    val nameState = remember { TextFieldState() }
                    val firstPasswordState = remember { TextFieldState() }
                    val secondPasswordState = remember { TextFieldState() }

                    OutlinedTextField(
                        state = nameState,
                        label = { Text("Имя пользователя") },
                        isError = nameState.text.isEmpty(),
                        supportingText = {
                            if (nameState.text.isEmpty()) {
                                Text("Обязательное поле")
                            }
                        }
                    )
                    OutlinedTextField(
                        state = firstPasswordState,
                        label = { Text("Пароль") },
                        isError = firstPasswordState.text.isEmpty() || firstPasswordState.text != secondPasswordState.text,
                        supportingText = {
                            if (firstPasswordState.text.isEmpty()) {
                                Text("Обязательное поле")
                            } else if (firstPasswordState.text != secondPasswordState.text) {
                                Text("Пароли должны совпадать")
                            }
                        }
                    )
                    OutlinedTextField(
                        state = secondPasswordState,
                        label = { Text("Повторите пароль") },
                        isError = secondPasswordState.text.isEmpty() || firstPasswordState.text != secondPasswordState.text,
                        supportingText = {
                            if (firstPasswordState.text.isEmpty()) {
                                Text("Обязательное поле")
                            } else if (firstPasswordState.text != secondPasswordState.text) {
                                Text("Пароли должны совпадать")
                            }
                        }
                    )
                    Button(
                        modifier = Modifier.width(200.dp),
                        onClick = {
                            viewModel.updateName(nameState.text.toString())
                            backstack.add(StudentRoute.CompetitionSelectionScreen)
                        },
                        enabled = !(nameState.text.isEmpty() || firstPasswordState.text.isEmpty() || secondPasswordState.text.isEmpty())
                                && firstPasswordState.text == secondPasswordState.text
                    ) {
                        Text("Войти")
                    }
                }
            }
        }
}