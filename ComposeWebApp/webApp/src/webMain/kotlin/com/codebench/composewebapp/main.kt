package com.codebench.composewebapp

import androidx.compose.ui.ExperimentalComposeUiApi
import androidx.compose.ui.window.ComposeViewport
import kotlinx.browser.document
import androidx.navigation.ExperimentalBrowserHistoryApi
import androidx.navigation.bindToBrowserNavigation

@OptIn(ExperimentalComposeUiApi::class)
@ExperimentalBrowserHistoryApi
fun main() {
    val body = document.body ?: return
    ComposeViewport(body) {
        App(
            onNavHostReady = { it.bindToBrowserNavigation() }
        )
    }
}