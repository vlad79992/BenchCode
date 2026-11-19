package org.codebench.composewebapp.shared

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import org.codebench.composewebapp.navigation.LocalNavBackStack
import org.codebench.composewebapp.navigation.SharedRoute
import org.codebench.composewebapp.navigation.StudentRoute

@Composable
fun RoleSelection() {
  val backstack = LocalNavBackStack.current

  Column(
      modifier = Modifier.fillMaxSize(),
      horizontalAlignment = Alignment.CenterHorizontally,
      verticalArrangement = Arrangement.Center,
  ) {
    Button(
        onClick = { backstack.add(StudentRoute.CompetitionSelection) },
        modifier = Modifier.width(200.dp),
    ) {
      Text("Я студент")
    }
    Button(
        onClick = {},
        modifier = Modifier.width(200.dp),
    ) {
      Text("Я преподаватель")
    }

    Spacer(modifier = Modifier.height(32.dp))

    Button(
        onClick = { backstack.add(SharedRoute.Settings) },
        modifier = Modifier.width(200.dp),
    ) {
      Text("Настройки")
    }
  }
}
