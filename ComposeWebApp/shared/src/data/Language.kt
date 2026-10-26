package org.codebench.composewebapp.data

enum class Language {
    Cpp,
    Python,
    Java,
    CSharp;

    override fun toString(): String {
        return when (this) {
            Cpp -> "C++"
            Python -> "Python"
            Java -> "Java"
            CSharp -> "C#"
        }
    }
}