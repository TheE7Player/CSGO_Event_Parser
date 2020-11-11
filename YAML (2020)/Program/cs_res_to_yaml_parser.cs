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
        // Holds the name of the event
        public string Name { get; set; }

        // Holds the comment of the event (if any)
        public string Comment { get; set; }

        // Holds the event type (with comma ',' if in multiple .res files)
        public string EventType { get; set; }

        // Holds the events attributes (if any)
        public List<string> Attributes { get; set; }

        // Holds multiple events (if any - Attributes will be set to null if so )
        public Dictionary<string, List<string>> MultiAttributes { get; set; }
        
        // Copies the entire class instance to a new instace of itself to pervent pointer deallocation
        public Event Copy()
        {
            // Create the instance of itself
            var self = new Event { Name = Name, Comment = Comment, EventType = EventType, Attributes = Attributes };
            
            // Assign the current holding itself and clear it
            Name = Comment = EventType = "";
            
            // Deallocate the attributes that were stored as well
            Attributes = null;
            MultiAttributes = null;

            // Finally return the created instace
            return self;
        }
    }

    class Program
    {
        // Solves the issue with some comments which contain Quotation Marks (") within the comment
        private static string SortMultiCommentQuotes(string input, out int StartIndex)
        {
            // Create a StringBuild for safe text buffering           
            var sb = new StringBuilder(input.Length);

            // Create a tracker to keep count of the comma depth (should be 2 if no comment ( 3 if so ) )
            int commaCount = 0;

            // Split the string into a character array to parse each character
            char[] characters = input.ToCharArray();

            // Foreach character in the string
            for (int i = 0; i < characters.Length; i++)
            {
                // If the comma depth hits 2
                if (commaCount == 2)
                {
                    // Increment it by 1
                    i++;

                    // Return back where the quote index is
                    StartIndex = i;

                    // Add an apostrophe to the buffer
                    sb.Append('\'');

                    // Increment again by one
                    i++;

                    // Loop through the entire array minus one
                    while (i < characters.Length-1)
                    {
                        // Append each char and increment by 1
                        sb.Append(characters[i]);
                        i++;
                    }

                    // Add another apostrophe at the end
                    sb.Append('\'');

                    // Finally return back the string that was built in the buffer
                    return sb.ToString();
                }

                // Increment the depth by one if the next character (if any) is a comma
                if (characters[i] == ',') commaCount++;
            }

            // Set the StartIndex to -1 if not any found
            StartIndex = -1;

            // Return empty if not found
            return "";
        }

        // Location of where the *.res files
        private static string location = @"C:\Users\Owner\Desktop\events";

        // Location of where to output the final .yaml file
        private static string outLocation = @"C:\Users\Owner\Desktop\events\events.yaml";

        // List of events and contents of the yaml file
        private static List<Event> events;
        private static List<string> new_yaml;

        // Generate the yaml contents for each .res file
        static void GenerateYaml(string resName, string[] resFile)
        {
            // Create the cache variables
            string name = null, comment = null, attribute = null; bool isMulti = false;

            string initialType; int initialIndex = 0;

            // Create the event instance we use for each event
            var _event = new Event();

            // Loop through the entire .res file given in resFile
            for (int i = 0; i < resFile.Length; i++)
            {

                // Trim the contents to make parsing easier
                resFile[i] = resFile[i].Trim();

                // Get rid of initial comments
                if (resFile[i].StartsWith("//")) continue;

                // Check if the current line has quotes and the next line contains an open-curely bracket
                if (resFile[i].Contains('"') && resFile[i + 1].Contains("{"))
                {
                    // Lets store its name by cutting the indexes of the quotes
                    name = resFile[i].Substring(resFile[i].IndexOf("\"") + 1, resFile[i].LastIndexOf("\"") - 1);

                    // Validate if the event is already there ( Means it occurs in more than 1 .res file )
                    if(events.Any(ev => ev.Name == name))
                    {
                        // Grab the current index of the event that is already made
                        initialIndex = events.FindIndex(g => g.Name == name);

                        // Set the flag for true since we already have this event stored
                        isMulti = true;

                        // Grab the type string as we going to append its second event type onto it
                        initialType = events[initialIndex].EventType;

                        // Get the MultiAttributes ready
                        if (events[initialIndex].MultiAttributes is null) events[initialIndex].MultiAttributes = new Dictionary<string, List<string>>();

                        // Move the attribute from Attributes into MultiAttributes and clear it
                        events[initialIndex].MultiAttributes.Add(initialType, events[initialIndex].Attributes);
                        events[initialIndex].Attributes = null;

                        // Append the new type
                        events[initialIndex].EventType = $"{initialType},{resName}";

                        // Re-append the event ( as it can be set to null if not )
                        _event.Name = events[initialIndex].Name; 
                        _event.EventType = events[initialIndex].EventType; 
                        _event.Comment = events[initialIndex].Comment;

                        // Display back to the console this event has been stored already and jump to the next line
                        Console.WriteLine($"[{resName}] Now parsing {name} (Dupe for {resName}) @ idx {i}");
                        continue;
                    }
                    else
                    {
                        // The event is not a multi - so we append it as a single
                        isMulti = false;

                        // Check if the event contains a comment (if any)
                        if (resFile[i].Contains("//")) { comment = resFile[i].Substring(resFile[i].IndexOf("//") + 2).Trim(); } else { comment = string.Empty; }
                        
                        // Increment the index by 1
                        i++;

                        // Store the names and other attributes
                        _event.Name = name; _event.EventType = resName; _event.Comment = comment;

                        // Display the event is stored and jump to the next line
                        Console.WriteLine($"[{resName}] Now parsing {name} @ idx {i}");
                        continue;
                    }
                }

                // If the current line is empty, skip this line
                if (string.IsNullOrEmpty(resFile[i])) continue;

                // If the current line contains an open-curely bracket, skip this line
                if (resFile[i].Contains('{')) continue;

                // If the current line contains a closed-curely bracket ( usually when hit the end of the event )
                if (resFile[i].Contains('}'))
                {
                    // Chance that the parser breaks/fails at the end, break if so
                    if(i >= resFile.Length-1)
                    {
                        break;
                    }

                    // If the current parsed event is a multiple named event
                    if(isMulti)
                    {
                        // Add the current Attributes list into the MultiAttribute list and null it
                        events[initialIndex].MultiAttributes.Add(resName, _event.Attributes);
                        events[initialIndex].Attributes = null;
                    }

                    // Reset the multi-event back to false
                    isMulti = false;

                    // If the event name is still null at this point - an error happened.
                    if (string.IsNullOrEmpty(_event.Name))
                        throw new Exception($"An Event Broke Parser - Missing name\nError happend at index {i}");

                    // We now append the copied variation of the event and start all over
                    events.Add(_event.Copy());

                    // We jump to the next line
                    continue;
                }

                // If the event Attributes list isn't initialized, we do so to prevent errors
                if (_event.Attributes is null)
                    _event.Attributes = new List<string>(2);

                // We parse the current line to a function which splits it down into a valid yaml format
                attribute = GetArgumentData(resFile[i]);
               
                // We now at the yaml syntax of an element on top of it
                attribute = $"- [{attribute}]";

                // We now append the line onto the Attributes list
                _event.Attributes.Add(attribute);

                // We now set it to null to restart the process
                attribute = null;
            }
        }

        static void Main(string[] args)
        {
            // Create 2 Lists: One contain events (events) and one for the new yaml file (new_yaml)
            events = new List<Event>(50); new_yaml = new List<string>(50);

            // Load in the files from the given folder (location) and only get the .res files
            var filesEvent = Directory.GetFiles(location).Where(x => Path.GetExtension(x) == ".res");

            // Create a dictionary that stores the event type based on the current parsed file
            var types = new Dictionary<string, string>
            {
                { "gameevents.res", "gameevents"},
                { "modevents.res", "cstrikeevents"},
                { "serverevents.res", "engineevents"}
            };

            // Create variables for caching and modifying
            string[] file; string lookfor; int trimIndex;

            // Foreach each individual .res file
            foreach (var csEvent in filesEvent)
            {
                // fill in the file array (file) for each .res file
                file = File.ReadAllLines(csEvent);

                // Each .res has a parent node, this is where the events are located
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

            // Now we triple check for any duplicates that got passed from the stage below
            var finalEvents = new List<Event>();

            // Loop through each event
            foreach (var item in events)
            {
                // Discard broken/faulty events that was parsed
                if (string.IsNullOrEmpty(item.Name) && item.Attributes is null)
                    continue;

                // Prevent duplicates that may occur
                if (!finalEvents.Any(x => x.Name == item.Name))
                {
                    finalEvents.Add(item);
                }
            }

            // Reassign the events to the finalsEvents list
            events = finalEvents;

            //Deallocate the list as its now redundant
            finalEvents = null;
            
            // Now we add the comments into the yaml file
            new_yaml.Add("# TheE7Player CS:GO *.res to *.yaml Parser\n");
           
            new_yaml.Add("# events.yaml ~ Generated by parsing .res files in pak01_dir.vpk (CS:GO)");
            new_yaml.Add($"# Generation of such is valid as from {DateTime.Now.Day}/{DateTime.Now.Month}/{DateTime.Now.Year} (DD/MM/YYYY) [Time of parsing *.res]");
            new_yaml.Add("# Some events are invoked by other events with the same name, these will be presented with a comma in its type");
            new_yaml.Add("# The attributes will contain a underscore with a single letter to represent the same event with its variation (_g, _m etc)\n");

            // Now we add the extra details
            new_yaml.Add("# Details");
            new_yaml.Add($"# ~ {events.Count} Available Events");

            // Do a tally of the numbers of events type
            int game = 0, mod = 0, sev = 0, group = 0;
            foreach (var item in events)
            {
                if (item.EventType == "gameevents") { game++; }
                else
                if (item.EventType == "modevents") { mod++; }
                else
                if (item.EventType == "serverevents") { sev++; }
                else { group++; }
            }

            // Display the events distribution
            new_yaml.Add($"# ~ gameevents.res : {game}");
            new_yaml.Add($"# ~ modevents.res : {mod}");
            new_yaml.Add($"# ~ serverevents.res : {sev}");
            new_yaml.Add($"# ~ duplicate events (Events which occur in 2 or more *.res files) : {group}\n");

            // Now we go through each event
            foreach (var item in events)
            {
                // Check again if any rouge events that got through the last 3 checks
                if (string.IsNullOrEmpty(item.Name) && item.Attributes is null)
                    continue;

                // Add the event name
                new_yaml.Add($"{item.Name}:");

                // Check if the event has a comment (//)
                if(!string.IsNullOrEmpty(item.Comment))
                    new_yaml.Add($"   comment: \"{item.Comment}\"");

                // Now we add the events type 
                new_yaml.Add($"   type: \"{item.EventType}\"");

                // Check if the event has 2 or more signatures (occurs in 2+ *.res)
                if (item.Attributes is null)
                {
                    // Validate if Multi... is empty, which means we don't have multiple attributes
                    if (item.MultiAttributes is null)
                    { new_yaml.Add($"   attributes: []"); } // Append an empty array
                    else
                    {
                        // Now we go through each variation of the event
                        foreach (var evnt in item.MultiAttributes)
                        {
                            // Append the right key name for the event
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

                            // Now we amend the variables/attributes
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
                    // Event has attributes!
                    // amend each one!
                    new_yaml.Add($"   attributes:");
                    foreach (var evt in item.Attributes)
                    {
                        new_yaml.Add($"     {evt}");
                    }
                }

                // Add a newline for the next event
                new_yaml.Add("\n");
            }

            // Remove last 2 index as they tend to be a gap
            new_yaml.RemoveAt(new_yaml.Count-1);
            new_yaml.RemoveAt(new_yaml.Count-1);

            // Then we write the .yaml file location we want to extract it to (outLocation)
            File.WriteAllLines(outLocation, new_yaml);

            // Print and display the result to the user
            Console.WriteLine($"Conversion was successful, Saved in {outLocation}");
            Console.ReadLine(); // Pauses console, prevents quick close
        }
    
        static string GetArgumentData(string argument)
        {
            // character buffer
            char[] c = argument.ToCharArray();

            // safe-text buffer
            var sb = new StringBuilder();

            // final-safe-text buffer ( output/return value )
            var output = new StringBuilder();

            // flag to let us know if the attribute contains a comment ( if any )
            bool containsComment = argument.Contains("//");

            // extra information and tracking about the attribute
            int depthCount = 0; int commentCount = 0;

            // track the quote depth to make sure we know when it truly is the end of the line
            // Alternative: Use a stack implementation ( cost heavy though )
            int quoteDepth = argument.Count(x => x == '"');

            // For each character in the string
            for (int i = 0; i < c.Length; i++)
            {
                // Continue if the character is a space
                if (char.IsWhiteSpace(c[i])) continue;

                // If the depthCount is 3 ( comment )
                if(depthCount == 3)
                {
                    // If the sb contains anything, clear it
                    if(sb.Length > 0) sb.Clear();

                    // We now skip character until we hit the two `//`
                    while (commentCount != 2)
                    {
                        if (c[i] == '/') commentCount++;

                        // Increment by one to prevent INF-LOOP
                        i++;
                    }

                    // Skip again as they tend to be a space after '//' in the .res file
                    if (char.IsWhiteSpace(c[i])) i++;

                    // Now we loop onwards from the comments index of the text
                    while (i < c.Length)
                    {
                        /*
                            [YAML] Just like C# and many other languages, you cannot have
                            quotes (") while in other quotes. We need to use a escape character
                            '\"' in place of a quote.
                        */
                        
                        // If the current character is a quote
                        if (c[i] == '"')
                        { 
                            // Append \" to the buffer
                            sb.Append('\\'); sb.Append('"'); 
                        }
                        else
                            sb.Append(c[i]); // Else, just add the character into the buffer
                        
                        i++; // Increment by one to prevent INF-LOOP
                    }

                    // Apppend the comment to the output buffer
                    output.Append($"{sb}\"");

                    // Finally return the output buffer to the function
                    return output.ToString();
                }

                // If the current character is a quote
                if(c[i] == '"')
                {
                    // Append the quote
                    sb.Append(c[i]);

                    // Increment by one
                    i++;

                    // Loop until we hit another quote
                    while (c[i] != '"')
                    {
                        sb.Append(c[i]); i++; // Add the character and increment by one
                    }

                    // Add the quote at the end of the text buffer
                    sb.Append(c[i]);

                    // Increment the depthCount by 1
                    depthCount++;
                }

                // If depthCount is two ( meaning the attributes data type )
                if(depthCount == 2)
                {
                    // We meed to validate if the attribute has a comment 
                    if(!containsComment)
                    {
                        // If not, we append text onto the output buffer
                        output.Append($"{sb}");

                        // And then return back to the function
                        return output.ToString();
                    }

                    // Else we prepare the other element for the comment
                    output.Append($"{sb}, \"");

                    // We clear the sb buffer for the comment to be amended
                    sb.Clear();

                    // We set the depthCount to 3 to amend the comment
                    depthCount = 3;
                }
                else
                {
                    // If not, (usually dC is 1) - we amend the attribute name and prepare for the 
                    // other element which would be the attributes data type
                    output.Append($"{sb}, ");

                    // We clear the sb buffer for the data type to be amended
                    sb.Clear();
                }                
            }

            // Then we return back the string if these conditions here are never met
            return output.ToString();
        }
    }
}
