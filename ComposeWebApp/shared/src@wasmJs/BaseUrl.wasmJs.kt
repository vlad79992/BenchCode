package org.codebench.composewebapp

import kotlinx.browser.window

actual fun getBaseUrl(): String {
    return window.location.origin
}