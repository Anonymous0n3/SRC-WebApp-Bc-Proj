using System.Text.Json.Serialization;

namespace SRC_WebApp_Bc_Proj.Server.DTO_Models
{
    // Třída reprezentující JSON data z terminálu
    public class TerminalDataDto
    {
        [JsonPropertyName("vesselName")]
        public string VesselName { get; set; }

        [JsonPropertyName("callSign")]
        public string CallSign { get; set; }

        [JsonPropertyName("mmsi")]
        public string Mmsi { get; set; }

        [JsonPropertyName("pob")]
        public int Pob { get; set; }

        [JsonPropertyName("latitude")]
        public string Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public string Longitude { get; set; }

        [JsonPropertyName("speed")]
        public double Speed { get; set; }

        [JsonPropertyName("heading")]
        public double Heading { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        // --- NOVĚ PŘIDANÁ DATA PRO MAYDAY ---
        [JsonPropertyName("natureOfDistress")]
        public string NatureOfDistress { get; set; }

        [JsonPropertyName("assistanceRequired")]
        public string AssistanceRequired { get; set; }
    }
}