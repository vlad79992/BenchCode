package com.codebench.composewebapp

import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.tooling.preview.Preview
import androidx.navigation.NavController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.codebench.composewebapp.student.MainScreen

@Composable
fun App(
    onNavHostReady: suspend (NavController) -> Unit = {}
) {
    MaterialTheme {
        val navController = rememberNavController()
        NavHost(navController = navController, startDestination = RoleSelection) {
            composable<RoleSelection> { RoleSelection (
                navigateToStudent = {
                    navController.navigate(StudentMainScreen)
                },
                navigateToTeacher = {

                }
            ) }
            composable<StudentMainScreen> { MainScreen() }
            // You can add more destinations similarly
        }

        LaunchedEffect(navController) {
            onNavHostReady(navController)
        }
    }
}
