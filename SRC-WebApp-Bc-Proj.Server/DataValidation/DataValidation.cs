using SRC_WebApp_Bc_Proj.Server.TextUtils;

namespace SRC_WebApp_Bc_Proj.Server.DataValidation
{
    public class DataValidation : IDataValidation
    {
        // Podle konvencí se injektované služby dělají jako private readonly
        private readonly ITextUtils _textUtils;

        // TADY JE TA MAGIE: Konstruktor, přes který ASP.NET Core vloží službu
        public DataValidation(ITextUtils textUtils)
        {
            _textUtils = textUtils;
        }

        public bool ValidateTextRightness(string userText, string perfectText)
        {
            // Ošetření případu, kdy je jeden z textů prázdný (např. VOSK nic neslyšel)
            if (string.IsNullOrWhiteSpace(userText) || string.IsNullOrWhiteSpace(perfectText))
                return false;

            // Použijeme injektovanou službu _textUtils
            string normalizedUser = _textUtils.NormalizeText(userText);
            string normalizedPerfect = _textUtils.NormalizeText(perfectText);

            double similarity = _textUtils.CalculateSimilarity(normalizedUser, normalizedPerfect);

            // Práh můžeš upravit (0.85 = 85% podobnost)
            return similarity >= 0.85;
        }
    }
}