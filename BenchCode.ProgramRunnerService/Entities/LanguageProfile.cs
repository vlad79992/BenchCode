using Docker.DotNet.Models;

namespace BenchCode.ProgramRunnerService.Entities;


/// <summary>
/// Базовый класс, от которого наследуются классы для запуска всех языков
/// </summary>
public abstract class LanguageProfile
{
    /// <summary>
    /// Параметры для каждой из стадий (компиляция и запуск) языка
    /// </summary>
    /// <param name="Image">
    /// Образ контейнера для выполнения команд на текущем этапе. <br/>
    /// Имя образа указывается либо в формате <c>&lt;имя&gt;:&lt;версия&gt;</c>, либо <c>&lt;имя&gt;</c>. 
    /// Во втором случае используется <c>&lt;имя&gt;:latest</c> версия контейнера
    /// </param>
    /// <param name="Cmd">Команды, выполняемые на текущем этапе (для компиляции или запуска кода)</param>
    /// <param name="Limits">Ограничения контейнера</param>
    public sealed record StageOptions
    (
        string Image,
        string[] Cmd,
        SandboxLimits Limits
    );
    /// <summary>
    /// Задает лимиты для контейнеров
    /// </summary>
    /// <param name="MemoryBytes"></param>
    /// <param name="NanoCpus"></param>
    /// <param name="PidsLimit"></param>
    /// <param name="Timeout"></param>
    /// <param name="WorkspaceBytes"></param>
    public sealed record SandboxLimits
    (
        long MemoryBytes,
        long NanoCpus,
        int PidsLimit,
        TimeSpan Timeout,
        long WorkspaceBytes
    );
    /// <summary>
    /// Краткое название профиля <br/> 
    /// <example>
    /// Например для C++ кратким названием будет "cpp"
    /// </example>
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// Имя файла с исходным кодом внутри папки <c>/workspace</c>, например "main.cpp" <br/>
    /// <br/>
    /// Путь <c>/workspace</c> задан по умолчанию в <see cref="Sandbox(StageOptions)"/> в свойстве <see cref="CreateContainerParameters.WorkingDir"/>
    /// </summary>
    public abstract string SourcePath { get; }
    /// <summary>
    /// Полный путь к артефакту сборки внутри контейнера для компиляции
    /// </summary>
    public virtual string ArtifactPath => "/workspace/main";
    
    /// <summary>
    /// Параметры контейнера для этапа компиляции
    /// </summary>
    protected abstract StageOptions BuildOptions { get; }
    /// <summary>
    /// Параметры контейнера для этапа исполнения кода
    /// </summary>
    protected abstract StageOptions RunOptions { get; }
    /// <summary>
    /// Функция для получения контейнера для компиляции кода
    /// </summary>
    /// <returns>Возвращает изолированную песочницу для компиляции кода</returns>
    public CreateContainerParameters BuildCompilerContainer() => Sandbox(BuildOptions);
    /// <summary>
    /// Функция для получения контейнера для запуска кода
    /// </summary>
    /// <returns>Возвращает изолированную песочницу для запуска кода</returns>
    public CreateContainerParameters BuildRunnerContainer() => Sandbox(RunOptions);

    /// <summary>
    /// Список всех образов, которые необходимы для  текущего языка
    /// </summary>
    public IReadOnlyList<string> RequiredImages => [BuildOptions.Image, RunOptions.Image];
    public TimeSpan BuildTimeout => BuildOptions.Limits.Timeout;
    public TimeSpan RunTimeout => RunOptions.Limits.Timeout;

    /// <summary>
    /// Функция для получения стандартных настроек изолированного контейнера
    /// </summary>
    /// <param name="options">Специфичные параметры создаваемого контейнера</param>
    /// <returns>Изолированный контейнер созданный с образом, командами и ограничениями ресурсов из <paramref name="options"/> </returns>
    protected static CreateContainerParameters Sandbox(StageOptions options) => new()
    {
        Image = options.Image,
        WorkingDir = "/workspace",
        Cmd = options.Cmd,
        User = "65534:65534",
        Tty = false,
        NetworkDisabled = true,
        Env = ["TMPDIR=/tmp", "HOME=/tmp"],
        HostConfig = new()
        {
            NetworkMode = "none",
            Memory = options.Limits.MemoryBytes,
            MemorySwap = options.Limits.MemoryBytes,
            NanoCPUs = options.Limits.NanoCpus,
            PidsLimit = options.Limits.PidsLimit,
            CapDrop = ["ALL"],
            SecurityOpt = ["no-new-privileges:true"],
            LogConfig = new()
            {
                Type = "json-file",
                Config = new Dictionary<string, string> { ["max-size"] = "1m", ["max-file"] = "1" }
            }
        }
    };
}