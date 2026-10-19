using WebMinimalApiPracticeV2.Data;
using WebMinimalApiPracticeV2.Data.Entities;
using WebMinimalApiPracticeV2.Dtos;
using Microsoft.EntityFrameworkCore;
using WebMinimalApiPracticeV2.Validation;

namespace WebMinimalApiPracticeV2.Endpoints
{
    public static class AuthorEndpoints
    {
        public static void MapAuthorEndpoints(this IEndpointRouteBuilder app)
        {
            //объединяем наши связанные эндпоинты в группу под общим маршрутом /authors!!!
            var group = app.MapGroup("/authors").WithTags("Authors");
            //.WithTags("Authors") - исключительно группируем визуально для страницы swagger!!!

            group.MapGet("/{id:int}", GetAuthorById);
            group.MapGet("/", GetAllAuthors);
            group.MapDelete("/{id:int}", DeleteAuthor);
            group.MapPost("/", CreateAuthor);
            group.MapPut("/{id:int}", UpdateAuthor);
        }

        //Получить автора по айди
        private static async Task<IResult> GetAuthorById(int id, ApiDbContext db)
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
        private static async Task<IResult> GetAllAuthors(ApiDbContext db)
        {
            var authors = await db.Authors
                .OrderBy(a => a.Id)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Age,
                    Books = a.Books.Select(b => new { b.Id, b.Title, b.PublishedYear })
                })
                .ToListAsync();

            return Results.Ok(authors);
        }

        //удалить автора
        private static async Task<IResult> DeleteAuthor(int id, ApiDbContext db)
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
        private static async Task<IResult> CreateAuthor(AuthorCreateDto dto, ApiDbContext db)
        {
            var errors = AuthorValidator.Validate(dto.Name, dto.Age, 
                dto.Books?.Select(b => (b.Title, b.PublishedYear)));
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
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
        private static async Task<IResult> UpdateAuthor(int id, AuthorUpdateDto dto, ApiDbContext db)
        {
            var errors = AuthorValidator.Validate(dto.Name, dto.Age, 
                dto.Books?.Select(b => (b.Title, b.PublishedYear)));
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
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
                Books = author.Books.Select(b => new { b.Id, b.Title, b.PublishedYear })
            });
        }
    }
}
