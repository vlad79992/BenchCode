package org.codebench.composewebapp.navigation

import androidx.compose.runtime.staticCompositionLocalOf
import androidx.navigation3.runtime.NavBackStack
import androidx.navigation3.runtime.NavKey

val LocalNavBackStack = staticCompositionLocalOf<NavBackStack<NavKey>> {
    error("NavBackStack not provided! Did you forget CompositionLocalProvider?")
}