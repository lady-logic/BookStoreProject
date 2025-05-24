using BookStoreApi.Models;
using System.Net.Http.Json;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BookStoreApiTests
{
    public class BookControllerTests 
    {
        private readonly WebApplicationFactory<Program> _factory;

        public BookControllerTests()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Test");
                });
        }

        [Fact]
        public async Task DebugAuthHeader_ShowsHeaderValue()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);

            // Debug: Was passiert mit dem Authorization Header?
            Console.WriteLine($"Token: {token}");

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            Console.WriteLine($"Authorization Header: {client.DefaultRequestHeaders.Authorization}");

            // Act - teste einen einfachen Endpoint mit Authorization
            var response = await client.GetAsync("/api/Account/profile");

            // Assert
            Console.WriteLine($"Response Status: {response.StatusCode}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Content: {content}");
            }
        }

        [Fact]
        public async Task DebugClaims_ShowsActualClaims()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/Account/debug-claims");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Claims Debug Output: {content}");
        }

        [Fact]
        public async Task AddBook_ValidBook_ReturnsCreatedWithCorrectLocation()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            // WICHTIG: Authorization Header setzen!
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var newBook = new BookModel
            {
                Title = "Test Book",
                Author = "Test Author",
                Pages = 200
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Books", newBook);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Prüfen, ob die Location-Header korrekt ist
            Assert.NotNull(response.Headers.Location);

            // Prüfen, ob das zurückgegebene Buch korrekt ist
            var returnedBook = await response.Content.ReadFromJsonAsync<BookModel>();
            Assert.NotNull(returnedBook);
            Assert.True(returnedBook.Id > 0);
            Assert.Equal(newBook.Title, returnedBook.Title);
            Assert.Equal(newBook.Author, returnedBook.Author);
            Assert.Equal(newBook.Pages, returnedBook.Pages);

            // Prüfen, ob das Buch tatsächlich hinzugefügt wurde, indem wir es abrufen
            var getResponse = await client.GetAsync(response.Headers.Location);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var retrievedBook = await getResponse.Content.ReadFromJsonAsync<BookModel>();
            Assert.NotNull(retrievedBook);
            Assert.Equal(returnedBook.Id, retrievedBook.Id);
            Assert.Equal(returnedBook.Title, retrievedBook.Title);
            Assert.Equal(returnedBook.Author, retrievedBook.Author);
            Assert.Equal(returnedBook.Pages, retrievedBook.Pages);
        }

        [Fact]
        public async Task AddBook_InvalidBook_ReturnsBadRequest()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            // WICHTIG: Authorization Header setzen!
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var invalidBook = new BookModel
            {
                // Fehlendes Title-Feld
                Author = "Test Author",
                Pages = 200
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Books", invalidBook);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddBook_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange - Client ohne Authentifizierung erstellen
            var clientWithoutAuth = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var newBook = new BookModel
            {
                Title = "Test Book",
                Author = "Test Author",
                Pages = 200
            };

            // Act
            var response = await clientWithoutAuth.PostAsJsonAsync("/api/Books", newBook);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAllBooks_Simple_ReturnsOkWithBooks()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            // Testdaten erstellen
            var book1 = new BookModel
            {
                Title = "Book One",
                Author = "Author One",
                Pages = 100
            };

            var book2 = new BookModel
            {
                Title = "Book Two",
                Author = "Author Two",
                Pages = 200
            };

            // Bücher hinzufügen
            await client.PostAsJsonAsync("/api/Books", book1);
            await client.PostAsJsonAsync("/api/Books", book2);

            // Act
            var response = await client.GetAsync("/api/Books");

            // Assert
            // 1. Status Code prüfen
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // 2. Response Body lesen
            var jsonResponse = await response.Content.ReadAsStringAsync();

            // 3. Debug: JSON anschauen (optional)
            Console.WriteLine($"Received JSON: {jsonResponse}");

            // 4. JSON zu Objekten konvertieren
            var books = JsonSerializer.Deserialize<List<BookModel>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // 5. Validierungen
            Assert.NotNull(books);
            Assert.Equal(2, books.Count);

            // 6. Inhalte prüfen
            Assert.Contains(books, b => b.Title == "Book One" && b.Author == "Author One");
            Assert.Contains(books, b => b.Title == "Book Two" && b.Author == "Author Two");
        }

        [Fact]
        public async Task GetBookById_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            using var client = _factory.CreateClient();
            // Keine Bücher hinzufügen - Liste startet leer

            var nonExistentId = 1; // Bei leerer Liste existiert ID 1 nicht

            // Act
            var response = await client.GetAsync($"/api/Books/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetBookById_Simple_ReturnsOk()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            // Testdaten erstellen
            var book = new BookModel
            {
                Title = "Book One",
                Author = "Author One",
                Pages = 100,
            };

            // Buch erstellen
            var postResponse = await client.PostAsJsonAsync("/api/Books", book);
            Console.WriteLine($"Location: {postResponse.Headers.Location}");
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            // ID aus Location Header extrahieren
            var location = postResponse.Headers.Location?.ToString();
            Assert.NotNull(location);

            var bookId = int.Parse(location.Split('/').Last());
            Console.WriteLine($"Created book with ID: {bookId}");

            // Act
            var getResponse = await client.GetAsync($"/api/Books/{bookId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var jsonResponse = await getResponse.Content.ReadAsStringAsync();
            var retrievedBook = JsonSerializer.Deserialize<BookModel>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(retrievedBook);
            Assert.Equal(bookId, retrievedBook.Id);
            Assert.Equal("Book One", retrievedBook.Title);
            Assert.Equal("Author One", retrievedBook.Author);
            Assert.Equal(100, retrievedBook.Pages);
        }

        private async Task<string> GetAuthTokenAsync(HttpClient client)
        {
            var loginModel = new LoginModel
            {
                Email = "admin@admin.de",
                Password = "password"
            };

            var loginResponse = await client.PostAsJsonAsync("/api/Account/login", loginModel);
            loginResponse.EnsureSuccessStatusCode();

            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<JsonElement>(loginContent);
            return loginResult.GetProperty("token").GetString()!;
        }
    }
}