# Build Orchestrator

Build Orchestrator — пакет инструментов для Unity с профилями сборки:
- автоувеличение версии
- управление define symbols
- шаблоны именования сборок
- запуск сборки
- постобработка и упаковка в zip
- кастомные действия по стадиям пайплайна

## Совместимость с Unity

- Unity: `2022.3` и новее в рамках той же LTS-линейки

## Установка (Git URL)

Добавьте зависимость в `Packages/manifest.json` проекта:

```json
{
  "dependencies": {
    "com.underdogg.build-orchestrator": "https://github.com/und3rd0gg/unity-build-orchestrator.git#v0.1.0"
  }
}
```

## Первый запуск

1. Откройте `Window/Package Manager`.
2. Выберите `Build Orchestrator`.
3. В разделе `Samples` импортируйте `Default BuildPipeline Config`.
4. Откройте меню `Tools/Build/Build Pipeline Manager`.
5. Проверьте импортированный `BuildPipelineConfig.asset` и настройте профили/флаги.

## Меню

- `Tools/Build/Build Pipeline Manager`
- `File/Build Profiles/Build Pipeline Manager`
- `Tools/Build/Create Build Pipeline Config`
- `File/Build Profiles/Create Build Pipeline Config`

## Примечания

- Пакет создает новые конфиг-ассеты в `Assets/BuildPipeline/Config`.
- Define symbols сохраняются в `PlayerSettings`.
- Dev-оверлей версии появляется только при `TL_BUILD_DEV` или `BUILD_DEV`.

## Структура пакета

- `Editor/` инструменты редактора и сервисы пайплайна сборки
- `Runtime/` runtime-компоненты
- `Samples~/DefaultConfig/` стартовый sample-конфиг

## Лицензия

MIT. См. `LICENSE.md`.
