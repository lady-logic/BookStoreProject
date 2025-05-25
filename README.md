# 📚 BookStoreApi

Ein einfaches ASP.NET Core Web API Projekt zum Verwalten von Büchern – mit dem Hauptfokus auf dem **Verständnis und der Anwendung von C#-Attributen**.

## 🎯 Ziel des Projekts

Dieses Projekt dient Lernzwecken. Es zeigt, wie man:

- **eigene Attribute** in C# erstellt (z. B. `CustomRoleAttribute`)
- **Filter** wie `ActionFilter` und `AuthorizationFilter` verwendet
- **JWT-Authentifizierung** integriert
- Attribute sinnvoll in einem ASP.NET Core Controller nutzt

## 🚀 Features

- 💘 CRUD-API für Bücher (`BooksController`)
- 🔐 Login via `AccountController` mit **JWT Token**
- 🛡️ Benutzerdefinierte Rollen-Absicherung mit dem `[CustomRole("Admin")]`-Attribut
- 🧪 Action Logging mit `[LogActionFilter]`
- 🧾 OpenAPI (Swagger) Dokumentation

## 🧠 Lerninhalte

| Thema                         | Beschreibung                                                                 |
|------------------------------|------------------------------------------------------------------------------|
| `CustomRoleAttribute`        | Prüft, ob ein Benutzer eine bestimmte Rolle besitzt                          |
| `LogActionFilter`            | Ein ActionFilter, der alle Requests in der Konsole loggt                     |
| `Authorize`, `AllowAnonymous`| Standard-Attribute von ASP.NET zur Zugriffskontrolle                         |
| Swagger Support              | Erlaubt das Testen geschützter Endpunkte durch manuelles Einfügen des Tokens|

## 🔧 Projektstruktur

```bash
BookStoreProject/
│
├── BookStoreApi/                 → Haupt-API-Projekt
│   ├── Controllers/              → API-Controller für Bücher und Kategorien
│   ├── Models/                   → Entitäten und DTOs
│   ├── Attributes/               → Eigene C#-Attribute
│   ├── Filters/                  → Action- und Authorization-Filter
│   ├── Middleware/               → Middleware-Komponenten
│   ├── Program.cs                → Einstiegspunkt
│   └── appsettings.json          → Konfiguration
│
└── BookStoreApiTests/            → Testprojekt
    └── Integrationstests
```

## 🔑 Authentifizierung (JWT)

### Login:
POST `/api/account/login`  
```json
{
  "email": "admin@admin.de",
  "password": "password"
}
```

### Ergebnis:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6..."
}
```

➡️ Kopiere den Token und klicke in Swagger auf "Authorize" → füge den Token mit `Bearer ` Präfix ein.

## 🐳 Docker Support

```bash
docker build -t bookstoreapi .
docker run -p 8080:8080 bookstoreapi
```

## 📚 Anforderungen

- .NET 8 SDK
- JetBrains Rider oder Visual Studio
- Optional: Docker

## 👩‍💻 Entwickelt von

**[@lady-logic](https://github.com/lady-logic)**

