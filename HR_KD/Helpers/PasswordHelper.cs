namespace HR_KD.Helpers
{
    public class PasswordHelper
    {
        public static string GenerateRandomKey(int lenght = 16)
        {
            var randomkey = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, lenght)
                .Select(s => s[randomkey.Next(s.Length)]).ToArray());

        }
    }
}
