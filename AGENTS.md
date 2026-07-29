# AGENTS.md — справочник для AI-агентов

Документ описывает устройство проекта **EasyEPLANner** для AI-агентов, которые будут вносить изменения в код. Здесь собраны факты о технологическом стеке, сборке, тестировании, архитектуре и соглашениях, принятых в команде.

Язык документации и комментариев в коде — **русский**. Новые комментарии и документацию следует писать на русском языке.

---

## 1. Общее описание проекта

**EasyEPLANner** — open-source Add-In для **EPLAN Electric P8** (версия 2.9), который помогает автоматизировать разработку управляющих программ для ПЛК. С помощью надстройки в EPLAN описываются технологические объекты (танк, бойлер и т.п.), их операции, шаги операций, ограничения, устройства ввода-вывода и привязка каналов к модулям I/O. В результате генерируются **Lua**-файлы, загружаемые в контроллер.

Поддерживаемые контроллеры:

- Phoenix Contact — PLCnext;
- WAGO — PFC200.

Основной язык разработки — **C#** на **.NET Framework 4.7.2**. Для интеграции с EPLAN используется **EPLAN API**. Часть бизнес-логики и шаблоны проектов описаны на **Lua**.

---

## 2. Технологический стек и ключевые файлы

### 2.1. Платформа и языки

- **C# 7.x / C# preview** (задано `LangVersion=preview` в `src/EasyEPlanner.csproj`).
- **.NET Framework 4.7.2** — целевая платформа основного проекта и подмодулей.
- **Windows Forms** — пользовательский интерфейс.
- **Lua 5.1 / LuaInterface** — встраивание Lua-скриптов и выполнение пользовательских сценариев.
- **EPLAN API 2.9** — работа с проектами EPLAN.

### 2.2. Основные зависимости

- `Eplan.EplApi.*` — сборки EPLAN API (AFu, Baseu, DataModelu, EServicesu, Guiu, HEServicesu, Starteru). Локально они берутся из GAC при установленном EPLAN; в CI загружаются из приватного репозитория `savushkin-r-d/EPLAN-API-bin`.
- `Aga.Controls` и `ObjectListView` — UI-компоненты для деревьев и списков (подписанные сборки в `src/libs/`).
- `Spire.XLS`, `Spire.Pdf`, `Spire.License` — генерация Excel-отчётов.
- `LuaInterface.dll`, `KopiLua.dll` — взаимодействие C# ↔ Lua.
- `NUnit 3.13.3`, `Moq 4.20.69`, `Microsoft.NET.Test.Sdk 17.7.2` — юнит-тесты.

### 2.3. Ключевые файлы конфигурации

| Файл | Назначение |
|------|-----------|
| `src/EasyEPlanner.sln` | Главный файл решения Visual Studio. |
| `src/EasyEPlanner.csproj` | Основной проект надстройки (.NET Framework 4.7.2, библиотека). |
| `EasyEplanner.Tests/EasyEplanner.Tests.csproj` | Проект юнит-тестов (SDK-style, `net472`, NUnit + Moq). |
| `.github/workflows/msbuild.yml` | CI: сборка, тесты, SonarCloud, формирование артефакта и релиз. |
| `.github/workflows/autoviersion.yml` | Ежемесячное обновление версии в `src/Properties/AssemblyInfo.cs`. |
| `docs/codestyle.md` | Соглашения по оформлению кода. |
| `docs/developer_manual/ReadMe.md` | Руководство начинающего разработчика. |
| `docs/user_manual/ReadMe.md` | Пользовательская документация. |
| `src/Properties/AssemblyInfo.cs` | Версия и метаданные сборки. |
| `src/Lua/*.lua` | Вспомогательные Lua-скрипты, копируются в выходной каталог. |
| `configuration.ini` | Файл конфигурации надстройки, создаётся/изменяется при работе. |

---

## 3. Структура репозитория

