package com.codebench.composewebapp

import Theme.AppTheme
import androidx.compose.foundation.Image
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeContentPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.mikepenz.markdown.m3.Markdown
import composewebapp.shared.generated.resources.Res
import composewebapp.shared.generated.resources.theme
import composewebapp.shared.generated.resources.theme_negate
import org.jetbrains.compose.resources.painterResource
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedTextField
import androidx.compose.ui.text.input.TextFieldValue

@OptIn(ExperimentalMaterial3Api::class)
@Composable
@Preview
fun App() {
    val systemTheme = isSystemInDarkTheme()
    var isDarkTheme by remember { mutableStateOf(systemTheme) }

    var selectedLanguage by remember { mutableStateOf(CodeTemplates.availableLanguages.firstOrNull() ?: "cpp") }
    var expanded by remember { mutableStateOf(false) }

    var textFieldState by remember {
        mutableStateOf(TextFieldValue(text = CodeTemplates.getTemplate(selectedLanguage)))
    }

    val themeIcon = remember(isDarkTheme) {
        if (isDarkTheme) Res.drawable.theme_negate else Res.drawable.theme
    }

    AppTheme(darkTheme = isDarkTheme) {
        Surface(color = MaterialTheme.colorScheme.background) {
            val scrollState = rememberScrollState()

            Column(
                modifier = Modifier
                    .safeContentPadding()
                    .fillMaxSize()
                    .verticalScroll(scrollState)
                    .padding(16.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.End
                ) {
                    Image(
                        painter = painterResource(themeIcon),
                        contentDescription = "Переключатель темы",
                        modifier = Modifier.size(24.dp)
                    )
                    Spacer(Modifier.width(12.dp))
                    Switch(
                        checked = isDarkTheme,
                        onCheckedChange = { isDarkTheme = it },
                    )
                }

                Spacer(Modifier.height(12.dp))

                ExposedDropdownMenuBox(
                    expanded = expanded,
                    onExpandedChange = { expanded = it },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    OutlinedTextField(
                        value = selectedLanguage.uppercase(),
                        onValueChange = {},
                        readOnly = true,
                        label = { Text("Язык программирования") },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
                        modifier = Modifier
                            .fillMaxWidth()
                            .menuAnchor(MenuAnchorType.PrimaryNotEditable, true)
                    )
                    ExposedDropdownMenu(
                        expanded = expanded,
                        onDismissRequest = { expanded = false }
                    ) {
                        CodeTemplates.availableLanguages.forEach { language ->
                            DropdownMenuItem(
                                text = { Text(language.uppercase()) },
                                onClick = {
                                    selectedLanguage = language
                                    // Подставляем новый шаблон через функцию getTemplate
                                    textFieldState = TextFieldValue(text = CodeTemplates.getTemplate(language))
                                    expanded = false
                                }
                            )
                        }
                    }
                }

                Spacer(Modifier.height(16.dp))

                // Карточка Markdown
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Markdown(
                            """
                            # Hello Markdown
                            - Bullet points
                            - **Bold** and *italic* text
                            """.trimIndent()
                        )
                    }
                }

                Spacer(Modifier.height(12.dp))

                // Карточка редактора кода
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        SyntaxExample(
                            value = textFieldState,
                            onValueChange = { textFieldState = it },
                            currentLanguage = selectedLanguage,
                            isDarkTheme = isDarkTheme,
                            modifier = Modifier.fillMaxWidth()
                        )
                    }
                }

                Spacer(Modifier.height(16.dp))

                // Кнопки действия
                Row(Modifier.fillMaxWidth()) {
                    Button(onClick = {}, modifier = Modifier.weight(1f)) {
                        Text("Протестировать")
                    }
                    Spacer(Modifier.width(16.dp))
                    Button(onClick = {}, modifier = Modifier.weight(1f)) {
                        Text("Отправить")
                    }
                }
            }
        }
    }
}
