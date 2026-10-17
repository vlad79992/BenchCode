package org.codebench.composewebapp.di

import org.codebench.composewebapp.data.UserPreferencesRepository
import org.codebench.composewebapp.navigation.SharedRoute
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.shared.RoleSelection
import org.codebench.composewebapp.shared.Settings
import org.codebench.composewebapp.shared.viewmodels.SettingsViewModel
import org.codebench.composewebapp.student.CompetitionMainScreen
import org.codebench.composewebapp.student.CompetitionSelection
import org.codebench.composewebapp.student.viewmodels.CompetitionMainScreenViewModel
import org.codebench.composewebapp.student.viewmodels.CompetitionSelectionViewModel
import org.koin.core.annotation.KoinExperimentalAPI
import org.koin.core.module.dsl.viewModel
import org.koin.dsl.module
import org.koin.dsl.navigation3.navigation

@OptIn(KoinExperimentalAPI::class)
val appModule = module {
    // Это datastore (хранит настройки)
    single { UserPreferencesRepository(dataStore = get()) }
    // Это viewmodel (хранят данные экранов)
    viewModel { SettingsViewModel(repository = get()) }
    viewModel { CompetitionSelectionViewModel() }
    viewModel { (competitionId: String) -> CompetitionMainScreenViewModel(competitionId) }

    navigation<SharedRoute.RoleSelection> {
        RoleSelection()
    }

    navigation<SharedRoute.Settings> {
        Settings()
    }

    navigation<StudentRoute.CompetitionSelection> {
        CompetitionSelection()
    }

    navigation<StudentRoute.CompetitionMainScreen> { route ->
        CompetitionMainScreen(competitionId = route.competitionId)
    }

}
