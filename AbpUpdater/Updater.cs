using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Volo.Abp.Cli.ProjectModification;

namespace AbpUpdater
{
    internal class Updater
    {
        internal const string DefaultEFProvider = "SqlServer";

        internal async Task UpdatePackages(string directory, Dictionary<string, string> connections, string abpversion, bool replaceProjectReferenceToNuget = true, string eFProvider = DefaultEFProvider)
        {
            var solution = Directory.GetFiles(directory, "*.sln").FirstOrDefault();

            if (solution != null)
            {
                var projectPaths = ProjectFinder.GetProjectFiles(solution);

                foreach (var projectFilePath in projectPaths)
                {
                    await UpdateProjectContentAsync(projectFilePath, connections, abpversion, replaceProjectReferenceToNuget, eFProvider);
                }
            }
        }

        /// <summary>
        /// Process project content
        /// </summary>
        /// <param name="projectFilePath"></param>
        /// <param name="connections"></param>
        /// <param name="abpversion"></param>
        /// <param name="replaceProjectReferenceToNuget"></param>
        /// <param name="efProvider"></param>
        /// <returns></returns>
        private async Task UpdateProjectContentAsync(string projectFilePath, Dictionary<string, string> connections, string abpversion, bool replaceProjectReferenceToNuget = true, string efProvider = "SqlServer")
        {
            //Process csproj file
            if (replaceProjectReferenceToNuget)
            {
                var content = File.ReadAllText(projectFilePath);
                content = await ReplaceProjectsReferenceAsync(content, abpversion, efProvider);
                //overwrite project file content
                File.WriteAllText(projectFilePath, content);
                Console.WriteLine("csproj file processing is complete!.");
            }
            string projectDir = Path.GetDirectoryName(projectFilePath);

            //Process appsettings file
            if (connections?.Count > 0 || efProvider != DefaultEFProvider)
            {
                var settingFileList = Directory.GetFiles(projectDir, "appsettings.json", SearchOption.AllDirectories);
                foreach (var filename in settingFileList)
                {
                    Console.WriteLine(filename);
                    AddConnectionString(filename, connections);
                }
            }

            //Process source file
            Console.WriteLine("Source file processing started.");
            var sourceFileList = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);
            foreach (var filename in sourceFileList)
            {
                Console.WriteLine(filename);
                ProcessSource(filename, efProvider);
            }
            Console.WriteLine("Source file processing finished.");
        }

        /// <summary>
        /// process json file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="connections"></param>
        private void AddConnectionString(string filename, Dictionary<string, string> connections)
        {
            var content = File.ReadAllText(filename);

            var doc = JsonDocument.Parse(content);
            foreach (var conn in connections)
            {
                if (!doc.RootElement.TryGetProperty("ConnectionStrings", out _))
                    return;
                JsonElement p;
                if (doc.RootElement.GetProperty("ConnectionStrings").TryGetProperty(conn.Key, out p))
                {
                    var str = p.ToString().Replace(@"\", @"\\");
                    content = content.Replace(str, conn.Value);
                }
            }
            File.WriteAllText(filename, content);
        }

        private void ProcessSource(string filename, string efProvider)
        {
            var content = File.ReadAllText(filename);

            content = content.Replace(DefaultEFProvider, efProvider);

            File.WriteAllText(filename, content);
        }

        private async Task<string> ReplaceProjectsReferenceAsync(string content, string abpversion, string efProvider = DefaultEFProvider)
        {
            var doc = XDocument.Parse(content);

            if (efProvider != DefaultEFProvider)
            {
                var packageNodeList = doc.XPathSelectElements($"/Project/ItemGroup/PackageReference[contains(@Include,'{DefaultEFProvider}')]");
                while (packageNodeList.Count() > 0)
                {
                    var element = packageNodeList.First();
                    var includeAtt = element.Attribute("Include");
                    if (includeAtt is null)
                    {
                        throw new Exception($"Invalid file struct:{element}");
                    }
                    includeAtt.Value = includeAtt.Value.Replace(DefaultEFProvider, efProvider);
                }
            }

            //Process ProjectNodes
            var projectNodeList = doc.XPathSelectElements("/Project/ItemGroup/ProjectReference[contains(@Include,'Volo')]");

            var att = new XAttribute("Version", abpversion);

            while (projectNodeList.Count() > 0)
            {
                var element = projectNodeList.First();
                var includeAtt = element.Attribute("Include");
                if (includeAtt is null)
                {
                    throw new Exception($"Invalid file struct :{element}");
                }
                var regex = new Regex(@"Volo([\w,.]+).csproj");
                if (regex.IsMatch(includeAtt.Value))
                {
                    includeAtt.Value = regex.Match(includeAtt.Value).Value.RemovePostFix(StringComparison.OrdinalIgnoreCase, ".csproj");
                    if (efProvider != DefaultEFProvider)
                    {
                        includeAtt.Value = includeAtt.Value.Replace(DefaultEFProvider, efProvider);
                    }
                    element.ReplaceWith(new XElement("PackageReference", element.Attributes().ToArray(), att));
                }
            }

            content = doc.ToString();
            return await Task.FromResult(content);

        }

    }

}
