## 1. Use Case Diagram (PlantUML)

```plantuml
@startuml BenchCode_Use_Case
left to right direction
skinparam packageStyle rectangle
skinparam shadowing false

actor "Пользователь" as User
actor "Владелец развёртывания" as Deployer
actor "Docker Runtime" as Docker <<external system>>

rectangle "Платформа BenchCode" {
  usecase "UC-01\nРегистрация и вход" as UC01
  usecase "UC-14\nВосстановление доступа" as UC14
  usecase "UC-11\nМои соревнования" as UC11
  usecase "UC-02\nПрисоединение по коду" as UC02
  usecase "UC-03\nПросмотр задач и статусов" as UC03
  usecase "UC-04\nОтправка решения" as UC04
  usecase "UC-12\nМои посылки" as UC12
  usecase "UC-05\nРезультаты и рейтинг" as UC05
  usecase "UC-06\nПанель управления (Owner)" as UC06
  usecase "UC-07\nСоздание соревнования" as UC07
  usecase "UC-08\nЗадачи и тесты (Owner)" as UC08
  usecase "UC-13\nНастройки и участники (Owner)" as UC13
  usecase "UC-09\nРеестр посылок задачи (Owner)" as UC09
  usecase "UC-15\nЭкспорт результатов" as UC15 #EEEEEE
  usecase "UC-10\nИзолированный прогон и замер" as UC10
}

User --> UC01
User --> UC14
User --> UC11
User --> UC02
User --> UC03
User --> UC04
User --> UC12
User --> UC05
User --> UC06
User --> UC07
User --> UC08
User --> UC13
User --> UC09
User --> UC15
Deployer --> UC14
UC10 --> Docker

UC11 ..> UC01 : <<include>>
UC02 ..> UC01 : <<include>>
UC04 ..> UC10 : <<include>>
UC07 ..> UC08 : <<include>>
UC06 ..> UC08 : <<include>>
UC06 ..> UC09 : <<include>>
UC06 ..> UC13 : <<include>>
UC06 ..> UC05 : <<include>>

note right of UC10
  Выполняется Воркером — компонентом системы.
  Воркер актором не является.
  Docker Runtime — внешняя система.
end note

note right of UC14
  Организационная процедура: сброс пароля
  выполняется владельцем развёртывания
  вне интерфейса системы.
end note

note bottom of UC15
  Should have. Вызывается из панели
  управления (UC-06).
end note

legend right
  Один тип учётной записи.
  Права Owner / Participant существуют
  только в рамках соревнования.
  Светлый фон — Should have,
  остальные сценарии — Must have.
endlegend
@enduml
```

**Что исправлено относительно прежней версии**
- Один пользовательский актор вместо «Студента» и «Преподавателя»; сценарий выбора режима удалён.
- Добавлен UC-01 (регистрация и вход), которого не было на диаграмме.
- Ассоциации пользователя со всеми сценариями, включая создание соревнования и задач.
- `<<extend>>` от создания соревнования к «входу по паролю» заменён на `<<include>>` к сценариям управления; вход по мастер-паролю отсутствует.
- Воркер исключён из акторов; Docker Runtime обозначен как внешняя система.
- Добавлены UC-11…UC-15; приоритет Should выделен визуально.

---

## 2. Диаграммы последовательностей (PlantUML)

Общее для всех сценариев: каждый запрос содержит токен доступа; при недействительном токене возвращается 401 (в схемах не показан отдельно); время хранится в UTC; префикс API отсутствует.

### 2.1 Присоединение к соревнованию по коду (UC-02)

```plantuml
@startuml BenchCode_Seq_Join
autonumber
actor "Пользователь" as user
participant "Frontend (WASM)" as site
participant "Backend API" as api
participant "Проверка прав" as authz
database "PostgreSQL" as db

user -> site: Вводит код, «Присоединиться»
activate site
site -> api: POST competitions/{code}/join
activate api
api -> authz: Токен, лимит попыток присоединения
activate authz
authz --> api: userId / отказ
deactivate authz
api -> db: Поиск опубликованного соревнования по коду
activate db
db --> api: Результат поиска
deactivate db
alt Превышен лимит попыток
  api --> site: 429 Too Many Requests
  site --> user: «Слишком много попыток. Повторите через минуту»
else Соревнование не найдено или не опубликовано
  api --> site: 404 Not Found
  site --> user: «Соревнование с таким кодом не найдено»
else Соревнование найдено
  api -> db: INSERT участия (Participant), повтор не создаёт дубликат
  activate db
  db --> api: Подтверждение
  deactivate db
  api --> site: 200 OK (competitionId)
  site --> user: Страница соревнования (UC-03)
end
deactivate api
deactivate site
@enduml
```

