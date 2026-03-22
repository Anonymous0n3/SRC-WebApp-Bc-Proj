using SRC_WebApp_Bc_Proj.Server.DTO_Models;

namespace SRC_WebApp_Bc_Proj.Server.TextUtils
{
    public interface ITextUtils
    {
        /*
         * Odstraní z textu všechny interpunkční znaky.
         */
        string RemovePunctuation(string input);

        /*
         * Odstraní z textu všechny nadbytečné mezery.
         */
        string RemoveExtraSpaces(string input);

        /*
         * Normalizuje text pro porovnávání, např. odstraní interpunkci, nahradí "may day" za "mayday" atd.
         */
        string NormalizeText(string input);

        /*
         * Počítá jak moc jsou dva normalizované texty podobné pomocí Levenshteinovy vzdálenosti.
         */
        double CalculateSimilarity(string normalizedUser, string normalizedPerfect);

        /*
         * Převádí data z TerminalDataDto do textové podoby pro porovnávání s uživatelským vstupem.
         */
        string NormalizeTerminalData(TerminalDataDto terminalData, string emergencyType);

        /*
         * Vytvoří textovou zprávu ve formátu Mayday z dat v TerminalDataDto, připravenou pro porovnávání s uživatelským vstupem.
         */
        string MaydayMessage(TerminalDataDto terminalData);

        /*
         * Vytvoří textovou zprávu ve formátu Panpan z dat v TerminalDataDto, připravenou pro porovnávání s uživatelským vstupem.
         */
        string PanpanMessage(TerminalDataDto terminalData);

        /*
         * Vytvoří textovou zprávu ve formátu Securite z dat v TerminalDataDto, připravenou pro porovnávání s uživatelským vstupem.
         */
        string SecuriteMessage(TerminalDataDto terminalData);

        /*
         * Konvertuje z číselné podoby na písemnou podobu
         */
        string convertNumToString(string mmsi);
    }
}
