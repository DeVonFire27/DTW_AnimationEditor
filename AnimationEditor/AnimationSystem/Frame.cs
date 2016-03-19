using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace AnimationEditor.AnimationSystem
{
    class Frame
    {
        public float duration = 0.1f;
        public RectangleF rendRect = new RectangleF { };
        public RectangleF collRect = new RectangleF { };
        public RectangleF actRect = new RectangleF { };

        public PointF anchorPoint = new PointF { };
        public PointF weaponPoint = new PointF { };

        public string eventMess = "none";

        public Frame() { }

        public Frame(Frame cpy)
        {
            Frame tempFrame = new Frame();
            tempFrame.rendRect = cpy.rendRect;
            tempFrame.collRect = cpy.collRect;
            tempFrame.actRect = cpy.actRect;
            tempFrame.anchorPoint = cpy.anchorPoint;
            tempFrame.weaponPoint = cpy.weaponPoint;
            tempFrame.duration = cpy.duration;

            tempFrame.eventMess = cpy.eventMess;
        }

    }
}
