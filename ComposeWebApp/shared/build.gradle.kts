import org.jetbrains.kotlin.gradle.ExperimentalWasmDsl

plugins {
    alias(libs.plugins.kotlinMultiplatform)
    alias(libs.plugins.composeMultiplatform)
    alias(libs.plugins.composeCompiler)
}

kotlin {
    jvm()
    
    @OptIn(ExperimentalWasmDsl::class)
    wasmJs {
        browser()
        binaries.executable()
    }
    
    
    sourceSets {
        commonMain.dependencies {
            implementation("com.mikepenz:multiplatform-markdown-renderer-m3:0.43.0")
            implementation(libs.syntaxmp)

            implementation(libs.compose.runtime)
            implementation(libs.compose.foundation)
            implementation(libs.compose.material3)
            implementation(libs.compose.ui)
            implementation(libs.compose.components.resources)
            implementation(libs.compose.uiToolingPreview)
            implementation(libs.androidx.lifecycle.viewmodelCompose)
            implementation(libs.androidx.lifecycle.runtimeCompose)
        }
        commonTest.dependencies {
            implementation(libs.kotlin.test)
        }
//        jsMain.dependencies {
//            implementation(libs.wrappers.browser)
//        }
        jvmMain.dependencies {
            implementation(compose.desktop.currentOs)
            implementation(libs.compose.uiToolingPreview)
        }
    }
}