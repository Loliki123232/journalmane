namespace journal.Models
{
    public class User
    {
        public int Id { get; }
        public string login { get; set; }
        public string password { get; set; }
        public bool rememberMe { get; set; }

    }
}