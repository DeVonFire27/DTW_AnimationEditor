using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AnimationEditor.AnimationSystem
{
    class Animation
    {
        public string aniImg;
        public int imgID;
        public string aniName = "animation";
        public bool isLooping = true;

        public List<Frame> frameList = new List<Frame>();
    }
}
