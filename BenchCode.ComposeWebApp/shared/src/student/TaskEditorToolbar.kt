package org.codebench.composewebapp.student

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.IconButton
import androidx.compose.material3.IconButtonDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.scale
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp

private val backIcon =
    Path().apply {
        moveTo(19f, 12f)
        lineTo(5f, 12f)
        moveTo(12f, 5f)
        lineTo(5f, 12f)
        lineTo(12f, 19f)
    }

private val attachIcon =
    Path().apply {
        moveTo(16f, 6f)
        lineTo(16f, 16f)
        cubicTo(16f, 19f, 14f, 21f, 11f, 21f)
        cubicTo(8f, 21f, 6f, 19f, 6f, 16f)
        lineTo(6f, 6f)
        cubicTo(6f, 4f, 7.5f, 2.5f, 9.5f, 2.5f)
        cubicTo(11.5f, 2.5f, 13f, 4f, 13f, 6f)
        lineTo(13f, 16f)
        cubicTo(13f, 18.8f, 9f, 18.8f, 9f, 16f)
        lineTo(9f, 6.5f)
    }

private val sendIcon =
    Path().apply {
        moveTo(3f, 5f)
        lineTo(21f, 12f)
        lineTo(3f, 19f)
        lineTo(3f, 5f)
        moveTo(3f, 12f)
        lineTo(14f, 12f)
    }

@Composable
internal fun TaskEditorToolbar(
    onBack: () -> Unit,
    onAttach: (() -> Unit)?,
    onSubmit: () -> Unit,
    canSubmit: Boolean,
) {
    Surface(
        shape = MaterialTheme.shapes.extraLarge,
        color = MaterialTheme.colorScheme.primary,
        shadowElevation = 4.dp,
    ) {
        Row(Modifier.padding(horizontal = 6.dp, vertical = 4.dp)) {
            ToolbarButton("Назад к задачам", backIcon, onBack)
            if (onAttach != null) ToolbarButton("Прикрепить файл", attachIcon, onAttach)
            ToolbarButton("Отправить решение", sendIcon, onSubmit, canSubmit)
        }
    }
}

@Composable
private fun ToolbarButton(label: String, icon: Path, action: () -> Unit, enabled: Boolean = true) {
    val color = MaterialTheme.colorScheme.onPrimary
    IconButton(
        onClick = action,
        enabled = enabled,
        modifier = Modifier.semantics { contentDescription = label },
        colors = IconButtonDefaults.iconButtonColors(contentColor = color),
    ) {
        Canvas(Modifier.size(21.dp)) {
            scale(
                size.width / 24f,
                size.height / 24f,
                pivot = androidx.compose.ui.geometry.Offset.Zero,
            ) {
                drawPath(
                    icon,
                    color.copy(alpha = if (enabled) 1f else 0.52f),
                    style = Stroke(width = 2f, cap = StrokeCap.Round, join = StrokeJoin.Round),
                )
            }
        }
    }
}
