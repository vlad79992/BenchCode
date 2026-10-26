package org.codebench.composewebapp.student.viewmodels

import androidx.lifecycle.ViewModel
import org.codebench.composewebapp.data.Language
import org.codebench.composewebapp.data.Task
import kotlin.uuid.Uuid

class CompetitionMainScreenViewModel(
    val competitionId: String
) : ViewModel() {
    val tasks = listOf(
        Task(
            Id = "1",
            Name = "Вывод строки",
            Description = "Выведите строку \"Hello, world\".",
            Languages = Language.entries
        ),
        Task(
            Id = "2",
            Name = "Сложение",
            Description = "На ввод подаются два числа, программа должна вывести их сумму.",
            Languages = Language.entries
        )
    )
}