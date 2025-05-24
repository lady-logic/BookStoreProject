using System.ComponentModel.DataAnnotations;

namespace BookStoreApi.Models;

public class RegisterModel
{
    [Required]
    public string Username { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    [MinLength(6)]
    public string Password { get; set; }

    [Compare("Password")]
    public string ConfirmPassword { get; set; }
}
