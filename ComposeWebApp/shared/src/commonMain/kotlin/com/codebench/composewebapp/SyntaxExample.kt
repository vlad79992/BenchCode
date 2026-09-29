package com.codebench.composewebapp

import androidx.compose.foundation.text.BasicTextField
import androidx.compose.material3.MaterialTheme
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
fun SyntaxExample(
    value: TextFieldValue,
    onValueChange: (TextFieldValue) -> Unit,
    currentLanguage: String,
    isDarkTheme: Boolean,
    modifier: Modifier = Modifier
) {
    val engine = remember { SyntaxTokenizer() }
    val theme = remember(isDarkTheme) {
        if (isDarkTheme) SyntaxTheme.DefaultDark else SyntaxTheme.DefaultLight
    }

    val annotatedString = rememberSyntaxAnnotatedString(
        code = value.text,
        languageLabel = currentLanguage,
        engine = engine,
        theme = theme
    )

    val finalTextFieldValue = value.copy(annotatedString = annotatedString)

    BasicTextField(
        value = finalTextFieldValue,
        onValueChange = onValueChange,
        textStyle = TextStyle(
            fontFamily = FontFamily.Monospace,
            fontSize = 14.sp,
            lineHeight = 20.sp,
            color = MaterialTheme.colorScheme.onSurface
        ),
        modifier = modifier
    )
}
