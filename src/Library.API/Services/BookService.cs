using Dapper;
using Library.API.Data;
using Library.API.Models;

namespace Library.API.Services;

public class BookService : IBookService
{
    private readonly IDbConnectionFactory _connectionFactory;
    public BookService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateAsync(Book book)
    {
        //var existingBook = await GetByIsbnAsync(book.Isbn);
        //if (existingBook is not null)
        //{
        //}

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(
            @"INSERT INTO Books (Isbn, Title, Author, ShortDescription, PageCount, ReleaseDate)
            VALUES (@Isbn, @Title, @Author, @ShortDescription, @PageCount, @ReleaseDate)", book);

        return true;
    }

    public Task<bool> DeleteAsync(string isbn)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Book>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Book?> GetByIsbnAsync(string isbn)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Book>> SearchbyTitleAsync(string searchTerm)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAsync(Book book)
    {
        throw new NotImplementedException();
    }
}
