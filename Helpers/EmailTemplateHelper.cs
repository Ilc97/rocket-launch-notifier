using System.IO;

namespace RocketLaunchNotifier.Email
{
    public static class EmailTemplateHelper
    {
        public static string LoadTemplate(string templateFilePath, string htmlList, string title, string description)
        {
            if (!File.Exists(templateFilePath))
            {
                throw new FileNotFoundException($"Email template not found: {templateFilePath}");
            }

            string templateContent = File.ReadAllText(templateFilePath).Replace("{{LaunchesList}}", htmlList).Replace("{{Title}}", title).Replace("{{Description}}", description);
            return templateContent;
        }
    }
}