### 2.2 Полная отправка решения (UC-04, UC-10)

```plantuml
@startuml BenchCode_Seq_Submit
autonumber
actor "Пользователь" as user
participant "Frontend (WASM)" as site
participant "Backend API" as api
participant "Проверка прав" as authz
database "PostgreSQL" as db
queue "RabbitMQ" as mq
participant "Воркер" as worker
participant "Docker Runtime" as docker

user -> site: Выбирает язык, вводит код, «Отправить на проверку»
activate site
site -> api: POST competitions/{id}/tasks/{taskId}/submit (язык, код)
activate api
api -> authz: Токен, право участия, лимит посылок, размер кода, язык
activate authz
authz -> db: Запись участия, задача, дедлайн, версия
activate db
db --> authz: Данные
deactivate db
authz --> api: Разрешено / отказ
deactivate authz
alt Отказ в приёме
  api --> site: 400 / 403 / 429
  site --> user: Сообщение об ошибке, посылка не создана
else Посылка принята
  api -> db: INSERT посылки (Status=Testing, CompetitionVersion, IsLate)
  activate db
  db --> api: submissionId
  deactivate db
  api -> mq: Публикация задания (submissionId)
  activate mq
  mq --> api: Задание принято
  deactivate mq
  api --> site: 202 Accepted (submissionId)
  site --> user: Статус «Тестируется»
end
deactivate api

group Асинхронная обработка принятой посылки
  mq -> worker: Задание на прогон
  activate worker
  worker -> db: Тесты задачи (публичные и скрытые)
  activate db
  db --> worker: Тесты
  deactivate db
  worker -> docker: Компиляция (30 с, 2 ГБ), прогон с лимитами задачи
  activate docker
  docker --> worker: Вердикты, время, память по тестам
  deactivate docker
  worker -> db: UPDATE посылки (Finished / SystemError), INSERT результатов по тестам
  activate db
  db --> worker: Подтверждение
  deactivate db
  deactivate worker
end

loop Опрос состояния до завершения
  site -> api: GET competitions/{id}/tasks/{taskId}/submissions/{submissionId}
  activate api
  api -> db: Чтение посылки и результатов по тестам
  activate db
  db --> api: Данные
  deactivate db
  api --> site: 200 OK (статус, вердикт, метрики)
  deactivate api
end
site --> user: Вердикт, время, память; детали только по публичным тестам
deactivate site
@enduml
```

### 2.3 Прогон на публичных тестах (UC-04, альтернативный поток)

```plantuml
@startuml BenchCode_Seq_PublicRun
autonumber
actor "Пользователь" as user
participant "Frontend (WASM)" as site
participant "Backend API" as api
participant "Проверка прав" as authz
queue "RabbitMQ" as mq
participant "Воркер" as worker
participant "Docker Runtime" as docker

user -> site: «Проверить на публичных тестах»
activate site
site -> api: POST competitions/{id}/tasks/{taskId}/test (язык, код)
activate api
api -> authz: Токен, право участия, лимит частоты, размер кода
activate authz
authz --> api: Разрешено / отказ
deactivate authz
alt Отказ
  api --> site: 400 / 403 / 429
  site --> user: Сообщение об ошибке
else Разрешено
  api -> mq: Задание на публичный прогон (без записи посылки)
  activate mq
  mq -> worker: Публичные тесты задачи
  activate worker
  worker -> docker: Компиляция и прогон на публичных тестах
  activate docker
  docker --> worker: Вердикты, время, память, вывод
  deactivate docker
  worker --> mq: Результат прогона
  deactivate worker
  mq --> api: Результат прогона
  deactivate mq
  api --> site: 200 OK (результаты по публичным тестам)
  site --> user: Результат на экране, в БД не сохраняется
end
deactivate api
deactivate site
@enduml
```

Публичный прогон выполняется синхронно: число тестов ограничено, повторы для замера не выполняются. Перевод на асинхронную схему — открытый пункт.

### 2.4 Таблица результатов (UC-05)

