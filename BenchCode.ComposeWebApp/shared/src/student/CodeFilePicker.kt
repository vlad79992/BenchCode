package org.codebench.composewebapp.student

internal expect fun codeFilePicker(
    onRead: (String) -> Unit,
    onError: (String) -> Unit,
): (() -> Unit)?
