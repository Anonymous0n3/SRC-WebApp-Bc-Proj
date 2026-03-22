namespace SRC_WebApp_Bc_Proj.Server.Speech_to_Text
{
    public interface ISpeechToTextConvert
    {
        //Conwerts Wav file to text and returns it
        string ConvertSpeechToText(string audioFilePath);
    }
}