```
EasyEPLANner/
├── src/                          # Основной исходный код
│   ├── Main/                     # Точка входа (AddInModule) и Action'ы EPLAN
│   ├── Configuration/            # Чтение/проверка конфигурации EPLAN-проекта
│   ├── Device/                   # Модели устройств и DeviceManager
│   │   ├── IODevices/            # Классы конкретных устройств (AI, AO, DI, DO, V, M и др.)
│   │   ├── View/                 # UI деревьев устройств
│   │   └── ViewModel/            # ViewModel для деревьев устройств
│   ├── Editor/                   # Редактор технологических объектов
│   ├── EProjectManager/          # Обертка над EPLAN API, события, интеракции
│   ├── FileSavers/               # Сохранение Lua, XML, Excel, SVG
│   ├── InterprojectExchange/     # Межпроектный обмен сигналами
│   ├── IO/                       # Модули ввода-вывода, узлы, IO-Link
│   ├── Logs/                     # Логгер для пользователя
│   ├── ModbusExchange/           # Обмен сигналами Modbus
│   ├── mpk/                      # Экспорт компонентов *.mpkx
│   ├── ProjectImportICP/         # Импорт проектов ICP CON (WAGO)
│   ├── PxcIolinkConfiguration/   # Генерация IOL-Conf
│   ├── StaticHelper/             # Статические хелперы
│   ├── TechObject/               # Модель технологических объектов
│   └── Lua/                      # Lua-скрипты и шаблоны
├── EasyEplanner.Tests/           # Юнит-тесты, структура повторяет src/
├── submodule/                    # Git-подмодули
│   ├── EplanIdleTimeModule/      # Модуль простоя EPLAN
│   ├── ObjectListview/           # UI-компонент ObjectListView
│   └── TreeViewAdv/              # UI-компонент TreeViewAdv (Aga.Controls)
└── docs/                         # Документация (ru, de, user_manual, developer_manual)
```

### 3.1. Проекты в решении

- `EasyEPlanner` — основная надстройка.
- `EasyEplanner.Tests` — юнит-тесты.
- `ObjectListView2012` — UI-компонент (дерево/список).
- `Aga.Controls` — UI-компонент (дерево).
- `EplanIdleTimeModule` — модуль простоя EPLAN.

---

## 4. Архитектура и runtime

### 4.1. Точка входа

`src/Main/AddInModule.cs` реализует `IEplAddIn` и `IEplAddInShadowCopy`. EPLAN вызывает этот класс при загрузке надстройки:

