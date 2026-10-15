package org.codebench.composewebapp.navigation

import androidx.navigation3.runtime.NavKey
import kotlinx.serialization.Serializable

@Serializable
sealed interface SelectionRoute : NavKey {
    @Serializable
    data object RoleSelection : SelectionRoute, NavKey
}