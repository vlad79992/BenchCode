package com.codebench.composewebapp.student

import androidx.compose.foundation.text.BasicTextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.sp

import com.gallatinapps.syntaxmp.compose.SyntaxTheme
import com.gallatinapps.syntaxmp.compose.rememberSyntaxAnnotatedString
import com.gallatinapps.syntaxmp.tokenizer.SyntaxTokenizer

@Composable
fun SyntaxExample(modifier: Modifier) {
    val engine = remember { SyntaxTokenizer() }
    val theme = remember { SyntaxTheme.DefaultLight }

    // 1. Храним полное состояние ввода (текст + позиция курсора)
    var textFieldState by remember {
        mutableStateOf(
            TextFieldValue(
                text = """
                    #include <iostream>
                    
                    int main()
                    {
                        std::cout << "Hello world!" << std::endl;
                        return 0;
                    }
                """.trimIndent()
            )
        )
    }

    // 2. Генерируем подсвеченный текст на основе текущего текста в стейте
    val annotatedString = rememberSyntaxAnnotatedString(
        code = textFieldState.text,
        languageLabel = "cpp",
        engine = engine,
        theme = theme
    )

    // 3. Собираем финальное значение для текстового поля:
    // Берем актуальный курсор/выделение из стейта, но подставляем подсвеченный текст
    val finalTextFieldValue = textFieldState.copy(annotatedString = annotatedString)

    BasicTextField(
        value = finalTextFieldValue,
        onValueChange = { newValue ->
            // 4. Просто сохраняем новое состояние, которое ввел пользователь
            textFieldState = newValue
        },
        textStyle = TextStyle(
            fontFamily = FontFamily.Monospace,
            fontSize = 14.sp,
            lineHeight = 20.sp
        ),
        modifier = modifier
    )
}
