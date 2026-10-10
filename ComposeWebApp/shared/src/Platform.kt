package org.codebench.composewebapp

interface Platform {
    val name: String
}

expect fun getPlatform(): Platform
