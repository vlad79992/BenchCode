package org.codebench.composewebapp.student

internal actual fun codeFilePicker(
    onRead: (String) -> Unit,
    onError: (String) -> Unit,
): (() -> Unit)? = { selectCodeFile(onRead, onError) }

@OptIn(ExperimentalWasmJsInterop::class)
@JsFun(
    """
    (onRead, onError) => {
        const picker = document.createElement('input');
        picker.type = 'file';
        picker.accept = '.c,.cc,.cpp,.cxx,.cs,.java,.py,.txt';
        picker.hidden = true;
        document.body.appendChild(picker);
        picker.oncancel = () => picker.remove();
        picker.onchange = async () => {
            const file = picker.files[0];
            picker.remove();
            if (!file) return;
            if (file.size > 65536) {
                onError('Файл должен быть не больше 64 КБ');
                return;
            }
            try {
                const code = await file.text();
                if (code.includes(String.fromCharCode(0))) {
                    onError('Выберите текстовый файл с исходным кодом');
                } else {
                    onRead(code);
                }
            } catch {
                onError('Не удалось прочитать файл');
            }
        };
        picker.click();
    }
"""
)
private external fun selectCodeFile(onRead: (String) -> Unit, onError: (String) -> Unit)
