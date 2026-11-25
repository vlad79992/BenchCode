package org.codebench.composewebapp.auth

import androidx.compose.foundation.Canvas
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import org.codebench.composewebapp.resources.Res
import org.codebench.composewebapp.resources.manrope
import org.jetbrains.compose.resources.Font

private val authColors =
    lightColorScheme(
        primary = Color(0xFF5046C8),
        onPrimary = Color.White,
        primaryContainer = Color(0xFFE8E7FF),
        onPrimaryContainer = Color(0xFF56566E),
        background = Color.White,
        onBackground = Color(0xFF22223A),
        surface = Color.White,
        onSurface = Color(0xFF22223A),
        surfaceContainerLow = Color(0xFFF4F3F9),
        outline = Color(0xFF79747E),
        error = Color(0xFFB3261E),
    )

internal object AuthArtworkColors {
    val peach = Color(0xFFFFBE85) to Color(0xFF553319)
    val rose = Color(0xFFF6A8A5) to Color(0xFF713A3B)
    val green = Color(0xFFD7EAAE) to Color(0xFF3B5630)
    val lavender = Color(0xFFD6C6F7) to Color(0xFF493481)
    val blue = Color(0xFFB9DDF4) to Color(0xFF274B73)
}

@Composable
internal fun AuthTheme(content: @Composable () -> Unit) {
    val manrope = FontFamily(Font(Res.font.manrope))
    val defaults = Typography()
    val typography =
        defaults.copy(
            headlineLarge = defaults.headlineLarge.copy(fontFamily = manrope),
            headlineSmall = defaults.headlineSmall.copy(fontFamily = manrope),
            titleMedium = defaults.titleMedium.copy(fontFamily = manrope),
            bodyLarge = defaults.bodyLarge.copy(fontFamily = manrope),
            bodyMedium = defaults.bodyMedium.copy(fontFamily = manrope),
            bodySmall = defaults.bodySmall.copy(fontFamily = manrope),
            labelLarge = defaults.labelLarge.copy(fontFamily = manrope),
            labelMedium = defaults.labelMedium.copy(fontFamily = manrope),
            labelSmall = defaults.labelSmall.copy(fontFamily = manrope),
        )
    MaterialTheme(colorScheme = authColors, typography = typography, content = content)
}

@Composable
internal fun AuthCloudBackground(modifier: Modifier = Modifier) {
    Canvas(modifier) {
        fun cloud(color: Color, x: Float, y: Float, spread: Float) {
            val center = Offset(size.width * x, size.height * y)
            val radius = size.maxDimension * spread
            drawCircle(
                brush =
                    Brush.radialGradient(
                        0f to color,
                        0.45f to color.copy(alpha = color.alpha * 0.55f),
                        1f to Color.Transparent,
                        center = center,
                        radius = radius,
                    ),
                radius = radius,
                center = center,
            )
        }

        cloud(Color(0x9EC5B4FA), 0.12f, 0.28f, 0.43f)
        cloud(Color(0x9DB5CCFF), 0.48f, 0.92f, 0.42f)
        cloud(Color(0xB0FFD7CC), 0.88f, 0.30f, 0.41f)
        cloud(Color(0x9DDCC0FA), 0.88f, 0.82f, 0.39f)
    }
}
