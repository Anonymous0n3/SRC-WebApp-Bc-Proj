namespace SRC_WebApp_Bc_Proj.Server.FileService
{
    public interface IFileService
    {
        //Create File and return its path
        string CreateFile(IFormFile file);

        //Remove File
        void RemoveFile(string filePath);
    }
}
