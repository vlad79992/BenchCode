package org.codebench.composewebapp.di

import io.ktor.client.HttpClient
import org.codebench.composewebapp.auth.AuthRepository
import org.codebench.composewebapp.auth.AuthTheme
import org.codebench.composewebapp.auth.AuthViewModel
import org.codebench.composewebapp.auth.LoginScreen
import org.codebench.composewebapp.auth.RegistrationPasswordScreen
import org.codebench.composewebapp.auth.RegistrationScreen
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
    single { HttpClient() }
    single { AuthRepository(get()) }
    viewModel { AuthViewModel(get()) }

    navigation<SharedRoute.Login> {
        AuthTheme { LoginScreen() }
    }

    navigation<SharedRoute.Registration> {
        AuthTheme { RegistrationScreen() }
    }

    navigation<SharedRoute.RegistrationPassword> { profile ->
        AuthTheme { RegistrationPasswordScreen(profile) }
    }

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
