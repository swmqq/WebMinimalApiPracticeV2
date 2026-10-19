# WebMinimalApiPracticeV2

REST API для учёта авторов и их книг на ASP.NET Core Minimal API. Данные хранятся в SQLite, документация доступна в Swagger UI.

## Возможности

- CRUD для авторов, книги создаются и изменяются вместе с автором;
- валидация входных данных с подробными ошибками по каждому полю;
- каскадное удаление: вместе с автором удаляются его книги;
- OpenAPI-спецификация и Swagger UI;
- миграции EF Core.

## Стек

- .NET 10, ASP.NET Core Minimal API
- Entity Framework Core 10 + SQLite
- OpenAPI (`Microsoft.AspNetCore.OpenApi`) + Swagger UI

## Структура проекта

```
WebMinimalApiPracticeV2/
├── Program.cs               # настройка приложения
├── Endpoints/
│   └── AuthorEndpoints.cs   # маршруты и обработчики /authors
├── Dtos/                    # модели входящих запросов
├── Validation/
│   └── AuthorValidator.cs   # правила валидации
├── Data/
│   ├── ApiDbContext.cs
│   └── Entities/            # AuthorEntity, BookEntity
├── Migrations/
└── WebMinimalApiPracticeV2.http   # готовые запросы для проверки
```

## Запуск

