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

        [Fact]
        public async Task AddBook_MissingTitle_ReturnsBadRequest()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var invalidBook = new
            {
                Author = "Test Author",
                Pages = 100
                // Title fehlt!
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Books", invalidBook);

            // Assert  
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddBook_EmptyTitleAndAuthor_ShowDetailedErrors()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var invalidBook = new BookModel
            {
                Title = "",      // Leer (Required Validation)
                Author = "",     // Leer (Required Validation) 
                Pages = 0        // Unter Range (1-1000)
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Books", invalidBook);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            PrintValidationResponse(responseContent);

            // Erwarte alle drei Validierungsfehler
            Assert.Contains("Title", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Author", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Pages", responseContent, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateBook_Simple_ReturnsOk()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            // Buch erstellen
            var book = new BookModel { Title = "Original", Author = "Original", Pages = 100 };
            var createResponse = await client.PostAsJsonAsync("/api/Books", book);
            var bookId = int.Parse(createResponse.Headers.Location.ToString().Split('/').Last());

            // Buch aktualisieren
            var updated = new BookModel { Title = "Updated Title", Author = "Updated Author", Pages = 200 };
            var updateResponse = await client.PutAsJsonAsync($"/api/Books/{bookId}", updated);

            // Assert
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            client.DefaultRequestHeaders.Authorization = null; // GET braucht keine Auth

            var getResponse = await client.GetAsync($"/api/Books/{bookId}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var getResponseContent = await getResponse.Content.ReadAsStringAsync();

            var persistedBook = JsonSerializer.Deserialize<BookModel>(getResponseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Final Assertions - Prüfe dass alle Änderungen persistent sind
            Assert.NotNull(persistedBook);
            Assert.Equal(bookId, persistedBook.Id);
            Assert.Equal("Updated Title", persistedBook.Title);
            Assert.Equal("Updated Author", persistedBook.Author);
            Assert.Equal(200, persistedBook.Pages);
        }

        [Fact]
        public async Task DeleteBook_Simple_ReturnsNoContent()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            // Buch erstellen
            var book = new BookModel { Title = "To Delete", Author = "Test", Pages = 100 };
            var createResponse = await client.PostAsJsonAsync("/api/Books", book);
            var bookId = int.Parse(createResponse.Headers.Location.ToString().Split('/').Last());

            // Buch löschen
            var deleteResponse = await client.DeleteAsync($"/api/Books/{bookId}");

            // Assert DELETE
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Subsequent GET
            client.DefaultRequestHeaders.Authorization = null;
            var getResponse = await client.GetAsync($"/api/Books/{bookId}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteBook_WithoutAuth_ReturnsUnauthorized()
        {
            // Arrange
            using var client = _factory.CreateClient();
            // Absichtlich KEINE Authentication

            var someId = 1;

            Console.WriteLine("Testing DELETE without authentication...");

            // Act
            var response = await client.DeleteAsync($"/api/Books/{someId}");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task BookWorkflow_SimpleE2E_WorksCorrectly()
        {
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);

            // CREATE
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            var book = new BookModel { Title = "E2E Test", Author = "E2E Author", Pages = 100 };
            var createResponse = await client.PostAsJsonAsync("/api/Books", book);
            var bookId = int.Parse(createResponse.Headers.Location.ToString().Split('/').Last());
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            // READ
            client.DefaultRequestHeaders.Authorization = null;
            var getResponse = await client.GetAsync($"/api/Books/{bookId}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // UPDATE
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            var updated = new BookModel { Title = "Updated", Author = "Updated", Pages = 200 };
            var updateResponse = await client.PutAsJsonAsync($"/api/Books/{bookId}", updated);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            // VERIFY
            client.DefaultRequestHeaders.Authorization = null;
            var verifyResponse = await client.GetAsync($"/api/Books/{bookId}");
            Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

            // DELETE
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            var deleteResponse = await client.DeleteAsync($"/api/Books/{bookId}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // CONFIRM
            client.DefaultRequestHeaders.Authorization = null;
            var confirmResponse = await client.GetAsync($"/api/Books/{bookId}");
            Assert.Equal(HttpStatusCode.NotFound, confirmResponse.StatusCode);
        }

        [Fact]
        public async Task ConcurrentOperations_Simple_DetectsRaceConditions()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            const int concurrentRequests = 5; // Klein anfangen

            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 1; i <= concurrentRequests; i++)
            {
                var book = new BookModel
                {
                    Title = $"Concurrent Book {i}",
                    Author = $"Author {i}",
                    Pages = 100 + i
                };

                tasks.Add(client.PostAsJsonAsync("/api/Books", book));
            }

            var responses = await Task.WhenAll(tasks);

            var createdIds = new List<int>();
            var successCount = 0;

            foreach (var response in responses)
            {
                Console.WriteLine($"Response: {response.StatusCode}");

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    successCount++;
                    var location = response.Headers.Location?.ToString();
                    if (location != null)
                    {
                        var id = int.Parse(location.Split('/').Last());
                        createdIds.Add(id);
                        Console.WriteLine($"  Created ID: {id}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"  Error: {errorContent}");
                }
            }

            Console.WriteLine($"\nResults: {successCount}/{concurrentRequests} successful");
            Console.WriteLine($"Created IDs: [{string.Join(", ", createdIds)}]");

            // Check 1: All should succeed
            Assert.Equal(concurrentRequests, successCount);

            // Check 2: All IDs should be unique (detects race conditions)
            var uniqueIds = createdIds.Distinct().ToList();
            if (uniqueIds.Count != createdIds.Count)
            {
                var duplicates = createdIds.GroupBy(x => x).Where(g => g.Count() > 1);
                foreach (var dup in duplicates)
                {
                    Console.WriteLine($"RACE CONDITION DETECTED: ID {dup.Key} appears {dup.Count()} times");
                }
            }

            Assert.Equal(createdIds.Count, uniqueIds.Count);
            Console.WriteLine("All IDs are unique - no race conditions detected");
        }

        [Fact]
        public async Task ConcurrentOperations_MultipleUPDATE_ShouldNotLoseData()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            // Erstelle ein Buch zum Testen
            var originalBook = new BookModel
            {
                Title = "Original Title",
                Author = "Original Author",
                Pages = 100
            };

            var createResponse = await client.PostAsJsonAsync("/api/Books", originalBook);
            var bookId = int.Parse(createResponse.Headers.Location.ToString().Split('/').Last());

            const int concurrentUpdates = 10;

            var updateTasks = new List<Task<HttpResponseMessage>>();

            for (int i = 1; i <= concurrentUpdates; i++)
            {
                var updateData = new BookModel
                {
                    Id = bookId,
                    Title = $"Updated Title {i:D2}",
                    Author = $"Updated Author {i:D2}",
                    Pages = 100 + i
                };

                var task = client.PutAsJsonAsync($"/api/Books/{bookId}", updateData);
                updateTasks.Add(task);
            }

            Console.WriteLine($"Starting {concurrentUpdates} concurrent UPDATE operations...");

            var responses = await Task.WhenAll(updateTasks);

            var successfulUpdates = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
            var failedUpdates = responses.Length - successfulUpdates;

            Console.WriteLine($"Successful updates: {successfulUpdates}/{concurrentUpdates}");
            Console.WriteLine($"Failed updates: {failedUpdates}");

            // Alle Updates sollten erfolgreich sein (keine Race Conditions)
            Assert.Equal(concurrentUpdates, successfulUpdates);

            client.DefaultRequestHeaders.Authorization = null;
            var finalGetResponse = await client.GetAsync($"/api/Books/{bookId}");
            Assert.Equal(HttpStatusCode.OK, finalGetResponse.StatusCode);

            var finalBook = JsonSerializer.Deserialize<BookModel>(
                await finalGetResponse.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Console.WriteLine($"Final state: {finalBook.Title} by {finalBook.Author}, {finalBook.Pages} pages");

            // Das Buch sollte EINEN der Update-Werte haben (nicht korrupt sein)
            var validTitles = Enumerable.Range(1, concurrentUpdates)
                .Select(i => $"Updated Title {i:D2}")
                .ToList();

            Assert.Contains(finalBook.Title, validTitles);
        }

        [Fact]
        public async Task ConcurrentOperations_MultipleDELETE_ShouldNotFail()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            // Erstelle mehrere Bücher
            var bookIds = new List<int>();
            for (int i = 1; i <= 5; i++)
            {
                var book = new BookModel
                {
                    Title = $"Book to Delete {i}",
                    Author = $"Author {i}",
                    Pages = 100 + i
                };

                var response = await client.PostAsJsonAsync("/api/Books", book);
                var id = int.Parse(response.Headers.Location.ToString().Split('/').Last());
                bookIds.Add(id);
            }

            Console.WriteLine($"Created {bookIds.Count} books for concurrent DELETE testing: [{string.Join(", ", bookIds)}]");

            var targetBookId = bookIds.First();
            const int concurrentDeletes = 5;

            var deleteTasks = Enumerable.Range(1, concurrentDeletes)
                .Select(i => client.DeleteAsync($"/api/Books/{targetBookId}"))
                .ToList();

            Console.WriteLine($"Starting {concurrentDeletes} concurrent DELETE operations on book {targetBookId}...");

            var deleteResponses = await Task.WhenAll(deleteTasks);

            var successfulDeletes = deleteResponses.Count(r => r.StatusCode == HttpStatusCode.NoContent);
            var notFoundDeletes = deleteResponses.Count(r => r.StatusCode == HttpStatusCode.NotFound);
            var errorDeletes = deleteResponses.Count(r =>
                r.StatusCode != HttpStatusCode.NoContent &&
                r.StatusCode != HttpStatusCode.NotFound);

            Console.WriteLine($"Successful deletes (204): {successfulDeletes}");
            Console.WriteLine($"Not found deletes (404): {notFoundDeletes}");
            Console.WriteLine($"Error deletes: {errorDeletes}");

            // Erwartung: 1 erfolgreich, Rest 404 (oder alle 404 wenn sehr schnell)
            Assert.True(successfulDeletes <= 1, "Only one DELETE should succeed");
            Assert.True(notFoundDeletes >= concurrentDeletes - 1, "Others should get 404");
            Assert.Equal(0, errorDeletes);

            client.DefaultRequestHeaders.Authorization = null;
            var verifyResponse = await client.GetAsync($"/api/Books/{targetBookId}");
            Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
        }

        [Fact]
        public async Task ConcurrentOperations_MixedCRUD_DataIntegrity()
        {
            // Arrange
            using var client = _factory.CreateClient();
            var token = await GetAuthTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);

            // Setup: Erstelle initial books
            var initialBooks = new List<int>();
            for (int i = 1; i <= 3; i++)
            {
                var book = new BookModel
                {
                    Title = $"Initial Book {i}",
                    Author = $"Initial Author {i}",
                    Pages = 100 + i
                };

                var response = await client.PostAsJsonAsync("/api/Books", book);
                var id = int.Parse(response.Headers.Location.ToString().Split('/').Last());
                initialBooks.Add(id);
            }

            Console.WriteLine($"Initial books: [{string.Join(", ", initialBooks)}]");

            var allTasks = new List<Task<(string operation, HttpResponseMessage response)>>();

            // CREATE tasks
            for (int i = 1; i <= 3; i++)
            {
                var newBook = new BookModel
                {
                    Title = $"Concurrent New {i}",
                    Author = $"Concurrent Author {i}",
                    Pages = 200 + i
                };

                var task = client.PostAsJsonAsync("/api/Books", newBook)
                    .ContinueWith(t => ($"CREATE Book {i}", t.Result));
                allTasks.Add(task);
            }

            // UPDATE tasks
            foreach (var bookId in initialBooks.Take(2))
            {
                var updateBook = new BookModel
                {
                    Id = bookId,
                    Title = $"Updated Concurrent {bookId}",
                    Author = $"Updated Author {bookId}",
                    Pages = 300 + bookId
                };

                var task = client.PutAsJsonAsync($"/api/Books/{bookId}", updateBook)
                    .ContinueWith(t => ($"UPDATE Book {bookId}", t.Result));
                allTasks.Add(task);
            }

            // DELETE task
            var deleteBookId = initialBooks.Last();
            var deleteTask = client.DeleteAsync($"/api/Books/{deleteBookId}")
                .ContinueWith(t => ($"DELETE Book {deleteBookId}", t.Result));
            allTasks.Add(deleteTask);

            Console.WriteLine($"Prepared {allTasks.Count} mixed concurrent operations");

            var results = await Task.WhenAll(allTasks);

            var allSuccessful = true;

            foreach (var (operation, response) in results)
            {
                var expected = operation.StartsWith("CREATE") ? HttpStatusCode.Created :
                              operation.StartsWith("UPDATE") ? HttpStatusCode.OK :
                              operation.StartsWith("DELETE") ? HttpStatusCode.NoContent :
                              HttpStatusCode.OK;

                var success = response.StatusCode == expected;
                allSuccessful &= success;

                var status = success ? "erfolgreich" : "fehlerhaft";
                Console.WriteLine($"{status} {operation}: {response.StatusCode}");

                if (!success)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"    Error: {errorContent}");
                }
            }

            client.DefaultRequestHeaders.Authorization = null;
            var finalGetResponse = await client.GetAsync("/api/Books");
            var finalBooks = JsonSerializer.Deserialize<List<BookModel>>(
                await finalGetResponse.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Check data integrity
            var allIds = finalBooks.Select(b => b.Id).ToList();
            var uniqueIds = allIds.Distinct().ToList();

            Assert.Equal(allIds.Count, uniqueIds.Count); // No duplicate IDs
            Assert.True(finalBooks.All(b => !string.IsNullOrEmpty(b.Title))); // Valid data
            Assert.True(finalBooks.All(b => b.Pages > 0)); // Valid data

            Console.WriteLine($"Final state: {finalBooks.Count} books, all with unique IDs and valid data");

            // Overall assertion
            Assert.True(allSuccessful, "All concurrent operations should succeed without race conditions");
        }

        private static void PrintValidationResponse(string responseContent)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(responseContent);

                // Status und Titel
                if (jsonDoc.RootElement.TryGetProperty("status", out var status))
                    Console.WriteLine($"Status: {status.GetInt32()}");

                if (jsonDoc.RootElement.TryGetProperty("title", out var title))
                    Console.WriteLine($"Title: {title.GetString()}");

                if (jsonDoc.RootElement.TryGetProperty("type", out var type))
                    Console.WriteLine($"Type: {type.GetString()}");

                // Errors Detail
                if (jsonDoc.RootElement.TryGetProperty("errors", out var errors))
                {
                    Console.WriteLine("Validation Errors:");
                    foreach (var errorField in errors.EnumerateObject())
                    {
                        Console.WriteLine($"  {errorField.Name}:");
                        foreach (var errorMessage in errorField.Value.EnumerateArray())
                        {
                            Console.WriteLine($"    - {errorMessage.GetString()}");
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Non-JSON Response: {responseContent}");
            }
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