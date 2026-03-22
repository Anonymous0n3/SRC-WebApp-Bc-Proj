namespace SRC_WebApp_Bc_Proj.Server.FileService
{
    public class FileService : IFileService
    {
        private readonly string _uploadPath;

        public FileService(IWebHostEnvironment environment)
        {
            // Definujeme cestu k upload složce
            _uploadPath = Path.Combine(environment.ContentRootPath, "uploads");

            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        public string CreateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Soubor je prázdný.");

            // Vytvoříme unikátní název, abychom předešli kolizím
            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            // Pokud FormFile nemá příponu (časté u Blobů z JS), můžete ji vynutit:
            if (string.IsNullOrEmpty(Path.GetExtension(fileName))) fileName += ".webm";

            string fullPath = Path.Combine(_uploadPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                // Poznámka: V synchronní metodě rozhraní musíme použít synchronní CopyTo
                file.CopyTo(stream);
            }

            return fullPath;
        }

        public void RemoveFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}