namespace WebMinimalApiPracticeV2.Data.Entities
{
    public class BookEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int PublishedYear { get; set; }

        public int AuthorId { get; set; }
        public AuthorEntity Author { get; set; }
    }
}
