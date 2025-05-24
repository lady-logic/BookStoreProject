using System.ComponentModel.DataAnnotations;

namespace BookStoreApi.Models;

public class BookModel
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }

    [Required]
    public string Author { get; set; }

    [Range(1, 1000)]
    public int Pages { get; set; }
}
