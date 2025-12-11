namespace Lampyris.CSharp.Common;

public static class PathUtil
{
    public static string SerializedDataSavePath
    {
        get
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.Name);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }
}
