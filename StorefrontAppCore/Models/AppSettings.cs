namespace StorefrontAppCore.Models
{
    // POCO class
    public class AppSettings
    {
        public string MailAccount { get; set; }
        public string MailPassword { get; set; }
        public string SmtpHost { get; set; }
        public string PublicKey { get; set; }
    }
}
