# LearnToShame

A MAUI application for programmer motivation and "endurance" training.

## Setup

1. Open the solution in Visual Studio or Rider.
2. Ensure you have the .NET MAUI workload installed (`dotnet workload install maui`).
3. Build and run on Android or Windows.

## Features

- **Roadmap**: Track your developer journey from Intern to Lead.
- **Shop**: Earn points by completing tasks and buy training sessions.
- **Training Session**: Test your endurance with content from Reddit (r/PrejacLevelTraining), filtered by your developer level.
- **Reddit Integration**: Fetches images based on level flairs.

## Architecture

- **MVVM**: Used throughout with CommunityToolkit.Mvvm.
- **SQLite**: Local data persistence.
- **MAUI**: Cross-platform UI.

## Notes

- The Reddit API integration uses public JSON endpoints for simplicity.
- Ensure you have internet access for fetching Reddit content.

### Консоль при запуске на iOS

При запуске в симуляторе или без отладчика в консоли могут появляться сообщения — на работу приложения они не влияют:

- **`Socket error while connecting to IDE on 127.0.0.1:10000: Connection refused`** — приложение пытается подключиться к IDE (hot reload). Если запуск идёт не из Rider/VS или без отладчика, соединение не устанавливается; это нормально.
- **`UIScene lifecycle will soon be required`** — предупреждение iOS о будущем переходе на сцены. Полная поддержка потребует изменений в .NET MAUI; пока приложение работает в текущем режиме.
