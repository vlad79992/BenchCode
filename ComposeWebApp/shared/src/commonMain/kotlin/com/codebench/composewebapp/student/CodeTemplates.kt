package com.codebench.composewebapp.student

object CodeTemplates {
    private val templates = mapOf(
        "cpp" to """
            #include <iostream>
            
            int main() {
                std::cout << "Hello world!" << std::endl;
                return 0;
            }
        """.trimIndent(),

        "kotlin" to """
            fun main() {
                println("Hello, World!")
            }
        """.trimIndent(),

        "java" to """
            public class Main {
                public static void main(String[] args) {
                    System.out.println("Hello, World!");
                }
            }
        """.trimIndent(),

        "javascript" to """
            function sayHello() {
                console.log("Hello, World!");
            }
            
            sayHello();
        """.trimIndent(),

        "python" to """
            def main():
                print("Hello, World!")

            if __name__ == "__main__":
                main()
        """.trimIndent(),
        "C#" to """
            Console.WriteLine("Hello, World!");
        """.trimIndent()
    )

    /**
     * Возвращает список всех доступных языков программирования.
     */
    val availableLanguages: List<String> = templates.keys.toList()

    /**
     * Возвращает шаблон кода для указанного языка или пустую строку, если язык не найден.
     */
    fun getTemplate(language: String): String = templates[language] ?: ""
}
