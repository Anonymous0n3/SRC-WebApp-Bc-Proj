using System.Text.Json;
using Vosk;

namespace SRC_WebApp_Bc_Proj.Server.Speech_to_Text
{
    public class VoskSpeechToTextConverter : ISpeechToTextConvert
    {
        private readonly string _modelPath;

        public VoskSpeechToTextConverter(string modelPath = "models/vosk-model-en-us-0.22")
        {
            _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, modelPath);

            if (!Directory.Exists(_modelPath))
            {
                throw new DirectoryNotFoundException($"Vosk model nebyl nalezen na cestě: {_modelPath}. " +
                    "Stáhni si model z https://alphacephei.com/vosk/models a vlož ho do složky s projektem.");
            }
        }

        public string ConvertSpeechToText(string audioFilePath)
        {
            if (!File.Exists(audioFilePath)) return "Soubor nenalezen.";

            using var model = new Model(_modelPath);
            using var rec = new VoskRecognizer(model, 16000.0f);

            // Zapneme výřečnost, abychom viděli slova i s jistotou (confidence)
            rec.SetWords(true);

            using var source = File.OpenRead(audioFilePath);

            // Přeskočíme hlavičku WAVu (prvních 44 bajtů), 
            // protože VoskRecognizer.AcceptWaveform očekává čistá PCM data bez hlavičky
            byte[] header = new byte[44];
            source.Read(header, 0, 44);

            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                rec.AcceptWaveform(buffer, bytesRead);
            }

            var finalJson = rec.FinalResult();

            // !!! TENTO VÝPIS JE KLÍČOVÝ - vlož ho sem do chatu, pokud to nepůjde
            Console.WriteLine($"DEBUG VOSK JSON: {finalJson}");

            using var doc = JsonDocument.Parse(finalJson);
            string text = doc.RootElement.GetProperty("text").GetString();

            return string.IsNullOrWhiteSpace(text) ? "Text nebyl rozpoznán." : text;
        }

        private string ParseTextFromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("text").GetString() ?? "";
        }
    }
}