using Microsoft.EntityFrameworkCore;
using WebMinimalApiPracticeV2.Data;
using WebMinimalApiPracticeV2.Data.Entities;

var builder = WebApplication.CreateBuilder();

//регистрируем нашу бд
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi(); //регистрируем спецификацию опен апи

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.MapGet("/authors/{id:int}", GetAuthorById);
app.MapGet("/authors", GetAllAuthors);
app.MapDelete("/authors/{id:int}", DeleteAuthor);
app.MapPost("/authors", CreateAuthor);
app.MapPut("/authors/{id:int}", UpdateAuthor);

//Получить автора по айди
static async Task<IResult> GetAuthorById(int id, ApiDbContext db)
{
    var author = await db.Authors
        .Where(a => a.Id == id)
        .Select(a => new
        {
            a.Id,
            a.Name,
            a.Age,
            Books = a.Books.Select(b => new { b.Id, b.Title, b.PublishedYear })
        })
        .FirstOrDefaultAsync();

    return author is null 
        ? Results.NotFound(new { Message = $"Автора с id:{id} нет" }) 
        : Results.Ok(author);
}

//список авторов
static async Task<IResult> GetAllAuthors(ApiDbContext db)
{
    var authors = await db.Authors
        .OrderBy(a => a.Id)
        .Select(a => new
        {
            a.Id,
            a.Name,
            a.Age,
            Books = a.Books.Select(b => new {b.Id, b.Title, b.PublishedYear})
        })
        .ToListAsync();

    return Results.Ok(authors);
}

//удалить автора
static async Task<IResult> DeleteAuthor(int id, ApiDbContext db)
{
    var author = await db.Authors.FindAsync(id);

    if (author is null)
    {
        return Results.NotFound(new { Message = $"Автора с таким id:{id} нет" });
    }

    db.Authors.Remove(author);
    await db.SaveChangesAsync();

    return Results.NoContent();
}

//добавить нового автора
static async Task<IResult> CreateAuthor(AuthorCreateDto dto, ApiDbContext db)
{
    var error = ValidateAuthor(dto.Name, dto.Age, dto.Books is null);
    if (error is not null)
    {
        return Results.BadRequest(new { Message = error });
    }

    var author = new AuthorEntity
    {
        Name = dto.Name,
        Age = dto.Age,
        Books = dto.Books.Select(b => new BookEntity
        {
            Title = b.Title,
            PublishedYear = b.PublishedYear
        })
        .ToList()
    };

    db.Authors.Add(author);
    await db.SaveChangesAsync();

    var result = new
    {
        author.Id,
        author.Name,
        author.Age,
        Books = author.Books.Select(b => new { b.Id, b.Title, b.PublishedYear })
    };

    return Results.Created($"/authors/{author.Id}", result);
}

//обновить автора
static async Task<IResult> UpdateAuthor(int id, AuthorUpdateDto dto, ApiDbContext db)
{
    var error = ValidateAuthor(dto.Name, dto.Age, dto.Books is null);
    if (error is not null)
    {
        return Results.BadRequest(new { Message = error });
    }

    var author = await db.Authors
        .Include(a => a.Books)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (author is null)
    {
        return Results.NotFound(new { Message = "Такого автора нет!" });
    }

    author.Name = dto.Name;
    author.Age = dto.Age;

    //книги которые не указаны в запросе будут нетроноту (проигнорированы)
    foreach (var dtoBook in dto.Books)
    {
        if (dtoBook.Id is int dtobookId)
        {
            var exists = author.Books.FirstOrDefault(b => b.Id == dtobookId);
            if (exists is null)
            {
                return Results.BadRequest(new { Message = $"Книга {dtobookId} не найдена у автора {id}" });
            }

            exists.Title = dtoBook.Title;
            exists.PublishedYear = dtoBook.PublishedYear;
        }

        else
        {
            author.Books.Add(new BookEntity
            {
                Title = dtoBook.Title,
                PublishedYear = dtoBook.PublishedYear
            });
        }
    }

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        author.Id,
        author.Name,
        author.Age,
        Books = author.Books.Select(b => new {b.Id, b.Title, b.PublishedYear})
    });
}

app.Run();

static string? ValidateAuthor(string Name, int Age, bool BookIsNull)
{

    if (BookIsNull) return "Поле books обязательно!";
    if (string.IsNullOrWhiteSpace(Name)) return "Укажите имя!";
    if (Age <= 0) return "Укажите корреткный возраст!";
    return null;
}

public record BookCreateDto(string Title, int PublishedYear);
public record AuthorCreateDto(string Name, int Age, List<BookCreateDto> Books);
public record BookUpdateDto(int? Id, string Title, int PublishedYear);
public record AuthorUpdateDto(string Name, int Age, List<BookUpdateDto> Books);