namespace WebMinimalApiPracticeV2.Dtos
{
    public record AuthorUpdateDto(string Name, int Age, List<BookUpdateDto> Books);
}
