package org.codebench.composewebapp.di

import org.codebench.composewebapp.data.UserPreferencesRepository
import org.codebench.composewebapp.navigation.SharedRoute
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.shared.RoleSelection
import org.codebench.composewebapp.shared.Settings
import org.codebench.composewebapp.shared.viewmodels.RoleSelectionViewModel
import org.codebench.composewebapp.shared.viewmodels.SettingsViewModel
import org.codebench.composewebapp.student.AuthentificationScreen
import org.codebench.composewebapp.student.CompetitionMainScreen
import org.codebench.composewebapp.student.CompetitionSelection
import org.codebench.composewebapp.student.TaskScreen
import org.codebench.composewebapp.student.viewmodels.AuthentificationViewModel
import org.codebench.composewebapp.student.viewmodels.CompetitionMainScreenViewModel
import org.codebench.composewebapp.student.viewmodels.CompetitionSelectionViewModel
import org.codebench.composewebapp.student.viewmodels.TaskScreenViewModel
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
    viewModel { RoleSelectionViewModel(repository = get()) }
    viewModel { CompetitionSelectionViewModel() }
    viewModel { AuthentificationViewModel(repository = get()) }
    viewModel { (competitionId: String) -> CompetitionMainScreenViewModel(competitionId) }
    viewModel { (taskId: String) -> TaskScreenViewModel(taskId, repository = get()) }
    // Для переходов
    navigation<SharedRoute.RoleSelection> { RoleSelection() }
    navigation<SharedRoute.Settings> { Settings() }
    navigation<StudentRoute.CompetitionSelectionScreen> { CompetitionSelection() }
    navigation<StudentRoute.AuthentificationScreen> { AuthentificationScreen() }
    navigation<StudentRoute.CompetitionMainScreen> { route -> CompetitionMainScreen(competitionId = route.competitionId) }
    navigation<StudentRoute.TaskScreen> { route -> TaskScreen(taskId = route.taskId) }
}
