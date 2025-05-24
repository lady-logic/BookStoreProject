using BookStoreApi.Attributes;
using BookStoreApi.Filters;
using BookStoreApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookStoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[LogActionFilter]
public class BooksController : ControllerBase
{
    private static List<BookModel> _books = new();

    [HttpGet]
    public IActionResult GetAllBooks() => Ok(_books);

    [HttpGet("{id}")]
    public IActionResult GetBook(int id)
    {
        var book = _books.FirstOrDefault(b => b.Id == id);
        return book == null ? NotFound() : Ok(book);
    }

    [HttpPost]
    [CustomRole("Admin")]
    public IActionResult AddBook([FromBody] BookModel book)
    {
        book.Id = _books.Count + 1;
        _books.Add(book);
        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
    }

    [HttpPut("{id}")]
    [CustomRole("Admin")]
    public IActionResult UpdateBook(int id, [FromBody] BookModel updated)
    {
        var book = _books.FirstOrDefault(b => b.Id == id);
        if (book == null) return NotFound();
        book.Title = updated.Title;
        book.Author = updated.Author;
        book.Pages = updated.Pages;
        return Ok(book);
    }

    [HttpDelete("{id}")]
    [CustomRole("Admin")]
    public IActionResult DeleteBook(int id)
    {
        var book = _books.FirstOrDefault(b => b.Id == id);
        if (book == null) return NotFound();
        _books.Remove(book);
        return NoContent();
    }
}