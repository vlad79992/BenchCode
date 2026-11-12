package org.codebench.composewebapp

import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.*
import org.codebench.composewebapp.di.appModule
import org.codebench.composewebapp.di.rememberPlatformModules
import org.codebench.composewebapp.navigation.NavigationRoot
import org.jetbrains.compose.reload.DevelopmentEntryPoint
import org.koin.compose.KoinApplication
import org.koin.dsl.koinConfiguration

@Composable
@DevelopmentEntryPoint
fun App() {
    val platformModules = rememberPlatformModules()
    KoinApplication(configuration = koinConfiguration(declaration = {
        modules(platformModules)
        modules(appModule)
    }), content = {
        MaterialTheme {
            NavigationRoot()
        }
    })
}
