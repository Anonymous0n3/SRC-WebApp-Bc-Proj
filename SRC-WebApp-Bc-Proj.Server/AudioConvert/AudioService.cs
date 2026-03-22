using FFMpegCore;

namespace SRC_WebApp_Bc_Proj.Server.AudioConvert
{
    public class AudioService : IAudioService
    {
        public void ConvertToWav(string inputPath, string outputPath)
        {
            try
            {
                FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(outputPath, true, options => options
                        .WithCustomArgument("-ar 16000") // Vzorkovací frekvence 16kHz
                        .WithCustomArgument("-ac 1")     // Mono kanál
                        .WithCustomArgument("-c:a pcm_s16le")) // Kodek PCM 16-bit
                    .ProcessSynchronously();

                Console.WriteLine($"Konverze úspěšná: {outputPath} (16kHz, Mono, PCM16)");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Chyba při konverzi přes FFmpeg: {ex.Message}", ex);
            }
        }
    }
}