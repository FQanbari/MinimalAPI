using FluentValidation;
using FluentValidation.Results;
using Library.API.Auth;
using Library.API.Data;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.IncludeFields = true;
});

builder.Configuration.AddJsonFile("appsettings.Local.json", true, true);

builder.Services.AddAuthentication(ApiKeySchemeConstants.SchemeName)
    .AddScheme<ApiKeyAuthSchemeOptions, ApiKeyAuthHandler>(ApiKeySchemeConstants.SchemeName, _ => {});
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IDbConnectionFactory>(_ =>
    new SqliteConnectionFactory(builder.Configuration.GetValue<string>("Database:ConnectionString")));

builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<IBookService, BookService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapPost("books",
    [Authorize(AuthenticationSchemes = ApiKeySchemeConstants.SchemeName)]
    async (Book book, IBookService bookService,
    IValidator<Book> validator, LinkGenerator linker,
    HttpContext context) =>
{
    var validationResult = await validator.ValidateAsync(book);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors);
    }

    var created = await bookService.CreateAsync(book);

    if (!created)
    {
        return Results.BadRequest(new List<ValidationFailure>
        {
            new ("ISBN", "A book with this ISBN-13 already exists")
        });
    }

    var path = linker.GetPathByName("GetBook", new { isbn = book.Isbn });
    var locationUri = linker.GetUriByName(context, "GetBook", new { isbn = book.Isbn });
    return Results.Created(locationUri, book);
    //return Results.CreatedAtRoute("GetBook", new { isbn = book.Isbn }, book); **
    //return Results.Created($"/books/${book.Isbn}",book);
}).WithName("CreateBook")
.Accepts<Book>("application/json")
.Produces<Book>(201)
.Produces<IEnumerable<ValidationFailure>>(400)
.WithTags("Books");

app.MapGet("books", async (IBookService bookService, string? searchTerm) =>
{
    if (searchTerm is not null && !string.IsNullOrWhiteSpace(searchTerm))
    {
        var matchedBooks = await bookService.SearchbyTitleAsync(searchTerm);

        return Results.Ok(matchedBooks);
    }

    var books = await bookService.GetAllAsync();
    return Results.Ok(books);
}).WithName("GetBooks")
.Produces<IEnumerable<Book>>(200)
.WithTags("Books");

app.MapGet("books/{isbn}", async (string isbn, IBookService bookService) =>
{
    var book = await bookService.GetByIsbnAsync(isbn);
    return book is not null ? Results.Ok(book) : Results.NotFound();
}).WithName("GetBook")
.Produces<Book>(200)
.Produces(404)
.WithTags("Books");

app.MapPut("books/{isbn}", async (string isbn, Book book, IBookService bookService,
    IValidator<Book> validator) =>
{
    book.Isbn = isbn;
    var validationResult = await validator.ValidateAsync(book);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors);
    }

    var updated = await bookService.UpdateAsync(book);
    return updated ? Results.Ok(book) : Results.NotFound();
}).WithName("UpdateBook")
.Accepts<Book>("application/json")
.Produces<Book>(200)
.Produces<IEnumerable<ValidationFailure>>(400)
.WithTags("Books");


app.MapDelete("books/{isbn}", async (string isbn, IBookService bookService,
    IValidator<Book> validator) =>
{
    var deleted = await bookService.DeleteAsync(isbn);
    return deleted ? Results.NoContent() : Results.NotFound();
}).WithName("DeleteBook")
.Produces(204)
.Produces(404)
.WithTags("Books");

// Db init here 
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();
