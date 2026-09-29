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

@Composable
@Preview
fun App() {
    val systemTheme = isSystemInDarkTheme()
    var isDarkTheme by remember { mutableStateOf(systemTheme) }
    val themeIcon = remember(isDarkTheme) {
        if (isDarkTheme) Res.drawable.theme_negate else Res.drawable.theme
    }
    AppTheme (darkTheme = isDarkTheme) {
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


                // Карточка для Markdown
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surface
                    )
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Markdown(
                            """
                        # Hello Markdown
                    
                        - Bullet points
                        - **Bold** and *italic* text
                    
                        [Check out this link](https://github.com/mikepenz/multiplatform-markdown-renderer)
                        """.trimIndent()
                        )
                    }
                }

                Spacer(Modifier.height(12.dp))

                // Карточка для примера синтаксиса
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surface
                    )
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        SyntaxExample(Modifier.fillMaxWidth())
                    }
                }

                Spacer(Modifier.height(16.dp))

                Row(Modifier.fillMaxWidth()) {
                    Button(
                        onClick = {},
                        modifier = Modifier.weight(1f)
                    ) {
                        Text("Протестировать")
                    }
                    Spacer(Modifier.width(16.dp))
                    Button(
                        onClick = {},
                        modifier = Modifier.weight(1f)
                    ) {
                        Text("Отправить")
                    }
                }

            }
        }
    }
}
