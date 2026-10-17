package org.codebench.composewebapp.di

import androidx.compose.runtime.Composable
import org.koin.core.module.Module

// для инициализации datastore на разных платформах
@Composable
expect fun rememberPlatformModules(): List<Module>
