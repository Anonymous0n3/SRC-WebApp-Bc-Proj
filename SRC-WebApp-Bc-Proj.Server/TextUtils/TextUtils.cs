using SRC_WebApp_Bc_Proj.Server.DTO_Models;
using SRC_WebApp_Bc_Proj.Server.TextUtils;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace SRC_WebApp_Bc_Proj.Server.TextUtils
{
    public class TextUtils : ITextUtils
    {
        private static readonly Dictionary<char, string> PhoneticAlphabet = new Dictionary<char, string>
        {
            {'A', "alpha"}, {'B', "bravo"}, {'C', "charlie"}, {'D', "delta"},
            {'E', "echo"}, {'F', "foxtrot"}, {'G', "golf"}, {'H', "hotel"},
            {'I', "india"}, {'J', "juliett"}, {'K', "kilo"}, {'L', "lima"},
            {'M', "mike"}, {'N', "november"}, {'O', "oscar"}, {'P', "papa"},
            {'Q', "quebec"}, {'R', "romeo"}, {'S', "sierra"}, {'T', "tango"},
            {'U', "uniform"}, {'V', "victor"}, {'W', "whiskey"}, {'X', "x-ray"},
            {'Y', "yankee"}, {'Z', "zulu"},
            {'0', "zero"}, {'1', "one"}, {'2', "two"}, {'3', "three"},
            {'4', "four"}, {'5', "five"}, {'6', "six"}, {'7', "seven"},
            {'8', "eight"}, {'9', "nine"}
        };

        public double CalculateSimilarity(string normalizedUser, string normalizedPerfect)
        {
            int distance = LevenshteinDistance(normalizedUser, normalizedPerfect);
            int maxLen = Math.Max(normalizedUser.Length, normalizedPerfect.Length);

            if (maxLen == 0)
                return 1.0;

            return 1.0 - (double)distance / maxLen;
        }

        private int LevenshteinDistance(string s, string t)
        {
            int[,] d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= t.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1,      // deletion
                                 d[i, j - 1] + 1),     // insertion
                        d[i - 1, j - 1] + cost);       // substitution
                }
            }

            return d[s.Length, t.Length];
        }

        public string NormalizeText(string input)
        {
            input = RemovePunctuation(input);
            input = RemoveExtraSpaces(input);

            input = input.Replace("may day", "mayday");
            input = input.Replace("been been", "pan pan");
            input = input.Replace("ben ben", "pan pan");
            input = input.Replace("ben pon", "pan pan");
            input = input.Replace("pon ben", "pan pan");
            input = input.Replace("pon pon", "pan pan");
            input = input.Replace("bon bon", "pan pan");
            input = input.Replace("security", "securite");
            input = input.Replace("good day sir", "securite");

            return input;
        }

        public string RemoveExtraSpaces(string input)
        {
            return Regex.Replace(input, @"\s+", " ").Trim();
        }

        public string RemovePunctuation(string input)
        {
            return Regex.Replace(input, @"[^\w\s]", "");
        }

        public string NormalizeTerminalData(TerminalDataDto terminalData, string emergencyType)
        {
            if (terminalData == null || string.IsNullOrWhiteSpace(emergencyType))
                return string.Empty;

            // Normalizace vstupu přepínače pro bezpečné porovnání
            string type = emergencyType.Trim().ToUpper();

            switch (type)
            {
                case "MAYDAY":
                    return MaydayMessage(terminalData);

                case "PAN_PAN":
                case "PANPAN":
                    return PanpanMessage(terminalData);

                case "SECURITE":
                    return SecuriteMessage(terminalData);

                // NOVÉ: Přidáno zpracování pro Radio Check
                case "RADIO_CHECK_SHIP":
                    return RadioCheckShipMessage(terminalData);

                case "RADIO_CHECK_STATION":
                    return RadioCheckStationMessage(terminalData);

                default:
                    // Záložní vrácení pouhých dat (pokud by šlo o standardní TX bez typu nouze)
                    string rawData = $"{terminalData.VesselName} {terminalData.CallSign} {terminalData.Mmsi} " +
                                     $"{terminalData.Latitude} {terminalData.Longitude} " +
                                     $"{terminalData.Speed} {terminalData.Heading} {terminalData.Pob}";

                    return NormalizeText(RemoveExtraSpaces(rawData)).ToLower();
            }
        }

        public string MaydayMessage(TerminalDataDto terminalData)
        {
            if (terminalData == null) return string.Empty;

            string msg =    //Distress call part
                            $"mayday mayday mayday " +
                            $"this is {terminalData.VesselName} {terminalData.VesselName} {terminalData.VesselName} " +
                            $"call sign {ConvertCallSign(terminalData.CallSign)}  " +
                            $"m m s i {convertNumToString(terminalData.Mmsi)} " +

                            //Distress message part
                            $"mayday " +
                            $"{terminalData.VesselName} call sign {ConvertCallSign(terminalData.CallSign)} m m s i {convertNumToString(terminalData.Mmsi)} " +
                            $"my position is {convertNumToString(terminalData.Latitude)} north {convertNumToString(terminalData.Longitude)} east " +
                            $"{terminalData.NatureOfDistress} {terminalData.AssistanceRequired} " +
                            $"{convertNumToString("" + terminalData.Pob)} persons on board over";

            return NormalizeText(RemoveExtraSpaces(msg)).ToLower();
        }

        public string PanpanMessage(TerminalDataDto terminalData)
        {
            if (terminalData == null) return string.Empty;

            string msg =    //Urgency call part
                            $"pan pan pan pan pan pan " +
                            $"all stations all stations all stations " +
                            $"this is " +
                            $"{terminalData.VesselName} {terminalData.VesselName} {terminalData.VesselName} call sign {ConvertCallSign(terminalData.CallSign)} mmsi {convertNumToString(terminalData.Mmsi)} " +

                            //Urgency message part
                            $"my position is {convertNumToString(terminalData.Latitude)} north {convertNumToString(terminalData.Longitude)} east " +
                            $"{terminalData.NatureOfDistress} " +
                            $"{terminalData.AssistanceRequired} " +
                            $"{convertNumToString("" + terminalData.Pob)} persons on board over";

            return NormalizeText(RemoveExtraSpaces(msg)).ToLower();
        }

        public string SecuriteMessage(TerminalDataDto terminalData)
        {
            if (terminalData == null) return string.Empty;

            string msg =    // Safety call part
                            $"securite securite securite " +
                            $"all ships all ships all ships " +
                            $"this is split radio this is split radio this is split radio over " +

                            // Safety message part
                            $"securite securite securite " +
                            $"all ships all ships all ships " +
                            $"this is split radio this is split radio this is split radio " +
                            $"{terminalData.NatureOfDistress} out";

            return NormalizeText(RemoveExtraSpaces(msg)).ToLower();
        }

        // NOVÉ: Generování očekávané fráze pro Ship to Ship Radio Check
        public string RadioCheckShipMessage(TerminalDataDto terminalData)
        {
            if (terminalData == null) return string.Empty;

            // Příklad rutinního volání: "Unknown vessel, unknown vessel, unknown vessel, this is Ocean Explorer, Ocean Explorer, Ocean Explorer, call sign WXYZ, how do you read me, over."
            string msg = $"{terminalData.NatureOfDistress} {terminalData.NatureOfDistress} {terminalData.NatureOfDistress} " +
                         $"this is {terminalData.VesselName} {terminalData.VesselName} {terminalData.VesselName} " +
                         $"radio check please over";

            return NormalizeText(RemoveExtraSpaces(msg)).ToLower();
        }

        // NOVÉ: Generování očekávané fráze pro Ship to Station Radio Check
        public string RadioCheckStationMessage(TerminalDataDto terminalData)
        {
            if (terminalData == null) return string.Empty;

            // Příklad rutinního volání: "Split radio, split radio, split radio, this is Ocean Explorer, Ocean Explorer, Ocean Explorer, call sign WXYZ, how do you read me, over."
            string msg = $"{terminalData.NatureOfDistress} {terminalData.NatureOfDistress} {terminalData.NatureOfDistress} " +
                         $"this is {terminalData.VesselName} {terminalData.VesselName} {terminalData.VesselName} " +
                         $"radio check please over";

            return NormalizeText(RemoveExtraSpaces(msg)).ToLower();
        }

        public string ConvertCallSign(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var phoneticWords = new List<string>();

            // Projdeme každý znak (převedený na velká písmena, aby seděl do slovníku)
            foreach (char c in input.ToUpper())
            {
                if (PhoneticAlphabet.TryGetValue(c, out string word))
                {
                    phoneticWords.Add(word);
                }
                else if (c == ' ')
                {
                    // Zachováme mezery, pokud by vstup obsahoval více slov
                    phoneticWords.Add("");
                }
            }

            // Spojíme slova mezerou, odstraníme případné vícenásobné mezery
            return RemoveExtraSpaces(string.Join(" ", phoneticWords));
        }

        public string convertNumToString(string mmsi)
        {
            if (string.IsNullOrWhiteSpace(mmsi))
                return string.Empty;

            var result = new List<string>();

            foreach (char c in mmsi)
            {
                switch (c)
                {
                    case '0': result.Add("zero"); break;
                    case '1': result.Add("one"); break;
                    case '2': result.Add("two"); break;
                    case '3': result.Add("three"); break;
                    case '4': result.Add("four"); break;
                    case '5': result.Add("five"); break;
                    case '6': result.Add("six"); break;
                    case '7': result.Add("seven"); break;
                    case '8': result.Add("eight"); break;
                    case '9': result.Add("nine"); break;
                    case '.': result.Add("point"); break;
                    case ' ':
                        continue;
                    default:
                        result.Add(c.ToString());
                        break;
                }
            }

            // Spojíme všechna slova jednou mezerou
            return string.Join(" ", result);
        }
    }
}