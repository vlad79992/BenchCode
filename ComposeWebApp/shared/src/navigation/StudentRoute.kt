package org.codebench.composewebapp.navigation

import androidx.navigation3.runtime.NavKey
import kotlinx.serialization.Serializable

@Serializable
sealed interface StudentRoute : NavKey {
    @Serializable
    data object CompetitionSelectionScreen : StudentRoute, NavKey

    @Serializable
    data class CompetitionMainScreen(val competitionId: String) : StudentRoute, NavKey

    @Serializable
    data class TaskScreen(val taskId: String) : StudentRoute, NavKey

    @Serializable
    data object AuthentificationScreen : StudentRoute, NavKey
}