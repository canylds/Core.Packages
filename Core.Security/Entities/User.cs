using Core.Persistence.Repositories;
using Core.Security.Enums;

namespace Core.Security.Entities;

public class User<TId> : Entity<TId>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public byte[] PasswordSalt { get; set; }
    public byte[] PasswordHash { get; set; }
    public AuthenticatorType AuthenticatorType { get; set; }

    public User()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        PasswordHash = [];
        PasswordSalt = [];
    }

    public User(string firstName, string lastName, string email, byte[] passwordSalt, byte[] passwordHash)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordSalt = passwordSalt;
        PasswordHash = passwordHash;
    }

    public User(TId id, string firstName, string lastName, string email, byte[] passwordSalt, byte[] passwordHash)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordSalt = passwordSalt;
        PasswordHash = passwordHash;
    }
}
