using System.Globalization;
using System.Text;

namespace HR_KD.Services
{
    public class UsernameGeneratorService
    {
        public string GenerateUsername(string fullName, int maNv)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "user" + maNv;

            // Chuẩn hóa & loại bỏ dấu
            string noDiacritics = RemoveDiacritics(fullName);

            // Gộp lại tên không dấu + mã nhân viên
            return noDiacritics.Replace(" ", "").ToLowerInvariant() + maNv;
        }

        private string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var ch in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString()
                          .Replace("đ", "d")
                          .Replace("Đ", "D");
        }
    }
}
