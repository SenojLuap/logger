using System;
using System.IO;
using System.Collections.Generic;

using static paujo.Debug.Logger;

namespace paujo.Debug {

    internal abstract class RenderNode {
        public abstract void Render(StreamWriter target, string message, IList<LogTag> tags);
    }


    internal class TextRenderNode : RenderNode {

        /// <summary>
        /// The text to render to the target
        /// </summary>
        public string Text { get; set; }

        public TextRenderNode(string text) { Text = text; }

        public override void Render(StreamWriter target, string message, IList<LogTag> tags) {
            target.Write(Text);
        }
    }


    internal class MessageRenderNode : RenderNode {

        public override void Render(StreamWriter target, string message, IList<LogTag> tags) {
            target.Write(message);
        }
    }


    public enum Metadata { Time, Tag, Date }


    internal class MetadataRenderNode : RenderNode {
    
        public Metadata Metadata { get; set; }

        public MetadataRenderNode(Metadata metadata) { Metadata = metadata; }

        public override void Render(StreamWriter target, string message, IList<LogTag> tags) {
            switch (Metadata) {
                case Metadata.Time:
                    target.Write(DateTime.Now.ToString("HH:mm:ss"));
                    break;
                case Metadata.Tag:
                    for (int i = 0; i < tags.Count; i++) {
                        target.Write(tags[i].ToString().ToUpper());
                        if (i < (tags.Count-1))
                            target.Write("/");
                    }
                    break;
                case Metadata.Date:
                    target.Write(DateTime.Now.ToString("d/M/yyyy"));
                    break;
            }
        }
    }
}