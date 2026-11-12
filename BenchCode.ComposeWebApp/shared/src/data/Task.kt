package org.codebench.composewebapp.data

import kotlin.uuid.Uuid

class Task(
    val Id: Uuid,
    val Name: String,
    val Description: String,
    val Languages: List<Language>
)