Нужен [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone <ссылка-на-репозиторий>
cd <папка-репозитория>/WebMinimalApiPracticeV2

# один раз: инструмент миграций
dotnet tool install --global dotnet-ef

# создать базу данных Api.db по миграциям
dotnet ef database update

dotnet run
```

База `Api.db` в репозиторий не входит (она в `.gitignore`), поэтому её нужно создать командой `dotnet ef database update`.

Адреса:

- API: http://localhost:5231
- Swagger UI: http://localhost:5231/swagger
- OpenAPI-спецификация: http://localhost:5231/openapi/v1.json

Документация (Swagger и спецификация) доступна только в режиме Development, он включён по умолчанию при `dotnet run`. HTTPS-адрес `https://localhost:7003` доступен в профиле `https`: `dotnet run --launch-profile https`.

Строка подключения лежит в `appsettings.json` (`Data Source=Api.db`).

### Почему `localhost`

Проект учебный и не размещён на сервере, поэтому API запускается на компьютере разработчика и слушает адрес `localhost` (он же `127.0.0.1`). Это значит:

- API доступно только с того же компьютера, из интернета и локальной сети к нему не подключиться;
- порты (`5231` для HTTP и `7003` для HTTPS) заданы в `Properties/launchSettings.json`, и при необходимости их можно поменять там;
- клиенты, которые работают с API, в том числе Telegram-бот, должны запускаться на той же машине, что и API;
- для HTTPS на `localhost` используется сертификат разработчика .NET, который не подписан доверенным центром. Поэтому в боте отключена проверка сертификата (`verify=False`), и делается это только для локальной разработки.

Для публикации API на сервере понадобятся реальный адрес, настоящий сертификат и отдельная настройка окружения: Swagger и сертификат разработчика в рабочем режиме не используются.

## Модель данных

Отдельных эндпоинтов для книг нет: они читаются, создаются и изменяются только вместе с автором.

**Author**

| Поле | Тип | Правила |
|------|-----|---------|
| `id` | int | Присваивается базой |
| `name` | string | Обязательное, не пустое, до 150 символов |
| `age` | int | От 1 до 120 |
| `books` | array | Обязательное поле, может быть пустым массивом |

**Book** (вложена в автора)

| Поле | Тип | Правила |
|------|-----|---------|
| `id` | int | Присваивается базой |
| `title` | string | Обязательное, не пустое |
| `publishedYear` | int | От 1 до текущего года |

## Эндпоинты

| Метод | Путь | Описание | Успех | Ошибки |
|-------|------|----------|-------|--------|
| GET | `/authors` | Все авторы с книгами, по возрастанию `id` | 200 | — |
| GET | `/authors/{id}` | Автор по `id` с книгами | 200 | 404 |
| POST | `/authors` | Создать автора с книгами | 201 + `Location` | 400 |
| PUT | `/authors/{id}` | Обновить автора и его книги | 200 | 400, 404 |
| DELETE | `/authors/{id}` | Удалить автора вместе с книгами | 204 | 404 |

`{id}` должен быть целым числом, иначе маршрут не совпадёт и вернётся 404.

### GET /authors, GET /authors/{id}

```json
{
  "id": 1,
  "name": "Александр Пушкин",
  "age": 37,
  "books": [
    { "id": 1, "title": "Евгений Онегин", "publishedYear": 1833 }
  ]
}
```

`GET /authors` возвращает массив таких объектов (на пустой базе `[]`).

### POST /authors

Запрос:

```json
{
  "name": "Александр Пушкин",
  "age": 37,
  "books": [
    { "title": "Евгений Онегин", "publishedYear": 1833 }
  ]
}
```

Ответ `201 Created` с заголовком `Location: /authors/1` и созданным автором в теле (в том же формате, что выше, уже с присвоенными `id`).

### PUT /authors/{id}

Запрос:

```json
{
  "name": "А. С. Пушкин",
  "age": 37,
  "books": [
    { "id": 1, "title": "Евгений Онегин (изд. 2)", "publishedYear": 1833 },
    { "title": "Капитанская дочка", "publishedYear": 1836 }
  ]
}
```

Как обрабатывается массив `books`:

- элемент с `id` обновляет существующую книгу этого автора;
- элемент без `id` добавляется как новая книга;
- книги, которых нет в запросе, не изменяются и не удаляются.

Ответ `200 OK` содержит обновлённого автора со всеми его книгами.

Если в `books` указан `id` книги, которой нет у этого автора, вернётся `400` и ничего не сохранится. Тело запроса проверяется раньше, чем ищется автор, поэтому некорректный запрос на несуществующий `id` вернёт `400`, а не `404`.

### DELETE /authors/{id}

Ответ `204 No Content`. Книги автора удаляются каскадно.

## Ошибки

**Ошибки валидации** (`400`, POST и PUT) приходят в стандартном формате `ProblemDetails`. В поле `errors` перечислены все неверные поля сразу, ключ показывает, где именно ошибка:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "name": ["..."],
    "age": ["..."],
    "books[0].title": ["..."],
    "books[0].publishedYear": ["..."]
  }
}
```

Ключи в `errors`: `name`, `age`, `books`, а для ошибок в книгах `books[<индекс>].title` и `books[<индекс>].publishedYear`.

**Остальные ошибки** (`404`, а также `400` «книга не найдена у автора») возвращаются в виде:

```json
{ "message": "Текст ошибки" }
```

Если тело запроса не удалось разобрать (например, в `age` пришла строка), ASP.NET вернёт стандартный `400` с полем `detail`.

## Примеры запросов

Создать автора:

```bash
curl -X POST http://localhost:5231/authors \
  -H "Content-Type: application/json" \
  -d '{"name":"Александр Пушкин","age":37,"books":[{"title":"Евгений Онегин","publishedYear":1833}]}'
```

Получить список и одного автора:

```bash
curl http://localhost:5231/authors
curl http://localhost:5231/authors/1
```

Обновить автора и добавить книгу:

```bash
curl -X PUT http://localhost:5231/authors/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"А. С. Пушкин","age":37,"books":[{"title":"Капитанская дочка","publishedYear":1836}]}'
```

Удалить автора:

```bash
curl -X DELETE http://localhost:5231/authors/1
```

Запрос с ошибкой валидации (`400`):

```bash
curl -X POST http://localhost:5231/authors \
  -H "Content-Type: application/json" \
  -d '{"name":"","age":0,"books":[]}'
```

В PowerShell вместо `curl` используйте `curl.exe` и экранируйте кавычки, либо проще проверяйте запросы через Swagger UI или файл `WebMinimalApiPracticeV2.http` (Visual Studio, Rider, VS Code с REST Client). В файле собраны запросы на все сценарии: успешные, ошибки валидации, `404` и удаление.

## Telegram-бот

Бот на Python работает с этим API: `<ссылка-на-репозиторий-бота>`.

## Планы

- отдельные эндпоинты для книг и удаление книги через API;
- единый формат ошибок для всех ответов (`ProblemDetails`);
- автоматические тесты (xUnit + `WebApplicationFactory`);
- Docker.
