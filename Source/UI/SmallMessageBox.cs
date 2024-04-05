using UnityEngine;
using Verse;

namespace ProgressRenderer
{
    public class SmallMessageBox : Window
    {
        private readonly string _text;

        public SmallMessageBox(string text)
        {
            _text = text;
            closeOnAccept = false;
            closeOnCancel = false;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(240f, 75f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, _text);
            Text.Anchor = backupAnchor;
        }

        public void Close()
        {
            Close(false);
        }
    }
}
