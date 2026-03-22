namespace SRC_WebApp_Bc_Proj.Server.AudioConvert
{
    public interface IAudioService
    {
        //Converts Webm to WAV for Speech to Text later on
        void ConvertToWav(string inputPath, string outputPath);
    }
}
