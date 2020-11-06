/*
        Copyright(c) James G / TheE7Player

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace csev_parser
{
    public class Event
    { 
        public string Name { get; set; }
        public string Comment { get; set; }
        public string EventType { get; set; }
        public List<string> Attribrutes { get; set; }
        public Dictionary<string, List<string>> MultiAttribrutes { get; set; }
        public Event Copy()
        {
            var self = new Event { Name = Name, Comment = Comment, EventType = EventType, Attribrutes = Attribrutes };
            Name = Comment = EventType = "";
            Attribrutes = null;
            return self;
        }
    }

    class Program
    {
        private static string SortMultiCommentQuotes(string input, out int StartIndex)
        {
            var sb = new StringBuilder(input.Length);
            int commaCount = 0;
            char[] characters = input.ToCharArray();

            for (int i = 0; i < characters.Length; i++)
            {
                if (commaCount == 2)
                {
                    i++;
                    StartIndex = i;
                    sb.Append('\'');
                    i++;
                    while (i < characters.Length-1)
                    {
                        sb.Append(characters[i]);
                        i++;
                    }
                    sb.Append('\'');
                    return sb.ToString();
                }

                if (characters[i] == ',') commaCount++;
            }
            StartIndex = -1;
            return "";
        }

        private static string location = @"C:\Users\Owner\Desktop\events";
        private static string outLocation = @"C:\Users\Owner\Desktop\events\events.yaml";

        private static List<Event> events;
        private static List<string> new_yaml;
        static void GenerateYaml(string resName, string[] resFile)
        {
            string name = null, comment = null, attribrute = null; bool isMulti = false;

            string initialType; int initialIndex = 0;

            var _event = new Event();

            for (int i = 0; i < resFile.Length; i++)
            {
                resFile[i] = resFile[i].Trim();

                // Get rid of initiall comments
                if (resFile[i].StartsWith("//")) continue;

                if (resFile[i].Contains('"') && resFile[i + 1].Contains("{"))
                {
                    name = resFile[i].Substring(resFile[i].IndexOf("\"") + 1, resFile[i].LastIndexOf("\"") - 1);

                    if(events.Any(ev => ev.Name == name))
                    {
                        initialIndex = events.FindIndex(g => g.Name == name);
                        isMulti = true;

                        initialType = events[initialIndex].EventType;

                        if (events[initialIndex].MultiAttribrutes is null) events[initialIndex].MultiAttribrutes = new Dictionary<string, List<string>>();

                        events[initialIndex].MultiAttribrutes.Add(initialType, events[initialIndex].Attribrutes);
                        events[initialIndex].Attribrutes = null;

                        events[initialIndex].EventType = $"{initialType},{resName}";

                        _event.Name = events[initialIndex].Name; 
                        _event.EventType = events[initialIndex].EventType; 
                        _event.Comment = events[initialIndex].Comment;
                        Console.WriteLine($"[{resName}] Now parsing {name} (Dupe for {resName}) @ idx {i}");
                        continue;
                    }
                    else
                    {
                        isMulti = false;

                        if (resFile[i].Contains("//")) { comment = resFile[i].Substring(resFile[i].IndexOf("//") + 2).Trim(); } else { comment = string.Empty; }
                        i++;
                        _event.Name = name; _event.EventType = resName; _event.Comment = comment;

                        Console.WriteLine($"[{resName}] Now parsing {name} @ idx {i}");
                        continue;
                    }
                }


                if (string.IsNullOrEmpty(resFile[i])) continue;

                if (resFile[i].Contains('{')) continue;

                if (resFile[i].Contains('}'))
                {
                    if(i >= resFile.Length-1)
                    {
                        break;
                    }

                    if(isMulti)
                    {
                        events[initialIndex].MultiAttribrutes.Add(resName, _event.Attribrutes);
                        events[initialIndex].Attribrutes = null;
                    }

                    isMulti = false;

                    if (string.IsNullOrEmpty(_event.Name))
                        throw new Exception($"An Event Broke Parser - Missing name\nError happend at index {i}");

                    events.Add(_event.Copy());
                    continue;
                }

                if (_event.Attribrutes is null)
                    _event.Attribrutes = new List<string>(2);

                attribrute = GetAgrumentData(resFile[i]);
               
                attribrute = $"- [{attribrute}]";
                _event.Attribrutes.Add(attribrute);
                attribrute = null;
            }
        }

        static void Main(string[] args)
        {

            events = new List<Event>(50); new_yaml = new List<string>(50);

            var filesEvent = Directory.GetFiles(location).Where(x => Path.GetExtension(x) == ".res");

            new_yaml.Add("# TheE7Player CS:GO *.res to *.yaml Parser\n");

            new_yaml.Add("# events.yaml ~ Generated by parsing .res files in pak01_dir.vpk (CS:GO)");
            new_yaml.Add($"# Generation of such is valid as from {DateTime.Now.Day}/{DateTime.Now.Month}/{DateTime.Now.Year} (DD/MM/YYYY) [Time of parsing *.res]");
            new_yaml.Add("# Some events are invoked by other events with the same name, these will be presented with a comma in its type");
            new_yaml.Add("# The attributes will contain a underscore with a single letter to represent the same event with its variation (_g, _m etc)\n");

            var types = new Dictionary<string, string>
            {
                { "gameevents.res", "gameevents"},
                { "modevents.res", "cstrikeevents"},
                { "serverevents.res", "engineevents"}
            };

            string[] file; string lookfor; int trimIndex;
            foreach (var csEvent in filesEvent)
            {
                file = File.ReadAllLines(csEvent);

                lookfor = $"\"{types[Path.GetFileName(csEvent)]}\"";

                /* Ignore all the initial comments
                
                    file starts with "<event name>" { ...

                    gameevents.res -> gameevents
                    modevents.res -> cstrikeevents
                    serverevents.res -> engineevents
                
                */

                // Find the starting of each file
                trimIndex = Array.FindIndex(file, f => f == lookfor);

                // Trim the array to get rid of unnesserary comments at the top
                file = file.Skip(trimIndex + 2).ToArray();

                // Start the process
                GenerateYaml(Path.GetFileNameWithoutExtension(csEvent), file);
            }

            // Details
            new_yaml.Add("# Details");

            var sList = events.Where(x => !(string.IsNullOrEmpty(x.Name) && (x.Attribrutes is null)));
            int TotalEvents = sList.Count();
            new_yaml.Add($"# ~ {TotalEvents} Available Events");

            int game = 0, mod = 0, sev = 0, group = 0;
            foreach (var item in sList)
            {
                if (item.EventType == "gameevents") { game++; }
                else
                if (item.EventType == "modevents") { mod++; }
                else
                if (item.EventType == "serverevents") { sev++; }
                else { group++; }
            }

            new_yaml.Add($"# ~ gameevents.res : {game}");
            new_yaml.Add($"# ~ modevents.res : {mod}");
            new_yaml.Add($"# ~ serverevents.res : {sev}");
            new_yaml.Add($"# ~ duplicate events (Events which occur in 2 or more *.res files) : {group}\n");
            foreach (var item in events)
            {
                if (string.IsNullOrEmpty(item.Name) && item.Attribrutes is null)
                    continue;

                // Add the event name
                new_yaml.Add($"{item.Name}:");

                if(!string.IsNullOrEmpty(item.Comment))
                    new_yaml.Add($"   comment: \"{item.Comment}\"");

                new_yaml.Add($"   type: \"{item.EventType}\"");

                if (item.Attribrutes is null)
                {
                    if (item.MultiAttribrutes is null)
                    { new_yaml.Add($"   attributes: []"); }
                    else
                    {
                        foreach (var evnt in item.MultiAttribrutes)
                        {
                            if (evnt.Key == "gameevents") 
                            { 
                                new_yaml.Add($"   attributes_g:"); 
                            }

                            if (evnt.Key == "modevents") 
                            { 
                                new_yaml.Add($"   attributes_m:");
                            }

                            if (evnt.Key == "serverevents") 
                            { 
                                new_yaml.Add($"   attributes_s:");
                            }

                            if(evnt.Value is null)
                            {
                                new_yaml[new_yaml.Count - 1] = $"{new_yaml[new_yaml.Count-1]} []";
                            }
                            else
                            foreach (var evt in evnt.Value)
                            {
                                new_yaml.Add($"     {evt}");
                            }
                        }
                    }
                }
                else
                {
                    new_yaml.Add($"   attributes:");
                    foreach (var evt in item.Attribrutes)
                    {
                        new_yaml.Add($"     {evt}");
                    }
                }
                new_yaml.Add("\n");
            }

            // Remove last 2 index as they tend to be a gap
            new_yaml.RemoveAt(new_yaml.Count-1);
            new_yaml.RemoveAt(new_yaml.Count-1);

            File.WriteAllLines(outLocation, new_yaml);

            Console.WriteLine($"Conversion was sucessful, Saved in {outLocation}");
            Console.ReadLine();
        }
    
        static string GetAgrumentData(string agrument)
        {
            char[] c = agrument.ToCharArray();
            var sb = new StringBuilder();
            var output = new StringBuilder();

            bool containsComment = agrument.Contains("//");
            int depthCount = 0; int commentCount = 0;
            for (int i = 0; i < c.Length; i++)
            {
                if (char.IsWhiteSpace(c[i])) continue;

                if(depthCount == 3)
                {
                    if(sb.Length > 0) sb.Clear();

                    while (commentCount != 2)
                    {
                        if (c[i] == '/') commentCount++;
                        i++;
                    }

                    if (char.IsWhiteSpace(c[i])) i++;

                    while (i < c.Length)
                    {
                        sb.Append(c[i]);i++;
                    }
                    output.Append($"{sb}'");
                    return output.ToString();
                }

                if(c[i] == '"')
                {
                    sb.Append(c[i]);
                    i++;
                    while (c[i] != '"')
                    {
                        sb.Append(c[i]); i++;
                    }
                    sb.Append(c[i]);
                    depthCount++;
                }

                if(depthCount == 2)
                {
                    if(!containsComment)
                    {
                        output.Append($"{sb}");
                        return output.ToString();
                    }
                    output.Append($"{sb}, '");
                    sb.Clear();
                    depthCount = 3;
                }
                else
                {
                    output.Append($"{sb}, ");
                    sb.Clear();
                }                
            }

            return output.ToString();
        }
    }
}
