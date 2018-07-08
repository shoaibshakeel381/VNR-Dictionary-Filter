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
            PrintUsage();

            try
            {
                string terms = "<?xml version=\"1.0\" encoding=\"utf-8\"?><!-- terms.xml 2018-07-07 13:11-->" +
                               "<grimoire version=\"1.0\" timestamp=\"0\"><terms>";

                if (args.Length == 0)
                {
                    terms += GetGlobalTerms();
                }
                else if (args[0].Equals("file_id"))
                {
                    var gameIds = args[1].Split(',').Select(int.Parse).ToList();
                    terms += GetGameSpecificTerms(gameIds);

                    if (args.Any(a => a.Equals("-g")))
                    {
                        terms += GetGlobalTerms();
                    }
                }
                else if (args[0].Equals("print"))
                {
                    terms += GetSummary();
                }
                else if (args.Length == 3 && args[0].Equals("remove"))
                {
                    var gameIds = args[2].Split(',').Select(int.Parse).ToList();
                    terms += GetTermsUnrelatedToGame(GetFileLocation(args[1]), gameIds);
                }
                else if (args.Length == 3 && args[0].Equals("merge"))
                {
                    MergeDictionaryFiles(GetFileLocation(args[1]), GetFileLocation(args[2]));
                    return;
                }
                else if (args.Length == 3 && args[0].Equals("element"))
                {
                    var element = args[1];
                    var value = args[2];
                    terms += GetMatchingTerms(element, value);
                }
                else
                {
                    Console.WriteLine("Invalid input");
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
                            "   DictFilter.exe file_id   <id> [-g]\n" +
                            "   DictFilter.exe element   <element_name> <value>\n" +
                            "   DictFilter.exe merge     <fileA> <fileB>\n" +
                            "   DictFilter.exe remove    <file> <file_id>\n" +
                            "   DictFilter.exe print\n\n" +
                            "Details:\n" +
                            "   file_id   Returns game specific terms.\n" +
                            "             File Ids can be found from Edit Dialog under Game\n" +
                            "             info page. Multiple File Ids should be separated\n" +
                            "             by comma. if -g is sepecified then full dictionary\n" +
                            "             will be created, including global terms.\n" +
                            "   element   Returns terms where <element_name> has value matching\n" +
                            "             the given <value>. <value> can be a regular expression.\n" +
                            "   merge     Merges two dictionary files and produces a new file.\n" +
                            "             Both files should be present in current directory.\n" + 
                            "             Each file must have a root element as parent to make xml valid.\n" +
                            "   remove    Remove game specific terms from given dict file.\n" +
                            "             File Ids can be found from Edit Dialog under Game\n" +
                            "             info page. Multiple File Ids should be separated by comma.\n" +
                            "   print     Prints Id, Special, Pattern, Text and Game Id to xml file.\n\n" +
                            "   If no parameter is provided, then global terms will be returned.");
        }

        private static string GetGameSpecificTerms(ICollection<int> targetGameIds)
        {
            var sb = new StringBuilder();
            using (var reader = XmlReader.Create(GetDictionaryFile()))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "term") continue;

                        if (XNode.ReadFrom(reader) is XElement el)
                        {
                            var gameId = el.Element("gameId");
                            if (gameId != null && targetGameIds.Contains(int.Parse(gameId.Value)))
                                sb.Append(el);
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static string GetTermsUnrelatedToGame(string filePath, ICollection<int> targetGameIds)
        {
            var sb = new StringBuilder();
            using (var reader = XmlReader.Create(filePath))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "term") continue;

                        if (XNode.ReadFrom(reader) is XElement el)
                        {
                            var gameId = el.Element("gameId");
                            if (gameId == null || !targetGameIds.Contains(int.Parse(gameId.Value)))
                                sb.Append(el);
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static string GetGlobalTerms()
        {
            var sb = new StringBuilder();
            using (var reader = XmlReader.Create(GetDictionaryFile()))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "term") continue;

                        if (XNode.ReadFrom(reader) is XElement el)
                        {
                            var special = el.Element("special");
                            if (special == null || !bool.Parse(special.Value) || el.Element("gameId") == null)
                                sb.Append(el);
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static string GetMatchingTerms(string element, string value)
        {
            var sb = new StringBuilder();
            using (var reader = XmlReader.Create(GetDictionaryFile()))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "term") continue;

                        if (XNode.ReadFrom(reader) is XElement el)
                        {
                            var elem = el.Element(element);
                            if (elem != null && Regex.IsMatch(elem.Value, value))
                                sb.Append(el);
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static void MergeDictionaryFiles(string fileAPath, string fileBPath)
        {
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
                using (var reader = XmlReader.Create(fileAPath))
                {
                    reader.MoveToContent();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name != "term") continue;

                            if (XNode.ReadFrom(reader) is XElement el)
                            {
                                writer.WriteRaw(el.ToString());
                            }
                        }
                    }
                }

                // Write FileB
                using (var reader = XmlReader.Create(fileBPath))
                {
                    reader.MoveToContent();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name != "term") continue;

                            if (XNode.ReadFrom(reader) is XElement el)
                            {
                                writer.WriteRaw(el.ToString());
                            }
                        }
                    }
                }

                // Write Ending Tags
                //writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// This will get some of the columns to file
        /// </summary>
        /// <returns></returns>
        private static string GetSummary()
        {
            var sb = new StringBuilder();
            using (var reader = XmlReader.Create(GetDictionaryFile()))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name != "term") continue;

                        if (XNode.ReadFrom(reader) is XElement el)
                        {
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

            return sb.ToString();
        }

        private static void WriteResults(string termsXml)
        {
            var replace = Regex.Replace(termsXml, @"\r\n|\r|\n", "");
            replace = Regex.Replace(replace, @"[ ]{2,}", " ");
            using (var writer = new XmlTextWriter("output.xml", Encoding.UTF8))
            {
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

