# FFH v1.177 Android APK Builder

Автоматическая попытка восстановить Unity-проект из Windows-сборки Femboy Futa House v1.177, добавить мобильное управление и собрать ARM64 Android APK.

## Что делает workflow
1. Скачивает исходный ZIP с Google Drive.
2. Извлекает внутренний AES ZIP.
3. Восстанавливает Unity-проект через AssetRipper.
4. Добавляет touch-контролы и Touchscreen bindings.
5. Собирает Android ARM64 APK через Unity 6000.2.6f2.
6. Если сборка падает — сохраняет восстановленный проект и логи для следующего исправления.
