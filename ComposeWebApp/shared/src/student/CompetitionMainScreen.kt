package org.codebench.composewebapp.student

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Card
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.tooling.preview.Devices
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import org.codebench.composewebapp.student.viewmodels.CompetitionMainScreenViewModel
import org.koin.compose.viewmodel.koinViewModel
import org.koin.core.parameter.parametersOf

@Composable
fun CompetitionMainScreen(competitionId: String) {
    val viewModel = koinViewModel<CompetitionMainScreenViewModel> {
        parametersOf(competitionId)
    }

    Column(
        modifier = Modifier.fillMaxSize(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Card {
            Text(
                "Код соревнования: ${viewModel.competitionId}",
                fontSize = 48.sp,
                fontStyle = FontStyle.Italic,
                fontWeight = FontWeight.W600,
                modifier = Modifier.padding(24.dp)
            )
        }
    }
}

@Composable
@Preview(name = "Desktop", device = Devices.DESKTOP, showSystemUi = true)
@Preview(name = "Phone", device = Devices.PHONE, showSystemUi = true)
fun CompetitionPreview() {
    CompetitionMainScreen("123")
}