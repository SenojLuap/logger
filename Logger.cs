using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace paujo.Debug {
    public class Logger {

        /// <summary>
        /// Tags that may be applied to a logged message.
        /// Filters the endpoints a message is sent to.
        /// </summary>
        public enum LogTag {
            Info,
            Warning,
            Error,
            Trace,
            Debug
        }


        public static LogTag[] AllLogTags { get => (LogTag[])Enum.GetValues(typeof(LogTag)); }


        /// <summary>
        /// The default message formatting used when none is provided.
        /// "[Date][Time] <TAGS> Message"
        /// </summary>
        public readonly string DEFAULT_FORMAT = "[" + SPECIAL_CHARACTER + DATE_CHARACTER + "]["
                + SPECIAL_CHARACTER + TIME_CHARACTER + "] <"
                + SPECIAL_CHARACTER + TAGS_CHARACTER + "> "
                + SPECIAL_CHARACTER + MESSAGE_CHARACTER;
        
        
        public const char SPECIAL_CHARACTER = '$';
        public const char MESSAGE_CHARACTER = 'm';
        public const char TAGS_CHARACTER = 't';
        public const char TIME_CHARACTER = 'i';
        public const char DATE_CHARACTER = 'd';


        /// <summary>
        /// Map from a stream to the tags it should receive messages for.
        /// </summary>
        private IDictionary<Stream, (HashSet<LogTag>, RenderNode[])> targets;


        public Logger() {
            targets = new Dictionary<Stream, (HashSet<LogTag>, RenderNode[])>();
        }


        /// <summary>
        /// Add a 'target' to log messages to.
        /// </summary>
        /// <param name="target">The stream to send messages to</param>
        /// <param name="tags">The tags that should be sent to the stream</param>
        /// <param name="messageFormat">Format string used when sending message to target</param>
        public void AddTarget(Stream target) => AddTarget(target, AllLogTags, DEFAULT_FORMAT);
        public void AddTarget(Stream target, IEnumerable<LogTag> tags) => AddTarget(target, tags, DEFAULT_FORMAT);
        public void AddTarget(Stream target, string messageFormat) => AddTarget(target, AllLogTags, messageFormat);
        public void AddTarget(Stream target, IEnumerable<LogTag> tags, string messageFormat) {
            var set = new HashSet<LogTag>();
            foreach (var tag in tags) set.Add(tag);
            var nodes = ParseMessageFormat(messageFormat);
            targets[target] = (set, nodes);
        }


        public void AddFileTarget(string uri) => AddFileTarget(uri, AllLogTags, DEFAULT_FORMAT);
        public void AddFileTarget(string uri, IEnumerable<LogTag> tags) => AddFileTarget(uri, tags, DEFAULT_FORMAT);
        public void AddFileTarget(string uri, string messageFormat) => AddFileTarget(uri, AllLogTags, messageFormat);
        public void AddFileTarget(string uri, IEnumerable<LogTag> tags, string messageFormat) => AddTarget(File.OpenRead(uri), tags, messageFormat);


        public void AddConsoleTarget() => AddConsoleTarget(AllLogTags, DEFAULT_FORMAT);
        public void AddConsoleTarget(IEnumerable<LogTag> tags) => AddConsoleTarget(tags, DEFAULT_FORMAT);
        public void AddConsoleTarget(string messageFormat) => AddConsoleTarget(AllLogTags, messageFormat);
        public void AddConsoleTarget(IEnumerable<LogTag> tags, string messageFormat) => AddTarget(Console.OpenStandardOutput(), tags, messageFormat);


        public void Info(string message) => Log(LogTag.Info, message);
        public void Warning(string message) => Log(LogTag.Warning, message);
        public void Error(string message) => Log(LogTag.Error, message);
        public void Trace(string message) => Log(LogTag.Trace, message);
        public void Debug(string message) => Log(LogTag.Debug, message);


        public void Log(LogTag tag, string message) {
            var tags = new LogTag[] {tag};
            Log(tags, message);
        }


        public void Log(IList<LogTag> tags, string message) {
            foreach (var targetData in targets) {
                var renderInfo = targetData.Value;
                foreach (var tag in tags) {
                    if (renderInfo.Item1.Contains(tag)) {
                        Render(targetData.Key, renderInfo.Item2, message, tags);
                        break;
                    }
                }
            }
        }


        private static void Render(Stream stream, RenderNode[] nodes, string message, IList<LogTag> tags) {
            int bufferSize = (typeof(FileStream) == stream.GetType() ) ? 4096 : 1024;
            using (var writer = new StreamWriter(stream, Encoding.Default, bufferSize, true)) {
                foreach (var node in nodes) node.Render(writer, message, tags);
                writer.WriteLine();
            }
        }


        private static RenderNode[] ParseMessageFormat(string format) {
            var res = new List<RenderNode>();

            string buffer = "";
            bool readingSpecialCharacter = false;

            foreach (var letter in format) {
                if (readingSpecialCharacter) {
                    if (letter == SPECIAL_CHARACTER) {
                        buffer += SPECIAL_CHARACTER;
                    } else {
                        if (buffer.Length > 0) {
                            res.Add(new TextRenderNode(buffer));
                            buffer = "";
                        }
                        if (letter == TAGS_CHARACTER)
                            res.Add(new MetadataRenderNode(Metadata.Tag));
                        else if (letter == TIME_CHARACTER)
                            res.Add(new MetadataRenderNode(Metadata.Time));
                        else if (letter == DATE_CHARACTER)
                            res.Add(new MetadataRenderNode(Metadata.Date));
                        else if (letter == MESSAGE_CHARACTER)
                            res.Add(new MessageRenderNode());
                        else
                            throw new FormatException("Unexpected control character: " + letter);
                    }
                    readingSpecialCharacter = false;
                } else if (letter == SPECIAL_CHARACTER) {
                    readingSpecialCharacter = true;
                } else {
                    buffer += letter;
                }
            }
            if (buffer.Length > 0) res.Add(new TextRenderNode(buffer));

            return res.ToArray();
        }
    }
}
