namespace WebMinimalApiPracticeV2.Data.Entities
{
    public class AuthorEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }

        public List<BookEntity>? Books { get; set; }
    }
}
