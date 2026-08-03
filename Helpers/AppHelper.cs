using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

namespace QUIZAPP.Helpers
{
    public static class AppHelper
    {
        private const int MaxSlugLength = 50;
        public static string GenerateSlug(string title)
        {
          
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }
            
            // Convert to lowercase
            string slug = title.ToLowerInvariant();

            // Remove accents
            slug = RemoveDiacritics(slug);

            // Replace invalid characters with hyphens
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);

            // Replace multiple spaces or hyphens with a single hyphen
            slug = Regex.Replace(slug, @"[\s-]+", "-");

            // Trim hyphens from the start and end
            slug = slug.Trim('-');

            // Truncate the slug if it's too long
            if (slug.Length > MaxSlugLength)
            {
                slug = slug.Substring(0, MaxSlugLength);

                // Ensure the slug is unique by appending a hash
                //slug = AppendUniqueHash(slug);
            }

            return slug;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
        public static string DisplayToast(string msg, string msgType, string position)
        {
            string tScript = "window.onload=function(){NioApp.Toast(" + "'" + msg + "'," + "'" + msgType + "'," + " {position:  " + "'" + position + "'});};";
            //ScriptManager.RegisterStartupScript(ctrl, ctrl.GetType(), "Myscript", tScript, true);
            //RegisterStartupScript(page, page.GetType(), "MyScript", tScript, true);
            return tScript;
        }
        public static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            StringBuilder initialsBuilder = new StringBuilder();
            string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                if (!string.IsNullOrEmpty(word))
                {
                    initialsBuilder.Append(char.ToUpper(word[0]));
                }
            }
            return initialsBuilder.ToString();
        }
        public static string GeneratePassword(int length)
        {
            // Define character sets
            char[] LowercaseChars = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            char[] Digits = "0123456789".ToCharArray();
            char[] SpecialChars = "!@#$%&*+".ToCharArray();

            // Ensure the password length is valid
            if (length < 8)
                throw new ArgumentException("Password length should be at least 8 characters.");

            // Initialize random generator and storage
            var random = new Random();
            var passwordChars = new char[length];

            // Guarantee one special character in the password
            passwordChars[0] = SpecialChars[random.Next(SpecialChars.Length)];

            // Fill the rest of the password with random lowercase letters and digits
            for (int i = 1; i < length; i++)
            {
                if (random.Next(2) == 0) // Randomly pick between lowercase or digit
                {
                    passwordChars[i] = LowercaseChars[random.Next(LowercaseChars.Length)];
                }
                else
                {
                    passwordChars[i] = Digits[random.Next(Digits.Length)];
                }
            }

            // Shuffle the password to ensure the special character is not always the first character
            passwordChars = passwordChars.OrderBy(c => random.Next()).ToArray();

            return new string(passwordChars);
        }
    }
}
