plugins {
    alias(libs.plugins.android.application)
}

    android {
    namespace = "org.codebench.composewebapp"
        compileSdk {
            version = release(37)
        }

        defaultConfig {
      applicationId = "org.codebench.composewebapp"
    minSdk = 33
    targetSdk = 37
    versionCode = 1
    versionName = "1.0"

    }

    buildTypes {
          release {
              optimization {
                  enable = true
                  packageScope = setOf("androidx.**", "kotlin.**", "kotlinx.**")
              }
          }
      }
        compileOptions {
            sourceCompatibility = JavaVersion.VERSION_11
            targetCompatibility = JavaVersion.VERSION_11
        }
    }

  dependencies {
      implementation(libs.androidx.core.ktx)
  }