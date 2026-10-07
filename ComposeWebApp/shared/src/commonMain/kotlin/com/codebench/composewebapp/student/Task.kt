package com.codebench.composewebapp.student

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeContentPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.mikepenz.markdown.m3.Markdown

@Composable
@Preview
fun Task() {
    val scrollState = rememberScrollState()

    Column(
        modifier = Modifier
            .background(MaterialTheme.colorScheme.primaryContainer)
            .safeContentPadding()
            .fillMaxSize()
            .verticalScroll(scrollState)
            .padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
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
                //SyntaxExample(Modifier.fillMaxWidth())
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