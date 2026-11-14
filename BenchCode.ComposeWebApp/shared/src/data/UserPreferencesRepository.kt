package org.codebench.composewebapp.data

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.intPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

class UserPreferencesRepository(
    private val dataStore: DataStore<Preferences>
) {
    private object PreferencesKeys {
        val COUNTER = intPreferencesKey("counter")
        val USER_NAME = stringPreferencesKey("user_name")
    }

    val counterFlow: Flow<Int> = dataStore.data.map { preferences ->
        preferences[PreferencesKeys.COUNTER] ?: 0
    }

    val userNameFlow: Flow<String> = dataStore.data.map { preferences ->
        preferences[PreferencesKeys.USER_NAME] ?: ""
    }

    suspend fun incrementCounter() {
        dataStore.edit { preferences ->
            val current = preferences[PreferencesKeys.COUNTER] ?: 0
            preferences[PreferencesKeys.COUNTER] = current + 1
        }
    }

    suspend fun saveUserName(name: String) {
        dataStore.edit { preferences ->
            preferences[PreferencesKeys.USER_NAME] = name
        }
    }
}
