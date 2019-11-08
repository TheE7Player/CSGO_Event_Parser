using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JSON_Single
{
    internal class Program
    {
        //Added extra '\' in \s << [NOTE]
        private static Regex EventHeaderRegex = new Regex("^(?:\\s)?\"(.+)\"(?:\\s+|\n)?(?://\\s(.+))?");

        private static Regex AttibruteRegex = new Regex("^(?:\\s+)\"(.*?)\"(?:\\s+)\"(.*?)\"(?:\\s+//\\s(.+))?");

        //Method starts: (Remember: n - 1!)
        //modevents - Line 25 to 1078
        //serverevents - Line 17 to 127
        //gamevents - Line 24 to 578
        private static void Main(string[] args)
        {
            //Lists of type "Event" which holds each event on every *.res file
            List<Event> modevents = new List<Event>(20);
            List<Event> serverevents = new List<Event>(20);
            List<Event> gameevents = new List<Event>(20);

            //Start parser, target the file (.res), reference the wanted list, where the first event is in line and file name of .res
            DoSearch(@"...modevents.res", ref modevents, 25, "modevents");
            DoSearch(@"...serverevents.res", ref serverevents, 17, "serverevents");
            DoSearch(@"...gameevents.res", ref gameevents, 24, "gameevents");

            //A seperate list to join them all up together
            var AllEvents = new List<Event>();

            //Join all 3 lists together
            AllEvents.AddRange(modevents);
            AllEvents.AddRange(serverevents);
            AllEvents.AddRange(gameevents);

            //New output of IEnumerable<string> to attempt to get rid of any duplicates, then select each element and get it's string
            var output = AllEvents.Distinct().Select(x => x.ToString());
            
            #region ATTEMPT_WRITE_FILE

            try
            {
                //Simple IO, write out what is in output into event.json file
                File.WriteAllLines(@"...events.json", output.ToArray());

                Console.WriteLine("FILE Created!");
                Console.ReadLine();
            }
            catch (Exception)
            {
                Console.WriteLine("FILE DOESN'T EXIST... CHECK PROGRAM");
                Console.ReadLine();
            }

            #endregion ATTEMPT_WRITE_FILE
        }

        /// <summary>
        /// Parser logic
        /// </summary>
        /// <param name="path">The .res file you wish to parse information from</param>
        /// <param name="events">The List file you want to get the parser information into</param>
        /// <param name="SkipBy">What line the first location is an event (player_say etc) Line: (25, 17, 24)</param>
        /// <param name="fileType">What category the event is (.res name: modevents, serverevents, gameevents)</param>
        private static void DoSearch(string path, ref List<Event> events, int SkipBy, string fileType)
        {
         
            string[] _f = File.ReadAllLines(path);
            //events = new List<Event>(20);
            _f = _f.Skip(SkipBy - 1).ToArray(); //Skip over comments (Where first event is located)

            for (int i = 0; i < _f.Length; i++)
            {
                var match = EventHeaderRegex.Match(_f[i]);

                if (match.Success) //Is line event name? ("player_say", "player_ping" etc)
                {
                    if ((match.Groups.Count - 1) == 2 && !String.IsNullOrEmpty(match.Groups[2].Value))
                        events.Add(new Event { EventName = match.Groups[1].Value, Comment = match.Groups[2].Value, EventType = fileType });
                    else
                        events.Add(new Event { EventName = match.Groups[1].Value, EventType = fileType });

                    i++; //Jump one down
                    Match att;
                    while (i < _f.Length)
                    {
                        if (_f[i].StartsWith("\t{") || _f[i].StartsWith("\t}") || string.IsNullOrEmpty(_f[i]))
                        {
                            if (_f[i].StartsWith("\t}"))
                                break;
                        }
                        else
                        {
                            att = AttibruteRegex.Match(_f[i]);
                            if (att.Success)
                            {
                                //This is an attibrute!
                                if ((att.Groups.Count - 1) == 3)
                                    events[events.Count - 1].AddAttribute(att.Groups[1].Value, att.Groups[2].Value, CleanComment(att.Groups[3].Value));
                                else
                                if ((att.Groups.Count - 1) == 2)
                                    events[events.Count - 1].AddAttribute(att.Groups[1].Value, att.Groups[2].Value);
                            }
                        }
                        i++; //Jump one down
                    }
                }
            }
        }

        /// <summary>
        /// Logic for the parse (DoSearch) to clean out the comments (Trim)
        /// </summary>
        /// <param name="comment">The comment to perform this operation on</param>
        /// <returns></returns>
        private static string CleanComment(string comment)
        {
            return comment.Replace("\t", " ").Trim();
        }
    }

    /// <summary>
    /// OOP Class to handle Events
    /// </summary>
    internal class Event
    {
        /// <summary>
        /// What category is this event from? (Server? Client? Game?)
        /// </summary>
        public string EventType { get; set; } //Is even from mod, game or server .res?

        /// <summary>
        /// The events name (player_say, player_chat, player_ping etc)
        /// </summary>
        public string EventName { get; set; } //The event name (player_say)

        /// <summary>
        /// The events comment (If any from .res file!)
        /// </summary>
        public string Comment { get; set; } //The event comment (if any)

        //Lazy Initialization as the Event may not have any attributes (Parameters!)
        private Lazy<List<E_Attributes>> attributes = new Lazy<List<E_Attributes>>();

        /// <summary>
        /// Add a attribute into an event
        /// </summary>
        /// <param name="Name">The attributes name</param>
        /// <param name="Type">The attributes type</param>
        /// <param name="Comment">The attributes comment (If any)</param>
        public void AddAttribute(string Name, string Type, string Comment = "") => attributes.Value.Add(new E_Attributes(Name, GetTypeFromText(Type), Comment));

        /// <summary>
        /// Taken directly from Java project (Same logic)
        /// </summary>
        /// <returns></returns>
        private static string GetTypeFromText(string input)
        {
            if (input.Equals("short") || input.Equals("long") || input.Equals("byte"))
                return "integer";

            if (input.Equals("string") || input.Equals("wstring"))
                return "string";

            if (input.Equals("bool"))
                return "bool";

            if (input.Equals("float"))
                return "float";

            return string.Empty;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (String.IsNullOrEmpty(Comment))
                sb.AppendLine($"\"{EventName}\"");
            else
                sb.AppendLine($"\"{EventName}\"  : {Comment}");

            sb.AppendLine("{");

            sb.AppendLine($"\t\"type\" : \"{EventType}\"");

            if (attributes.IsValueCreated)
                foreach (var item in attributes.Value)
                {
                    sb.AppendLine(item.ToString());
                }

            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    internal struct E_Attributes
    {
        private string Attribute_Name; //Holds attributes name
        private string Attribute_Type; //Is the attribute string, integer, boolean, float?
        private string Attribute_Comment; //Does it contain a note/comment?

        public E_Attributes(string Name, string type, string comment)
        {
            Attribute_Name = Name;
            Attribute_Type = type;
            Attribute_Comment = comment;
        }

        public override string ToString()
        {
            return $"\t\"{Attribute_Name}\"    \"{Attribute_Type}\"    \"{Attribute_Comment}\"";
        }
    }
}