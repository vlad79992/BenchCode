package org.codebench.composewebapp.shared

import androidx.compose.ui.Modifier
import androidx.compose.ui.input.key.*

internal fun Modifier.submitOnEnter(onSubmit: () -> Unit): Modifier = onPreviewKeyEvent { event ->
    if (event.key != Key.Enter && event.key != Key.NumPadEnter) {
        false
    } else {
        if (event.type == KeyEventType.KeyDown) onSubmit()
        true
    }
}
