namespace WebMinimalApiPracticeV2.Dtos
{
    public record AuthorCreateDto(string Name, int Age, List<BookCreateDto> Books);
}
