package org.codebench.composewebapp

import androidx.datastore.core.DataStore
import androidx.datastore.core.okio.WebLocalStorage
import androidx.datastore.preferences.core.PreferenceDataStoreFactory.create
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.PreferencesSerializer

fun createDataStore(): DataStore<Preferences> =
    create(
        storage =
            WebLocalStorage(
                serializer = PreferencesSerializer,
                name = dataStoreFileName,
            )
    )
