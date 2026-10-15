package org.codebench.composewebapp
// примеры того, как писать код, который зависит от платформы
// а еще эта функция используется в стандартных тестах
interface Platform {
    val name: String
}

expect fun getPlatform(): Platform
