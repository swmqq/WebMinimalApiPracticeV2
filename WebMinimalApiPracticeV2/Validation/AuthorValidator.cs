namespace WebMinimalApiPracticeV2.Validation
{
    public static class AuthorValidator
    {
        public static Dictionary<string, string[]> Validate(string? name, int age, IEnumerable<(string? Title, int Year)>? books)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(name))
                errors["name"] = ["Укажите имя!"];
            
            else if (name.Length > 150)
                errors["name"] = ["Укажите имя не длинее 150 символов!"];
            
            if (age is <= 0 or > 120)
                errors["age"] = ["Укажите корректный возраст! от 1 до 120"];
            
            if (books is null)
                errors["books"] = ["Полу books обязательно!"];
            
            else
            { 
                int i = 0;
                foreach (var (title, year) in books)
                {
                    if (string.IsNullOrWhiteSpace(title)) errors[$"books[{i}].title"] = ["Укажите название книг!"];

                    if (year < 1 || year > DateTime.Now.Year) errors[$"books[{i}].publishedYear"] = ["Укажите корректный год выпукска!"];
                    i++;
                }
            }

            return errors;
        }
    }
}
