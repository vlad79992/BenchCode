package org.codebench.composewebapp.di

import org.codebench.composewebapp.data.UserPreferencesRepository
import org.codebench.composewebapp.navigation.SelectionRoute
import org.codebench.composewebapp.navigation.StudentRoute
import org.codebench.composewebapp.shared.RoleSelection
import org.codebench.composewebapp.student.CompetitionMainScreen
import org.codebench.composewebapp.student.CompetitionSelection
import org.codebench.composewebapp.student.viewmodels.CompetitionMainScreenViewModel
import org.codebench.composewebapp.student.viewmodels.CompetitionSelectionViewModel
import org.codebench.composewebapp.student.viewmodels.UserPreferencesViewModel
import org.koin.core.annotation.KoinExperimentalAPI
import org.koin.core.module.dsl.viewModel
import org.koin.dsl.module
import org.koin.dsl.navigation3.navigation

@OptIn(KoinExperimentalAPI::class)
val appModule = module {
    single { UserPreferencesRepository(dataStore = get()) }

    viewModel { UserPreferencesViewModel(repository = get()) }
    viewModel { CompetitionSelectionViewModel() }
    viewModel { (competitionId: String) -> CompetitionMainScreenViewModel(competitionId) }

    navigation<SelectionRoute.RoleSelection> {
        RoleSelection()
    }

    navigation<StudentRoute.CompetitionSelection> {
        CompetitionSelection()
    }

    navigation<StudentRoute.CompetitionMainScreen> { route ->
        CompetitionMainScreen(competitionId = route.competitionId)
    }
}
