using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace VNRSharedDictFilter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 1)
                {
                    Console.WriteLine("Getting global terms");
                    PrintGlobalTerms(GetFileLocation(args[0]));
                }
                else if (args.Length == 3 && args[0].Equals("gamespecific"))
                {
                    Console.WriteLine("Getting game specific terms");
                    var gameIds = args[2].Split(',').Select(int.Parse).ToList();
                    PrintTermsRelatedToGame(GetFileLocation(args[1]), gameIds, true);
                }
                else if (args.Length >= 2 && args[0].Equals("remove"))
                {
                    Console.WriteLine("Removing terms");
                    if (args.Length == 3)
                    {
                        var gameIds = args[2].Split(',').Select(int.Parse).ToList();
                        PrintTermsRelatedToGame(GetFileLocation(args[1]), gameIds, false);
                    }
                    else
                    {
                        PrintFilteredTermsByElementValue(GetFileLocation(args[1]), "term", ".*");
                    }
                }
                else if (args.Length == 3 && args[0].Equals("merge"))
                {
                    Console.WriteLine("Merging dictionaries");
                    MergeDictionaryFiles(GetFileLocation(args[1]), GetFileLocation(args[2]));
                }
                else if (args.Length == 4 && args[0].Equals("element"))
                {
                    Console.WriteLine("Filtering terms based on given criteria");
                    var element = args[2];
                    var value = args[3];
                    PrintFilteredTermsByElementValue(GetFileLocation(args[1]), element, value);
                }
                else
                {
                    PrintUsage();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:\n" +
                              "   DictFilter.exe                <dictionary_file>\n" +
                              "   DictFilter.exe gamespecific   <dictionary_file> <game_file_id>\n" +
                              "   DictFilter.exe element        <dictionary_file> <element_name> <value>\n" +
                              "   DictFilter.exe merge          <dictionary_fileA> <dictionary_fileB>\n" +
                              "   DictFilter.exe remove         <dictionary_file> [file_id]\n" +
                              "\n" +
                              "Details:\n" +
                              "   gamespecific    Returns game specific terms. Filteration will be\n" +
                              "                   done by File Ids. File Ids can be found from Edit\n" +
                              "                   Dialog under Game info page. Multiple ids\n" +
                              "                   should be separated by comma.\n" +
                              "   element         Returns terms where <element_name> has value matching\n" +
                              "                   the given <value>. <value> can be a regular expression.\n" +
                              "                   Any terms which don't have elemnt information will be \n" + 
                              "                   ignored.\n" +
                              "   merge           Merges two dictionary files and produces a new file.\n" +
                              "                   Both files should be present in current directory.\n" + 
                              "                   Each file must have a root element as parent to make xml valid.\n" +
                              "   remove          Remove game specific terms from given dict file.\n" +
                              "                   File Ids can be found from Edit Dialog under Game\n" +
                              "                   info page. Multiple ids should be separated by comma.\n" +
                              "                   If file_id is not specified then only disabled terms\n" +
                              "                   will be removed.\n" +
                              "\n" +
                              "   If only dictionary file is provided without any other parameters then global\n" +
                              "   terms will be returned.\n" +
                              "   If no parameter is provided, then this guide will be printed.\n" +
                              "\n" +
                              "NOTE: Disabled terms will be always be ignored.");
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="targetGameIds"></param>
        /// <param name="specific">if true then game specific terms will be returned.<br/>If false then terms not related to game will be returned.</param>
        /// <returns></returns>
        private static void PrintTermsRelatedToGame(string filePath, ICollection<int> targetGameIds, bool specific)
        {
            int allTermCount = 0, filteredTermCount = 0, disabledTermCount = 0;
            using (var writer = new XmlTextWriter(GetOutputFileName(), Encoding.UTF8))
            {
                // Write Starting Tags
                writer.WriteStartDocument();
                writer.WriteComment(" terms.xml 2018-07-07 18:37");
                writer.WriteStartElement("grimoire");
                writer.WriteAttributeString("version", "1.0");
                writer.WriteAttributeString("timestamp", "0");
                writer.WriteStartElement("terms");

                using (var reader = XmlReader.Create(filePath, GetXmlReaderSettings()))
                {
                    reader.ReadToFollowing("term");

                    do
                    {
                        allTermCount++;
                        var termXml = reader.ReadOuterXml();
                        if (IsTermDisabled(termXml))
                        {
                            disabledTermCount++;
                            continue;
                        }

                        var gameId = GetGameId(termXml);
                        var isSpecial = GetIsSpecial(termXml);

                        if (specific)
                        {
                            if (!isSpecial || !targetGameIds.Contains(gameId)) continue;
                        }
                        else
                        {
                            if (isSpecial && targetGameIds.Contains(gameId)) continue;
                        }

                        writer.WriteRaw(termXml);
                        filteredTermCount++;
                    } while (reader.IsStartElement("term"));
                }

                // Write Ending Tags
                writer.WriteEndDocument();
            }
            
            Console.WriteLine($"Number of terms found: {allTermCount}\nFiltered term count: {filteredTermCount}\nDisabled term count: {disabledTermCount}");
        }

        private static void PrintGlobalTerms(string filePath)
        {
            int allTermCount = 0, filteredTermCount = 0, disabledTermCount = 0;
            using (var writer = new XmlTextWriter(GetOutputFileName(), Encoding.UTF8))
            {
                // Write Starting Tags
                writer.WriteStartDocument();
                writer.WriteComment(" terms.xml 2018-07-07 18:37");
                writer.WriteStartElement("grimoire");
                writer.WriteAttributeString("version", "1.0");
                writer.WriteAttributeString("timestamp", "0");
                writer.WriteStartElement("terms");

                using (var reader = XmlReader.Create(filePath, GetXmlReaderSettings()))
                {
                    reader.ReadToFollowing("term");

                    do
                    {
                        allTermCount++;
                        var termXml = reader.ReadOuterXml();
                        if (IsTermDisabled(termXml))
                        {
                            disabledTermCount++;
                            continue;
                        }

                        if (GetIsSpecial(termXml)) continue;

                        writer.WriteRaw(termXml);
                        filteredTermCount++;
                    } while (reader.IsStartElement("term"));
                }

                // Write Ending Tags
                writer.WriteEndDocument();
            }

            Console.WriteLine($"Number of terms found: {allTermCount}\nFiltered term count: {filteredTermCount}\nDisabled term count: {disabledTermCount}");
        }

        private static void PrintFilteredTermsByElementValue(string filePath, string element, string value)
        {
            int allTermCount = 0, filteredTermCount = 0, disabledTermCount = 0;
            using (var writer = new XmlTextWriter(GetOutputFileName(), Encoding.UTF8))
            {
                // Write Starting Tags
                writer.WriteStartDocument();
                writer.WriteComment(" terms.xml 2018-07-07 18:37");
                writer.WriteStartElement("grimoire");
                writer.WriteAttributeString("version", "1.0");
                writer.WriteAttributeString("timestamp", "0");
                writer.WriteStartElement("terms");

                using (var reader = XmlReader.Create(filePath, GetXmlReaderSettings()))
                {
                    reader.ReadToFollowing("term");

                    do
                    {
                        allTermCount++;
                        var termXml = reader.ReadOuterXml();
                        if (IsTermDisabled(termXml))
                        {
                            disabledTermCount++;
                            continue;
                        }

                        var elemValue = GetElementValue(termXml, element);
                        if (elemValue == null || !Regex.IsMatch(elemValue, value)) continue;

                        writer.WriteRaw(termXml);
                        filteredTermCount++;
                    } while (reader.IsStartElement("term"));
                }

                // Write Ending Tags
                writer.WriteEndDocument();
            }
            

            Console.WriteLine($"Number of terms found: {allTermCount}\nFiltered term count: {filteredTermCount}\nDisabled term count: {disabledTermCount}");
        }

        private static void MergeDictionaryFiles(string fileAPath, string fileBPath)
        {
            int fileATermCount = 0, fileBTermCount = 0, disabledTermCount = 0;
            using (var writer = new XmlTextWriter(GetOutputFileName(), Encoding.UTF8))
            {
                // Write Starting Tags
                writer.WriteStartDocument();
                writer.WriteComment(" terms.xml 2018-07-07 18:37");
                writer.WriteStartElement("grimoire");
                writer.WriteAttributeString("version", "1.0");
                writer.WriteAttributeString("timestamp", "0");
                writer.WriteStartElement("terms");

                // Write FileA
                using (var reader = XmlReader.Create(fileAPath, GetXmlReaderSettings()))
                {
                    reader.ReadToFollowing("term");

                    do
                    {
                        var termXml = reader.ReadOuterXml();
                        if (IsTermDisabled(termXml))
                        {
                            disabledTermCount++;
                            continue;
                        }
                        
                        writer.WriteRaw(termXml);
                        fileATermCount++;
                    } while (reader.IsStartElement("term"));
                }

                // Write FileB
                using (var reader = XmlReader.Create(fileBPath, GetXmlReaderSettings()))
                {
                    reader.ReadToFollowing("term");

                    do
                    {
                        var termXml = reader.ReadOuterXml();
                        if (IsTermDisabled(termXml))
                        {
                            disabledTermCount++;
                            continue;
                        }

                        writer.WriteRaw(termXml);
                        fileBTermCount++;
                    } while (reader.IsStartElement("term"));
                }

                // Write Ending Tags
                //writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            Console.WriteLine($"Terms Found:\nFile A: {fileATermCount}\nFile B: {fileBTermCount}\nDisabled term count: {disabledTermCount}");
        }
        
        private static string GetFileLocation(string fileName)
        {
            var currentPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
            return currentPath.FullName + "\\" + fileName;
        }

        private static XmlReaderSettings GetXmlReaderSettings()
        {
            return new XmlReaderSettings
            {
                ValidationType = ValidationType.None
            };
        }

        private static XmlDocument GetXmlDocument(string xml)
        {
            var document = new XmlDocument();
            document.LoadXml(xml);

            return document;
        }

        private static string GetElementValue(string xml, string element)
        {
            var elems = GetXmlDocument(xml).GetElementsByTagName(element);
            if (elems.Count > 0)
            {
                return elems[0].InnerText;
            }

            return null;
        }

        private static string GetAttributeValue(string xml, string attr)
        {
            var doc = GetXmlDocument(xml);
            var attribute = doc.DocumentElement?.Attributes[attr];
            return attribute?.Value;
        }

        private static bool IsTermDisabled(string xml)
        {
            var val = GetAttributeValue(xml, "disabled");
            return !string.IsNullOrWhiteSpace(val) && bool.Parse(val);
        }

        private static int GetGameId(string xml)
        {
            var gameId = -1;

            var value = GetElementValue(xml, "gameId");
            if (!string.IsNullOrWhiteSpace(value))
            {
                gameId = int.Parse(value);
            }

            return gameId;
        }

        private static bool GetIsSpecial(string xml)
        {
            var value = GetElementValue(xml, "special");

            return !string.IsNullOrWhiteSpace(value) && bool.Parse(value);
        }

        private static string GetOutputFileName()
        {
            return "DictFilterOut.xml";
        }
    }
}

