using System.Text.Json;
using Vosk;

namespace SRC_WebApp_Bc_Proj.Server.Speech_to_Text
{
    public class VoskSpeechToTextConverter : ISpeechToTextConvert
    {
        private readonly string _modelPath;
        private readonly Model _voskModel;

        public VoskSpeechToTextConverter(string modelPath = "models/vosk-model-en-us-0.22")
        {
            _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, modelPath);

            if (!Directory.Exists(_modelPath))
            {
                throw new DirectoryNotFoundException($"Vosk model nebyl nalezen na cestě: {_modelPath}.");
            }

            // Model se načte jen jednou při startu (musí být Singleton v Program.cs)
            Vosk.Vosk.SetLogLevel(0); // Volitelné: vypne zbytečný spam v konzoli
            _voskModel = new Model(_modelPath);
        }

        public string ConvertSpeechToText(string audioFilePath)
        {
            if (!File.Exists(audioFilePath)) return "Soubor nenalezen.";

            // Tady už používáme načtený _voskModel, nenačítáme ho znovu!
            using var rec = new VoskRecognizer(_voskModel, 16000.0f);
            rec.SetWords(true);

            using var source = File.OpenRead(audioFilePath);

            byte[] header = new byte[44];
            source.Read(header, 0, 44);

            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                rec.AcceptWaveform(buffer, bytesRead);
            }

            var finalJson = rec.FinalResult();

            using var doc = JsonDocument.Parse(finalJson);
            string text = doc.RootElement.GetProperty("text").GetString();

            return string.IsNullOrWhiteSpace(text) ? "Text nebyl rozpoznán." : text;
        }

        // Protože Model držíme v paměti (unmanaged resources), je slušnost přidat Dispose
        public void Dispose()
        {
            _voskModel?.Dispose();
        }

        private string ParseTextFromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("text").GetString() ?? "";
        }
    }
}