```plantuml
@startuml BenchCode_Seq_Leaderboard
autonumber
actor "Пользователь" as user
participant "Frontend (WASM)" as site
participant "Backend API" as api
participant "Проверка прав" as authz
database "PostgreSQL" as db

user -> site: «Результаты»
activate site
site -> api: GET competitions/{id}/leaderboard
activate api
api -> authz: Токен, право участия
activate authz
authz --> api: Разрешено / отказ
deactivate authz
alt Право отсутствует
  api --> site: 403 Forbidden
  site --> user: «Доступ запрещён»
else Право подтверждено
  api -> db: Посылки текущей версии, IsLate=false, вердикт AC; участники; настройки
  activate db
  db --> api: Данные
  deactivate db
  api -> api: Лучшие AC по задачам, максимумы по тестам, нормирование, ранжирование
  api --> site: 200 OK (таблица, средние показатели, признак доступности чужого кода)
  site --> user: Таблица результатов
end
deactivate api
deactivate site
@enduml
```

### 2.5 Создание соревнования и задачи (UC-07, UC-08)

```plantuml
@startuml BenchCode_Seq_Create
autonumber
actor "Пользователь" as user
participant "Frontend (WASM)" as site
participant "Backend API" as api
participant "Проверка прав" as authz
database "PostgreSQL" as db

user -> site: Заполняет форму; дедлайн в локальном времени
activate site
site -> site: Отображение часового пояса, преобразование дедлайна в UTC
site -> api: POST competitions (название, дедлайн?, веса, флаг, лимиты, тег группы)
activate api
api -> authz: Токен, квота активных соревнований
activate authz
authz --> api: Разрешено / отказ
deactivate authz
alt Ошибка валидации или квота превышена
  api --> site: 400 / 429
  site --> user: Сообщение об ошибке
else Черновик создан
  api -> db: INSERT соревнования (Id=код 6 символов, Status=Draft, Version=1) и участия (Owner)
  activate db
  db --> api: Подтверждение
  deactivate db
  api --> site: 201 Created (код соревнования)
  site --> user: Панель управления, код и ссылка-приглашение
end
deactivate api

user -> site: Добавляет задачу: условие, лимиты, языки, способ проверки, тесты
site -> api: POST competitions/{id}/tasks
activate api
api -> authz: Токен, право Owner
activate authz
authz --> api: Разрешено / отказ
deactivate authz
alt Право отсутствует или лимиты тестов превышены
  api --> site: 403 / 400
  site --> user: Сообщение об ошибке
else Задача сохранена
  api -> db: INSERT задачи и тестов (проверка 1 МБ / 10 МБ / 100 тестов)
  activate db
  db --> api: taskId
  deactivate db
  api --> site: 201 Created (taskId)
  site --> user: Задача в списке
end
deactivate api

user -> site: «Опубликовать»
site -> api: POST competitions/{id}/publish
activate api
api -> authz: Токен, право Owner
activate authz
authz --> api: Разрешено
deactivate authz
api -> db: Проверка наличия задач, UPDATE Status=Published
activate db
db --> api: Подтверждение
deactivate db
api --> site: 200 OK
site --> user: Соревнование опубликовано, код доступен для рассылки
deactivate api
deactivate site
@enduml
```

Изменение задач, тестов, лимитов или весов у опубликованного соревнования с посылками увеличивает `Version`; в рейтинг попадают только посылки текущей версии (BR-19). Отдельного сценария перепроверки нет.

---

## 3. Диаграммы состояний

### 3.1 Жизненный цикл посылки

```plantuml
@startuml BenchCode_State_Submission
hide empty description

[*] --> Testing : Полная посылка сохранена\nи поставлена в очередь
Testing --> Finished : Проверка завершена\nс вердиктом
Testing --> SystemError : Сбой очереди,\nворкера или Docker Runtime
SystemError --> Testing : Ручной повторный запуск
Finished --> [*]
SystemError --> [*]

note right of Finished
  Допустимые вердикты:
  AC, WA, CE, RE, TLE, MLE
end note
@enduml
```

`SystemError` обозначает сбой платформы, а не вердикт пользовательского решения. При ручном повторном запуске посылка снова переходит в `Testing`.

### 3.2 Жизненный цикл соревнования

```plantuml
@startuml BenchCode_State_Competition
hide empty description

[*] --> Draft : Создание
Draft --> Published : Есть хотя бы одна\nвалидная задача
Published --> Archived : Архивация
Draft --> [*] : Удаление,\nесли нет посылок
Archived --> [*]
@enduml
```
