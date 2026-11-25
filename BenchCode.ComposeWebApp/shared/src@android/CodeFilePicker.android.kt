package org.codebench.composewebapp.student

internal actual fun codeFilePicker(
    onRead: (String) -> Unit,
    onError: (String) -> Unit,
): (() -> Unit)? = null
