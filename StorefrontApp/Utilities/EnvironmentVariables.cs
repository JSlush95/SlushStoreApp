using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StorefrontApp.Utilities
{
    public static class EnvironmentVariables
    {
        public static string MailAccount => Environment.GetEnvironmentVariable("MailAccount", EnvironmentVariableTarget.Machine);
        public static string MailPassword => Environment.GetEnvironmentVariable("MailPassword", EnvironmentVariableTarget.Machine);
        public static string SmtpHost => Environment.GetEnvironmentVariable("SmtpHost", EnvironmentVariableTarget.Machine);
        public static string PublicKey => Environment.GetEnvironmentVariable("PublicKey", EnvironmentVariableTarget.Machine);
    }
}