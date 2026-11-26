namespace BenchCode.Shared.Entities;

/// <summary>
/// Общий результат тестирования программы
/// </summary>
public enum Verdict
{
    /// <summary>
    /// Успешное прохождение теста без ошибок
    /// </summary>
    OK  = 0,
    /// <summary>
    /// Неверный ответ (англ. Wrong Answer)
    /// </summary>
    WA  = 1,
    /// <summary>
    /// Превышение лимита по времени (англ. Time Limit Exceeded)
    /// </summary>
    TLE = 2,
    /// <summary>
    /// Превышение лимита по памяти (англ. Memory Limit Exceeded)
    /// </summary>
    MLE = 3,
    /// <summary>
    /// Ошибка времени выполнения (англ. Runtime Error)
    /// </summary>
    RE = 4,
    /// <summary>
    /// Превышение лимита вывода (англ. Output Limit Exceeded)
    /// </summary>
    OLE = 5,
    /// <summary>
    /// Ошибка компиляции (англ. Compilation Error)
    /// </summary>
    CE = 6,
}