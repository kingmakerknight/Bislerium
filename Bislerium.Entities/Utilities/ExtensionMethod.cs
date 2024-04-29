namespace Bislerium.Entities.Utilities;

public static class ExtensionMethod
{
    public static string SetUniqueFileName(this string fileExtension)
    {
        var renamedFileName = DateTime.Now.Year.ToString() + 
                              DateTime.Now.Month.ToString() + 
                              DateTime.Now.Day.ToString() + 
                              DateTime.Now.Hour.ToString() + 
                              DateTime.Now.Minute.ToString() + 
                              DateTime.Now.Millisecond.ToString();
        
        return renamedFileName + fileExtension;
    }
}