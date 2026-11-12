package org.codebench.composewebapp.navigation

import androidx.navigation3.runtime.NavKey
import kotlinx.serialization.Serializable

@Serializable
sealed interface SharedRoute : NavKey {
    @Serializable
    data object RoleSelection : SharedRoute, NavKey
    @Serializable
    data object Settings : SharedRoute, NavKey
}