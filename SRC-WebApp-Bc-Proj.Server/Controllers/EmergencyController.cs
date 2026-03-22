using Microsoft.AspNetCore.Mvc;
using SRC_WebApp_Bc_Proj.Server.AudioConvert;
using SRC_WebApp_Bc_Proj.Server.DataValidation;
using SRC_WebApp_Bc_Proj.Server.DTO_Models;
using SRC_WebApp_Bc_Proj.Server.FileService;
using SRC_WebApp_Bc_Proj.Server.Speech_to_Text;
using SRC_WebApp_Bc_Proj.Server.TextUtils;
using System.Text.Json;
using System.IO;
using System;

namespace SRC_WebApp_Bc_Proj.Server.Controllers
{
    [ApiController]
    [Route("api")]
    public class EmergencyController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IDataValidation _dataValidation;
        private readonly ITextUtils _textUtils;

        public EmergencyController(IWebHostEnvironment environment, IDataValidation dataValidation, ITextUtils textUtils)
        {
            _environment = environment;
            _dataValidation = dataValidation;
            _textUtils = textUtils;
        }

        [HttpPost("emergency-upload")]
        public async Task<IActionResult> UploadEmergency(
        [FromForm] EmergencyUploadDto input,
        [FromServices] IAudioService audioService,
        [FromServices] IFileService fileService,
        [FromServices] ISpeechToTextConvert sttService)
        {
            string tempWebmPath = null;
            string wavPath = null;
            string transcribedText = "";
            string expectedText = "";
            bool isValid = false;

            try
            {
                // 1. Zpracování JSON dat
                TerminalDataDto terminalInfo;
                try
                {
                    terminalInfo = JsonSerializer.Deserialize<TerminalDataDto>(input.TerminalData,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch { return BadRequest("Chyba formátu dat terminálu."); }

                if (input.Audio != null && input.Audio.Length > 0)
                {
                    // A) Uložení a konverze
                    tempWebmPath = fileService.CreateFile(input.Audio);
                    wavPath = Path.ChangeExtension(tempWebmPath, ".wav");
                    audioService.ConvertToWav(tempWebmPath, wavPath);

                    // B) Převod řeči na text (co UŽIVATEL SKUTEČNĚ ŘEKL)
                    transcribedText = sttService.ConvertSpeechToText(wavPath) ?? "";
                }

                // 2. Vygenerování ideální zprávy (co MĚL uživatel říct)
                expectedText = _textUtils.NormalizeTerminalData(terminalInfo, input.EmergencyType);

                // --- SPOČÍTÁNÍ SKÓRE PRO VŠECHNY ZPRÁVY (Důležité pro CSV statistiky do bakalářky) ---
                string normalizedTranscribed = _textUtils.NormalizeText(transcribedText);
                double similarity = _textUtils.CalculateSimilarity(normalizedTranscribed, expectedText);

                // 3. Validace (porovnání skutečnosti s ideálem přes existující logiku)
                isValid = _dataValidation.ValidateTextRightness(transcribedText, expectedText);

                // =====================================================================
                // 4. LOGIKA PRO RADIO CHECK - SIMULACE ODPOVĚDI
                // =====================================================================
                string finalActualText = transcribedText; // Defaultně vracíme, co STT poznalo

                if (!string.IsNullOrEmpty(input.EmergencyType) && input.EmergencyType.ToUpper().StartsWith("RADIO_CHECK"))
                {
                    // Z dat terminálu si vytáhneme jména pro sestavení odpovědi (nastaveno na frontendu)
                    string targetName = terminalInfo.NatureOfDistress?.ToLower() ?? "station";
                    string myShipName = terminalInfo.VesselName?.ToLower() ?? "vessel";

                    if (similarity >= 0.85)
                    {
                        finalActualText = $"{myShipName} this is {targetName} i read you loud and clear over";
                    }
                    else if (similarity >= 0.60)
                    {
                        finalActualText = $"{myShipName} this is {targetName} i read you three out of five over";
                    }
                    else if (similarity >= 0.30)
                    {
                        finalActualText = $"station calling {targetName} your signal is weak and unreadable say again over";
                    }
                    else
                    {
                        finalActualText = "[STATIC NOISE - NO RESPONSE]";
                    }
                }
                // =====================================================================

                // =====================================================================
                // 5. ZÁPIS DAT PRO BAKALÁŘKU DO CSV SOUBORU
                // =====================================================================
                try
                {
                    // Složka "thesis_data" - pohlídej si z minula to namapování v docker-compose!
                    string csvFolder = Path.Combine(_environment.ContentRootPath, "thesis_data");
                    Directory.CreateDirectory(csvFolder);
                    string csvPath = Path.Combine(csvFolder, "results.csv");

                    bool isNewFile = !System.IO.File.Exists(csvPath);
                    using (var writer = new StreamWriter(csvPath, append: true))
                    {
                        if (isNewFile)
                        {
                            writer.WriteLine("Timestamp;UserId;EmergencyType;ExpectedText;TranscribedText;SimilarityScore;IsValid");
                        }

                        // Nahradíme středníky čárkou, aby se nám nerozbil formát CSV
                        string safeExpected = expectedText.Replace(";", ",");
                        string safeTranscribed = transcribedText.Replace(";", ",");

                        writer.WriteLine($"{DateTime.UtcNow:O};{input.UserId};{input.EmergencyType};{safeExpected};{safeTranscribed};{Math.Round(similarity, 4)};{isValid}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při zápisu dat do CSV: {ex.Message}");
                }
                // =====================================================================

                // 6. Sestavení návratové zprávy pro uživatele (systémová zpráva v ANGLIČTINĚ)
                string responseMessage = isValid
                    ? "✅ TX SUCCESSFUL: MESSAGE ACKNOWLEDGED"
                    : "❌ WARNING: MESSAGE DOES NOT MEET GMDSS STANDARD. REPEAT TRANSMISSION.";

                Console.WriteLine($"--- NEW TRANSMISSION LOG ---");
                Console.WriteLine($"Type:       {input.EmergencyType}");
                Console.WriteLine($"User ID:    {input.UserId}");
                Console.WriteLine($"Expected:   {expectedText}");
                Console.WriteLine($"Transcript: {transcribedText}");
                Console.WriteLine($"Returned:   {finalActualText}");
                Console.WriteLine($"Valid:      {isValid}");
                Console.WriteLine($"Similarity: {Math.Round(similarity * 100, 1)} %");

                // 7. Odeslání všech dat zpět na frontend
                return Ok(new
                {
                    isValid = isValid,
                    expectedText = expectedText,
                    actualText = finalActualText,
                    message = responseMessage
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kritická chyba: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
            finally
            {
                // --- ÚKLID SOUBORŮ ---
                if (!string.IsNullOrEmpty(tempWebmPath) && System.IO.File.Exists(tempWebmPath))
                {
                    fileService.RemoveFile(tempWebmPath);
                }

                if (!string.IsNullOrEmpty(wavPath) && System.IO.File.Exists(wavPath))
                {
                    fileService.RemoveFile(wavPath);
                }
            }
        }
    }
}