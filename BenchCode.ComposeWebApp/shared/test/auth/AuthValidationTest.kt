package org.codebench.composewebapp.auth

import kotlin.test.Test
import kotlin.test.assertNotNull
import kotlin.test.assertNull

class AuthValidationTest {
    @Test
    fun acceptsEmailWithSurroundingSpaces() {
        assertNull(emailError(" person@example.com "))
    }

    @Test
    fun rejectsInvalidEmail() {
        assertNotNull(emailError("example.com"))
        assertNotNull(emailError(""))
    }

    @Test
    fun requiresEightPasswordCharacters() {
        assertNotNull(passwordError("1234567"))
        assertNull(passwordError("12345678"))
    }
}
