package org.codebench.composewebapp.navigation

import androidx.navigation3.runtime.NavKey
import kotlinx.serialization.Serializable

@Serializable
sealed interface SharedRoute : NavKey {
    @Serializable data object Login : SharedRoute

    @Serializable data object Registration : SharedRoute

    @Serializable
    data class RegistrationPassword(
        val firstName: String,
        val lastName: String,
        val email: String,
    ) : SharedRoute

    @Serializable data object RoleSelection : SharedRoute, NavKey

    @Serializable data object Settings : SharedRoute, NavKey
}
