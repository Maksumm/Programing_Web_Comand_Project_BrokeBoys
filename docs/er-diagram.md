# Схема бази даних (PostgreSQL, EF Core)

## Від полів екранів до таблиць

Поля, які показують екрани: **Календар** — назва товару, місяць, ціна; **Категорії** — назва, опис, кількість підписок; **Підписки** — e-mail, категорія, поріг, «активна».

| Нормальна форма | Що зроблено |
|---|---|
| 1НФ | Кожне поле атомарне; ціни за 12 місяців — не 12 колонок і не список в одній комірці, а 12 рядків у `SeasonalPrices`. |
| 2НФ | Усі таблиці мають одноколонковий ключ `Id`, тому часткових залежностей від частини ключа немає. Природні ключі («товар + місяць», «користувач + категорія») закріплені унікальними індексами. |
| 3НФ | Транзитивних залежностей немає: категорія описується лише в `Categories`, товар — лише в `Products` (у цінах і підписках є тільки посилання `ProductId` / `CategoryId`). Кількість підписок і товарів **не зберігається** — вона обчислюється запитом `COUNT`, інакше її довелося б синхронізувати. Назви й описи двома мовами (`…Uk`, `…En`) — це різні атрибути однієї сутності, а не повтор. |

## ER-діаграма

```mermaid
erDiagram
    Category ||--o{ Product : "має (ON DELETE RESTRICT)"
    Product ||--o{ SeasonalPrice : "має ціни (ON DELETE CASCADE)"
    Category ||--o{ Subscription : "отримує (ON DELETE CASCADE)"

    Category {
        int Id PK
        string NameUk UK "унікальне"
        string NameEn UK "унікальне"
        string DescriptionUk
        string DescriptionEn
    }
    Product {
        int Id PK
        int CategoryId FK "унікальне разом з NameUk, окремо — з NameEn"
        string NameUk
        string NameEn
    }
    SeasonalPrice {
        int Id PK
        int ProductId FK "унікальне разом з Month"
        int Month "CHECK 1..12"
        decimal Price "CHECK більше 0"
    }
    Subscription {
        int Id PK
        string OwnerId "oid користувача з токена; унікальне разом з CategoryId"
        string Email "куди надсилати; нижній регістр"
        int CategoryId FK
        decimal Threshold "NULL або 0,01..1000"
        bool IsActive
    }
```

## Унікальні індекси

| Таблиця | Індекс | Навіщо |
|---|---|---|
| `Categories` | `NameUk`, `NameEn` (окремо) | дві категорії не можуть мати однакову назву жодною мовою |
| `Products` | (`CategoryId`, `NameUk`), (`CategoryId`, `NameEn`) | назва товару не повторюється в межах категорії; індекс обслуговує й зовнішній ключ |
| `SeasonalPrices` | (`ProductId`, `Month`) | один товар має одну ціну на місяць |
| `Subscriptions` | (`OwnerId`, `CategoryId`) | один користувач підписується на категорію один раз; `OwnerId` — `oid` з токена Entra ID, не змінюється разом із поштою (e-mail зберігається в нижньому регістрі) |

Порушення унікальності (SQLSTATE `23505`) API повертає як **409** з `ProblemDetails`.

## Правила `OnDelete`

| Зовнішній ключ | Правило | Чому |
|---|---|---|
| `Products.CategoryId` → `Categories` | **Restrict** | категорію з товарами видалити не можна — товари не мають зникати разом з нею; API повертає 409 |
| `SeasonalPrices.ProductId` → `Products` | **Cascade** | ціни не існують без товару |
| `Subscriptions.CategoryId` → `Categories` | **Cascade** | підписка на видалену категорію втрачає сенс |

## Команди

```bash
docker compose up -d
dotnet user-secrets set "ConnectionStrings:Catalogue" "Host=localhost;Port=5433;Database=catalogue;Username=postgres;Password=postgres" --project ApiApp
dotnet tool install --global dotnet-ef            # або: dotnet tool update --global dotnet-ef
dotnet ef migrations add InitialCreate --project ApiApp
dotnet ef database update --project ApiApp
```

Схема змінюється лише міграціями (`dotnet ef database update`); застосунок сам таблиць не створює.
