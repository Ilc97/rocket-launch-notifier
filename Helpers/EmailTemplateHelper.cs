using System.IO;

namespace RocketLaunchNotifier.Email
{
    public static class EmailTemplateHelper
    {
        public static string LoadTemplate(string templateFilePath, string htmlList)
        {
            if (!File.Exists(templateFilePath))
            {
                throw new FileNotFoundException($"Email template not found: {templateFilePath}");
            }

            string templateContent = File.ReadAllText(templateFilePath);
            return templateContent.Replace("{{LaunchesList}}", htmlList);
        }
    }
}