- `OnRegister` — регистрирует надстройку для загрузки при старте.
- `OnInitGui` — создаёт главное меню `EPlaner` и регистрирует `Action`-классы.
- `OnInit` — при старте может запустить внешний `EasyEPLANnerUpdater.exe` из папки `Updater\`.

### 4.2. Основные менеджеры

- `ProjectManager` — синглтон, координирует загрузку/сохранение проекта, генерацию отчётов.
- `EProjectManager` — обёртка над EPLAN API, управляет режимами редактирования, сохранением/загрузкой файлов.
- `DeviceManager` — управляет устройствами, их типами, параметрами, проверками.
- `IOManager` — управляет узлами и модулями ввода-вывода.
- `TechObjectManager` — управляет деревом технологических объектов.
- `BaseTechObjectManager` — загружает и хранит описание базовых объектов.

### 4.3. Поток данных

1. Пользователь работает в EPLAN: размещает устройства на ФСА, задаёт модули I/O, открывает редактор технологических объектов.
2. Надстройка считывает данные из EPLAN через `Configuration/*Reader` и `EProjectManager`.
3. Описание проекта хранится в оперативной памяти в виде деревьев `TechObject`, `Device`, `IO` и т.д.
4. При сохранении `FileSavers` генерируют Lua-файлы (`main.plua`, `main.io.lua`, `main.objects.lua`, `main.restrictions.lua`, `prg.lua` и др.), XML для SCADA и Excel-отчёты.

### 4.4. Lua-скрипты

В папке `src/Lua/` находятся скрипты, которые копируются в выходной каталог:

- `sys.lua` — загрузка описания проекта из Lua-файлов.
- `sys_base_objects_initializer.lua` — инициализация базовых объектов.
- `sys_io_modules.lua` — описание поддерживаемых модулей I/O.
- `sys_io_nodes.lua` — описание поддерживаемых узлов I/O.
- `sys_iolink_devices.lua` — описание IO-Link устройств.
- `sys_interproject_io.lua` — межпроектный обмен сигналами.
- `sys_shared_initializer.lua` — чтение `main.shared.lua`.
- `mainPattern.plua` — шаблон `main.plua`.

Описание базовых объектов (`sys_base_objects_description.lua`) поставляется из внешнего репозитория `ptusa-lua-diary-system` и не входит в основной репозиторий.

### 4.5. Генерируемые файлы проекта

Надстройка создаёт в каталоге проектов (указанном в `configuration.ini`) файлы:

- `main.plua` — управляющая программа.
- `main.io.lua` — привязка устройств к модулям I/O.
- `main.objects.lua` — описание технологических объектов.
- `main.restrictions.lua` — ограничения операций.
- `main.modbus_srv.lua` — Modbus-сервер.
- `prg.lua` — базовая функциональность объектов.
- `*.svg` — статистика по проекту.

---

## 5. Сборка

### 5.1. Подготовка

Перед сборкой необходимо инициализировать подмодули:

```bash
git submodule update --init --recursive
```

Для локальной сборки также требуется установленный **EPLAN Electric P8 2.9** (для доступа к сборкам EPLAN API из GAC).

### 5.2. Конфигурации сборки

Решение поддерживает три конфигурации:

- `Debug` — сборка отладочная, имя сборки `EPLAN.EplAddin.EasyEPlanner_debug.dll`, `TreatWarningsAsErrors=true`.
- `Release` — релизная сборка, имя `EPLAN.EplAddin.EasyEPlanner.dll`.
- `ReleaseSigned` — релизная сборка с подписью строгим именем (delay sign) для последующей подписи на сервере EPLAN.

Во всех конфигурациях `PlatformTarget=x64`.

### 5.3. Команды сборки

Восстановление NuGet-пакетов:

```bash
nuget restore src/EasyEPlanner.sln
```

Сборка решения:

```bash
msbuild /m /p:Configuration=Release src/EasyEPlanner.sln
```

Для отладки:

```bash
msbuild /m /p:Configuration=Debug src/EasyEPlanner.sln
```

Сборка через Visual Studio: открыть `src/EasyEPlanner.sln`, выбрать конфигурацию и нажать `F6` (или **Сборка → Собрать решение**).

### 5.4. Особенности подписанной сборки

Для production-сборки:

1. Переключить конфигурацию на `ReleaseSigned`.
2. Заменить ссылку на `EplanIdleTimeModule` в `src/EasyEPlanner.csproj` с проекта на уже подписанную сборку `EPLAN.EplAddin.IdleTimeModule.dll` из каталога пользовательской надстройки.
3. Использовать ключ `src/key/2112_Public.snk` (отложенная подпись).
4. После сборки запаковать `EPLAN.EplAddin.EasyEPlanner.dll` в ZIP и загрузить на сервер подписи EPLAN: https://service.eplan.de/exchange.
5. Скачать подписанную EPLAN сборку и распространять пользователям.

---

## 6. Тестирование

### 6.1. Юнит-тесты

- Фреймворк: **NUnit 3**.
- Моки: **Moq**.
- Проект: `EasyEplanner.Tests/EasyEplanner.Tests.csproj`.
- Целевая платформа тестов: `net472`.

Запуск тестов (после сборки):

```bash
cd EasyEplanner.Tests
dotnet test --no-build --configuration Release
```

Сборка и запуск тестов:

```bash
cd EasyEplanner.Tests
dotnet test --configuration Release
```

### 6.2. Структура тестов

Структура папок в `EasyEplanner.Tests/` повторяет структуру `src/`. Файлы тестов именуются с суффиксом `.Tests`. Рекомендуемая конвенция именования тестов:

```
UnitOfWork_StateUnderTest_ExpectedBehavior
```

Тестовые данные размещаются в подпапках `TestData` и копируются в выходной каталог (`CopyToOutputDirectory=PreserveNewest`).

### 6.3. Интеграционное тестирование

Полноценная проверка надстройки требует:

- Установленного EPLAN с лицензией, поддерживающей разработку API (для отладки неподписанных надстроек).
- Установленного Lua в системе.
- Файлов базовых объектов из репозитория `ptusa-lua-diary-system`.

В `src/CMD/` находятся текстовые скрипты `TestLuaAvailability.txt` и `TestProjectScript.txt`, которые используются `ProjectHealthChecker` для автоматического тестирования проекта через Lua.

### 6.4. CI/CD

GitHub Actions (`.github/workflows/msbuild.yml`):

1. Проверка членства в организации для `pull_request_target`.
2. Восстановление NuGet-пакетов.
3. Загрузка EPLAN API из приватного репозитория.
4. Сборка `Release` через MSBuild.
5. Запуск юнит-тестов и сбор покрытия (`dotnet-coverage`).
6. Анализ в SonarCloud.
7. Формирование артефакта `EasyEPLANner` из `src/bin/Release` (без `.pdb` и без сборок `Eplan.EplApi.*`).
8. При создании тега — автоматический GitHub Release с ZIP-архивом и ChangeLog.

Рабочий процесс `autoviersion.yml` ежемесячно обновляет версию в `src/Properties/AssemblyInfo.cs`.

---

## 7. Соглашения по коду

Основной источник: `docs/codestyle.md`.

### 7.1. Именование

- Имена переменных, объектов — **camelCase**, без подчёркиваний: `isVariable`.
- Имена классов, методов, структур — **PascalCase**: `MyClass`, `MyMethod()`.
- Не использовать транслитерацию русских слов в идентификаторах.

### 7.2. Форматирование

- Длина строки — **80 символов**.
- Отступ — **4 пробела**.
- В строке — **один оператор**.
- Фигурные скобки всегда присутствуют, даже для одиночных операторов.
- Стандартные настройки форматирования C# из Visual Studio.

### 7.3. Комментарии

- Комментарии пишутся на **русском языке**.
- Используется XML-документация (`<summary>`, `<param>`, `<returns>`, `<remarks>`).
- Пример оформления класса:

```csharp
//------------------------------------------------------------------------------
/// <summary>
/// Убрать определенный элемент из коллекции.
/// </summary>
class StepPath
{
}
```

### 7.4. Тесты

- Покрывать тестами новый функционал.
- Покрывать изменения в старом коде по мере возможности; избегать ради тестов масштабного рефакторинга legacy-кода.
- Имена тестов отражают проверяемый сценарий.

---

## 8. Безопасность и важные замечания

- **Подпись надстройки.** Production-сборка должна быть подписана ключами EPLAN. В репозитории находится только публичный ключ `src/key/2112_Public.snk` для delay sign. Приватный ключ у EPLAN.
- **Секреты CI.** В GitHub Actions используются секреты `EPLAN_API_BIN`, `SONAR_TOKEN`, `GITHUB_TOKEN`, `BOT_ACCESS_TO_EPLAN`. Они не должны попадать в код или логи.
- **Внешний процесс.** Надстройка запускает `Updater\EasyEPLANnerUpdater.exe` из каталога надстройки. При изменении логики обновлений учитывайте риски запуска сторонних процессов.
- **Выполнение Lua.** Надстройка выполняет Lua-скрипты из каталога проектов и каталога надстройки. Будьте внимательны с путями и валидацией входных данных.
- **P/Invoke.** Используются вызовы Win32 (`PInvokeUtil.cs`, `StaticHelper/PInvokeUtil.cs`). Изменения в этой области требуют проверки на x64.
- **Предупреждения как ошибки.** В конфигурации `Debug` включен `TreatWarningsAsErrors=true`.
- **Enum-значения.** При добавлении типов/подтипов устройств в `DeviceTypeEnum` и `DeviceSubTypeEnum` добавляйте значения **только в конец**, чтобы не нарушить совместимость с контроллером. Виртуальные устройства `DEV_VTUG`/`Y` должны идти в конце перечисления типов.
- **Базовые объекты.** Файл `sys_base_objects_description.lua` не хранится в основном репозитории; его источник — `ptusa-lua-diary-system`. Не генерируйте фиктивные базовые объекты, если не уверены в формате.

---

## 9. Установка и отладка надстройки

### 9.1. Подключение к EPLAN

1. Собрать надстройку (Debug) или скачать Release-версию.
2. Запустить EPLAN.
3. Открыть **Сервисные программы → API-Addins**.
4. Нажать **Загрузить**, выбрать файл `EPLAN.EplAddin.EasyEPlanner_debug.dll` (для отладки) или `EPLAN.EplAddin.EasyEPlanner.dll` (для production).
5. В меню EPLAN появится пункт **EPlaner**.

### 9.2. Отладка

- Для отладки используйте **неподписанную** Debug-сборку.
- Требуется лицензия EPLAN с компонентом разработки API.
- После загрузки надстройки в EPLAN подключитесь к процессу `EPLAN.exe` из Visual Studio (**Debug → Attach to process**).
- Критические ошибки в взаимодействии с EPLAN могут приводить к аварийному завершению EPLAN.

---

## 10. Полезные ссылки

- `docs/codestyle.md` — стиль кода.
- `docs/contributing.md` — как внести вклад.
- `docs/developer_manual/ReadMe.md` — детальное описание архитектуры.
- `docs/user_manual/ReadMe.md` — руководство пользователя.
- `ReadMe.md` — общее описание проекта.
- EPLAN API Help: https://www.eplan.help/en-us/Infoportal/Content/api/2023/index.html
