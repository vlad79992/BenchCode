package com.codebench.composewebapp

interface Platform {
    val name: String
}

expect fun getPlatform(): Platform