package org.codebench.composewebapp.auth

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
internal data class RegisterRequest(
    val firstName: String,
    val lastName: String,
    val email: String,
    val password: String,
)

@Serializable internal data class AuthSession(@SerialName("access_token") val accessToken: String)
