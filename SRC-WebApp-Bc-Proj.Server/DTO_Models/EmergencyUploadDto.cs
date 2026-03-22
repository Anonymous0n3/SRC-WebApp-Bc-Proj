namespace SRC_WebApp_Bc_Proj.Server.DTO_Models
{
    // Třída pro přijetí dat z formuláře (FormData)
    public class EmergencyUploadDto
    {
        public IFormFile Audio { get; set; }        // Soubor
        public string? EmergencyType { get; set; }  // "MAYDAY", "PAN_PAN" nebo null
        public string TerminalData { get; set; }    // JSON string z Reactu
        public string UserId { get; set; }          // ID uživatele

    }
}
