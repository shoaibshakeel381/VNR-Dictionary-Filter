using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace VNRSharedDictFilter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string terms = "<?xml version=\"1.0\" encoding=\"utf-8\"?><!-- terms.xml 2018-07-07 13:11-->" +
                               "<grimoire version=\"1.0\" timestamp=\"0\"><terms>";

                if (args.Length == 0)
                {
                    Console.WriteLine("Getting global terms");
                    terms += GetGlobalTerms();
                }
                else if (args[0].Equals("gamespecific"))
                {
                    Console.WriteLine("Getting game specific terms");
                    var gameIds = args[1].Split(',').Select(int.Parse).ToList();
                    terms += GetGameSpecificTerms(gameIds);
                }
                else if (args[0].Equals("print"))
                {
                    Console.WriteLine("Getting terms summary");
                    terms += GetSummary();
                }
                else if (args.Length == 3 && args[0].Equals("remove"))
                {
                    Console.WriteLine("Removing terms associated with given file id");
                    var gameIds = args[2].Split(',').Select(int.Parse).ToList();
                    terms += GetTermsUnrelatedToGame(GetFileLocation(args[1]), gameIds);
                }
                else if (args.Length == 3 && args[0].Equals("merge"))
                {
                    Console.WriteLine("Merging dictionaries");
                    MergeDictionaryFiles(GetFileLocation(args[1]), GetFileLocation(args[2]));
                    return;
                }
                else if (args.Length == 3 && args[0].Equals("element"))
                {
                    Console.WriteLine("Filtering terms based on given criteria");
                    var element = args[1];
                    var value = args[2];
                    terms += GetMatchingTerms(element, value);
                }
                else
                {
                    Console.WriteLine("Invalid input.\n");
                    PrintUsage();
                    return;
                }

                terms += "</terms></grimoire>";
                WriteResults(terms);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:\n" +
                            "   DictFilter.exe\n" +
                            "   DictFilter.exe gamespecific   <file_id>\n" +
                            "   DictFilter.exe element        <element_name> <value>\n" +
                            "   DictFilter.exe merge          <fileA> <fileB>\n" +
                            "   DictFilter.exe remove         <file> <file_id>\n" +
                            "   DictFilter.exe print\n\n" +
                            "Details:\n" +
                            "   gamespecific    Returns game specific terms. Filteration will be\n" +
                            "                   done by File Ids. File Ids can be found from Edit\n" +
                            "                   Dialog under Game info page. Multiple ids\n" +
                            "                   should be separated by comma.\n" +
                            "   element         Returns terms where <element_name> has value matching\n" +
                            "                   the given <value>. <value> can be a regular expression.\n" +
                            "   merge           Merges two dictionary files and produces a new file.\n" +
                            "                   Both files should be present in current directory.\n" + 
                            "                   Each file must have a root element as parent to make xml valid.\n" +
                            "   remove          Remove game specific terms from given dict file.\n" +
                            "                   File Ids can be found from Edit Dialog under Game\n" +
                            "                   info page. Multiple ids should be separated by comma.\n" +
                            "   print           Prints Id, Special, Pattern, Text and Game Id to xml file.\n\n" +
                            "   If no parameter is provided, then global terms will be returned.\n\n" +
                            "NOTE: Disabled terms will be always be ignored.");
        }

        private static string GetGameSpecificTerms(ICollection<int> targetGameIds)
        {
            int allTermCount = 0, filteredTermCount = 0;
            var sb = new StringBuilder();
            var settings = new XmlReaderSettings {ValidationType = ValidationType.None};
            using (var reader = XmlReader.Create(GetDictionaryFile(), settings))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "term") continue;

                        if (XNode.ReadFrom(reader) is XElement el)
                        {
                            allTermCount++;
                            if (bool.Parse(el.Attribute("disabled")?.Value ?? "false")) continue;

                            var gameId = int.Parse(el.Element("gameId")?.Value ?? "-1");
                            var isSpecial = bool.Parse(el.Element("special")?.Value ?? "false");
                            if (isSpecial && targetGameIds.Contains(gameId))
                            {
                                sb.Append(el);
                                filteredTermCount++;
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Number of terms found: {allTermCount}\nFiltered term count: {filteredTermCount}");
            return sb.ToString();
        }

        private static string GetTermsUnrelatedToGame(string filePath, ICollection<int> targetGameIds)
        {
            int allTermCount = 0, filteredTermCount = 0;
            var sb = new StringBuilder();
            var settings = new XmlReaderSettings { ValidationType = ValidationType.None };
            using (var reader = XmlReader.Create(filePath, settings))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "term") continue;

                        if (XNode.ReadFrom(reader) is XElement el)
                        {
                            allTermCount++;
                            if (bool.Parse(el.Attribute("disabled")?.Value ?? "false")) continue;

                            var gameId = int.Parse(el.Element("gameId")?.Value ?? "-1");
                            if (!targetGameIds.Contains(gameId))
                            {
                                sb.Append(el);
                                filteredTermCount++;
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Number of terms found: {allTermCount}\nFiltered term count: {filteredTermCount}");
            return sb.ToString();
        }

        private static string GetGlobalTerms()
        {
            int allTermCount = 0, filteredTermCount = 0;
            var sb = new StringBuilder();
            var settings = new XmlReaderSettings { ValidationType = ValidationType.None };
            using (var reader = XmlReader.Create(GetDictionaryFile(), settings))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "term") continue;

                        if (XNode.ReadFrom(reader) is XElement el)
                        {
                            allTermCount++;
                            if (bool.Parse(el.Attribute("disabled")?.Value ?? "false")) continue;

                            var isSpecial = bool.Parse(el.Element("special")?.Value ?? "false");
                            if (!isSpecial)
                            {
                                sb.Append(el);
                                filteredTermCount++;
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Number of terms found: {allTermCount}\nFiltered term count: {filteredTermCount}");
            return sb.ToString();
        }

        private static string GetMatchingTerms(string element, string value)
        {
            int allTermCount = 0, filteredTermCount = 0;
            var sb = new StringBuilder();
            var settings = new XmlReaderSettings { ValidationType = ValidationType.None };
            using (var reader = XmlReader.Create(GetDictionaryFile(), settings))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "term") continue;

                        if (XNode.ReadFrom(reader) is XElement el)
                        {
                            allTermCount++;
                            if (bool.Parse(el.Attribute("disabled")?.Value ?? "false")) continue;

                            var elem = el.Element(element);
                            if (elem != null && Regex.IsMatch(elem.Value, value))
                            {
                                sb.Append(el);
                                filteredTermCount++;
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Number of terms found: {allTermCount}\nFiltered term count: {filteredTermCount}");
            return sb.ToString();
        }

        private static void MergeDictionaryFiles(string fileAPath, string fileBPath)
        {
            int fileATermCount = 0, fileBTermCount = 0;
            using (var writer = new XmlTextWriter("output.xml", Encoding.UTF8))
            {
                // Write Starting Tags
                writer.WriteStartDocument();
                writer.WriteComment(" terms.xml 2018-07-07 18:37");
                writer.WriteStartElement("grimoire");
                writer.WriteAttributeString("version", "1.0");
                writer.WriteAttributeString("timestamp", "0");
                writer.WriteStartElement("terms");

                // Write FileA
                var settings = new XmlReaderSettings { ValidationType = ValidationType.None };
                using (var reader = XmlReader.Create(fileAPath, settings))
                {
                    //reader.MoveToContent();
                    while (reader.Read())
                    {
                        if (reader.IsStartElement("term"))
                        {
                            if (bool.Parse(reader.GetAttribute("disabled") ?? "false")) continue;

                            fileATermCount++;
                            string xml = reader.ReadOuterXml();
                            writer.WriteRaw(xml);
                        }
                    }
                }

                // Write FileB
                using (var reader = XmlReader.Create(fileBPath, settings))
                {
                    reader.MoveToContent();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name != "term") continue;

                            if (XNode.ReadFrom(reader) is XElement el)
                            {
                                if (bool.Parse(el.Attribute("disabled")?.Value ?? "false")) continue;

                                fileBTermCount++;
                                writer.WriteRaw(el.ToString());
                            }
                        }
                    }
                }

                // Write Ending Tags
                //writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            Console.WriteLine($"Terms Found:\nFile A: {fileATermCount}\nFile B: {fileBTermCount}");
        }

        /// <summary>
        /// This will get some of the columns to file
        /// </summary>
        /// <returns></returns>
        private static string GetSummary()
        {
            int allTermCount = 0, filteredTermCount = 0;
            var sb = new StringBuilder();
            var settings = new XmlReaderSettings { ValidationType = ValidationType.None };
            using (var reader = XmlReader.Create(GetDictionaryFile(), settings))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "term") continue;

                        if (XNode.ReadFrom(reader) is XElement el)
                        {
                            allTermCount++;
                            if (bool.Parse(el.Attribute("disabled")?.Value ?? "false")) continue;
                            filteredTermCount++;
                            // Id
                            sb.Append("<item "+ el.Attribute("id") +" ");

                            // Special
                            sb.Append("special=\"");
                            if (el.Element("special") != null)
                            {
                                sb.Append(el.Element("special")?.Value + "\" ");
                            }
                            else
                            {
                                sb.Append("false\" ");
                            }

                            // Pattern
                            sb.Append("pattern=\"" + el.Element("pattern")?.Value + "\" ");

                            // Text
                            sb.Append("text=\"" + el.Element("text")?.Value + "\" ");

                            // Game Id
                            sb.Append("gameId=\"" + el.Element("gameId")?.Value + "\"");

                            // close
                            sb.Append("></item>\n\r\n");
                        }
                    }
                }
            }

            Console.WriteLine($"Number of terms found: {allTermCount}\nDisabled term count: {allTermCount - filteredTermCount}");
            return sb.ToString();
        }

        private static void WriteResults(string termsXml)
        {
            var replace = Regex.Replace(termsXml, @"\r\n|\r|\n", "");
            replace = Regex.Replace(replace, @"[ ]{2,}", " ");
            using (var writer = new XmlTextWriter("output.xml", Encoding.UTF8))
            {
                if (writer.Settings != null)
                {
                    writer.Settings.CheckCharacters = false;
                    writer.Settings.DoNotEscapeUriAttributes = true;
                }
                writer.WriteRaw(replace);
            }
        }

        private static string GetDictionaryFile()
        {
            return GetFileLocation("Dictionary.xml");
        }

        private static string GetFileLocation(string fileName)
        {
            var currentPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
            return currentPath.FullName + "\\" + fileName;
        }
    }
}

