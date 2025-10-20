namespace App.Infrastructure.Initializer { }

public interface IDbInitializer
{
    public Task InitializeAsync();
}

