using FluentAssertions;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Library.API.Tests.Integration;

public class LibraryEndpointsTests: IClassFixture<LibraryApiFactory>, IAsyncLifetime
{
    private readonly LibraryApiFactory _factory;
    private readonly List<string> _createdIsbns = new();

    public LibraryEndpointsTests(LibraryApiFactory factory)
    {
         _factory = factory;
    }

    [Fact]
    public async Task Createbook_CreateBook_WhenDAtaIsCorrect()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();

        // Act
        var result = await httpclient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);
        var createdBook = await result.Content.ReadFromJsonAsync<Book>();
        

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        createdBook.Should().BeEquivalentTo(book);
        result.Headers.Location.Should().Be($"http://localhost/books/{book.Isbn}");
    }

    [Fact]
    public async Task CreateBook_fails_WhenIsbnIsInvalid()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();
        book.Isbn = "INVALID";

        // Act
        var result = await httpclient.PostAsJsonAsync("/books", book); 
        _createdIsbns.Add(book.Isbn);
        var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
        var error = errors!.Single();

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        error.PropertyName.Should().Be("Isbn");
        error.ErrorMessage.Should().Be("Value was not a valid ISBN-13");
    }

    [Fact]
    public async Task CreateBook_Fails_WhenBookExists()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();
     

        // Act
        await httpclient.PostAsJsonAsync("/books", book);
        var result = await httpclient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);
        var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
        var error = errors!.Single();

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        error.PropertyName.Should().Be("ISBN");
        error.ErrorMessage.Should().Be("A book with this ISBN-13 already exists");
    }

    [Fact]
    public async Task GetBook_ReturnBook_WhenBookExist()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();
        await httpclient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        var result = await httpclient.GetAsync($"/books/{book.Isbn}");        
        var existingbook = await result.Content.ReadFromJsonAsync<Book>();   

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        existingbook.Should().BeEquivalentTo(book);
    }

    [Fact]
    public async Task GetBook_ReturnNotFound_WhenBookNotExist()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();

        // Act
        var result = await httpclient.GetAsync($"/books/{book.Isbn}");

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllBook_ReturnAllBooks_WhenBookExist()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();
        await httpclient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);
        var books = new List<Book> { book};

        // Act
        var result = await httpclient.GetAsync($"/books");
        var returnedBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        returnedBooks.Should().BeEquivalentTo(books);
    }

    [Fact]
    public async Task GetAllBook_ReturnNoBooks_WhenNoBookExist()
    {
        // Arrange
        var httpclient = _factory.CreateClient();

        // Act
        var result = await httpclient.GetAsync($"/books");
        var returnedBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        returnedBooks.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchBook_ReturnBooks_WhenTitleMatches()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();
        await httpclient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);
        var books = new List<Book> { book };

        // Act
        var result = await httpclient.GetAsync($"/books?searchTerm=oder");
        var returnedBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        returnedBooks.Should().BeEquivalentTo(books);
    }

    [Fact]
    public async Task Updatebook_UpdateBooks_WhenDataIsCorrect()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();
        await httpclient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        book.PageCount = 100;
        var result = await httpclient.PutAsJsonAsync($"/books/{book.Isbn}",book);
        var updatedbook = await result.Content.ReadFromJsonAsync<Book>();

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        updatedbook.Should().BeEquivalentTo(book);
    }

    [Fact]
    public async Task Updatebook_doesNotUpdateBooks_WhenDataIsInCorrect()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();
        await httpclient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        book.Title = string.Empty;
        var result = await httpclient.PutAsJsonAsync($"/books/{book.Isbn}", book);
        var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
        var error = errors!.Single();

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        error.PropertyName.Should().Be("Title");
        error.ErrorMessage.Should().Be("'Title' must not be empty.");
    }

    [Fact]
    public async Task Updatebook_ReturnNotFound_WhenBookDoesNotExist()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();

        // Act
        var result = await httpclient.PutAsJsonAsync($"/books/{book.Isbn}", book);
     

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deletebook_ReturnNotBooks_WhenBookDoesNotExists()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();

        // Act
        var result = await httpclient.DeleteAsync($"/books/{book.Isbn}");

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deletebook_ReturnNoContent_WhenBookDoesExists()
    {
        // Arrange
        var httpclient = _factory.CreateClient();
        var book = GenerateBook();
        await httpclient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        var result = await httpclient.DeleteAsync($"/books/{book.Isbn}");

        // Assert
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }
    private Book GenerateBook(string title = "The Dirty Coder")
    {
        return new Book 
        {
            Isbn = "9783070630308",//, GenerateIsbn13(),
            Title = title,
            Author = "Fatemeh Qanbari",
            PageCount = 919,
            ShortDescription = "my diery",
            ReleaseDate = DateTime.Today,
        };

    }
    private string GenerateIsbn13()
    {
        string prefix = "978";

        string randomPart = Random.Shared.Next(100000000, 999999999).ToString();

        string isbnWithoutCheckDigit = prefix + randomPart;

        int checkDigit = CalculateIsbn13CheckDigit(isbnWithoutCheckDigit);

        return $"{isbnWithoutCheckDigit}{checkDigit}";
    }

    private int CalculateIsbn13CheckDigit(string isbnWithoutCheckDigit)
    {
        int sum = 0;

        for (int i = 0; i < isbnWithoutCheckDigit.Length; i++)
        {
            int digit = int.Parse(isbnWithoutCheckDigit[i].ToString());
            if (i % 2 == 0)
            {
                sum += digit;
            }
            else
            {
                sum += digit * 3;
            }
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        var httpClient = _factory.CreateClient();
        foreach (var createdIsbn in _createdIsbns)
        {
            await httpClient.DeleteAsync($"/books/{createdIsbn}");
        }
    }
}
