using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace AnimationEditor
{
    public partial class Form1 : Form
    {

        public enum SelectedRect { None, renderRect, collisionRect, activeRect, anchorPt, weaponPt };

        SGP.CSGP_Direct3D DX = SGP.CSGP_Direct3D.GetInstance();
        SGP.CSGP_TextureManager TM = SGP.CSGP_TextureManager.GetInstance();
        Bitmap bit;
        SelectedRect sel = SelectedRect.None;
        SelectedRect selPt = SelectedRect.None;

        DateTime lastTime = DateTime.Now;
        float timeOnFrame = 0;
        bool isPlayin = false;

        AnimationEditor.AnimationSystem.AnimationSystem system = new AnimationEditor.AnimationSystem.AnimationSystem();
        int currSpriteSheet = -1;
        bool looping = true;

        public bool Looping
        {
            get { return looping; }
            set { looping = value; }
        }

        public Form1()
        {
            InitializeComponent();

            DX.Initialize(panel1, false);
            DX.AddRenderTarget(panel3);

            TM.Initialize(DX.Device, DX.Sprite);
        }

        public void Render()
        {
            //***Sprite Sheet Rect***//
            DX.Clear(panel1, Color.Gray);
            DX.DeviceBegin();
            DX.SpriteBegin();

            if (AnimationsList.SelectedIndex > -1)
            {
                Point offset = new Point(0, 0);
                offset.X += panel1.AutoScrollPosition.X; //need to account for scrollbar size
                offset.Y += panel1.AutoScrollPosition.Y;

                TM.Draw(currSpriteSheet, offset.X, offset.Y);

                PointF tempAnchor = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].anchorPoint;
                tempAnchor.X += offset.X; tempAnchor.Y += offset.Y;
                Rectangle tempRect = new Rectangle((int)tempAnchor.X - 4, (int)tempAnchor.Y - 4, 8, 8);
                DX.DrawRect(tempRect, Color.IndianRed);

                PointF tempWeapon = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].weaponPoint;
                tempWeapon.X += offset.X; tempWeapon.Y += offset.Y;
                tempRect = new Rectangle((int)tempWeapon.X - 4, (int)tempWeapon.Y - 4, 8, 8);
                DX.DrawRect(tempRect, Color.Gold);

                RectangleF tempRendRect = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect;
                tempRendRect.X += offset.X; tempRendRect.Y += offset.Y;
                DX.DrawHollowRect(new Rectangle((int)tempRendRect.X, (int)tempRendRect.Y, (int)tempRendRect.Width, (int)tempRendRect.Height),
                    Color.SpringGreen, 2);

                RectangleF tempCollRect = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect;
                tempCollRect.X += offset.X; tempCollRect.Y += offset.Y;
                DX.DrawHollowRect(new Rectangle((int)tempCollRect.X, (int)tempCollRect.Y, (int)tempCollRect.Width, (int)tempCollRect.Height),
                    Color.Blue, 2);

                RectangleF tempActRect = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect;
                tempActRect.X += offset.X; tempActRect.Y += offset.Y;
                DX.DrawHollowRect(new Rectangle((int)tempActRect.X, (int)tempActRect.Y, (int)tempActRect.Width, (int)tempActRect.Height),
                    Color.OrangeRed, 2);
            }

            DX.SpriteEnd();
            DX.DeviceEnd();
            DX.Present();
            //*****//

            //***Preview Rect***//
            DX.Clear(panel3, Color.Black);
            DX.DeviceBegin();
            DX.SpriteBegin();

            if (AnimationsList.SelectedIndex > -1)
            {
                PointF pos = new PointF(panel3.Width * 0.5f, panel3.Height * 0.75f);
                PointF anchorPt = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].anchorPoint;
                RectangleF renderRect = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect;
                PointF actAnchor = new PointF(anchorPt.X - renderRect.X, anchorPt.Y - renderRect.Y);

                RectangleF collRect = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect;
                Point actColl = new Point((int)collRect.X - (int)renderRect.X, (int)collRect.Y - (int)renderRect.Y);

                //
                pos.X -= actAnchor.X; pos.Y -= actAnchor.Y;
                actColl.X += (int)pos.X; actColl.Y += (int)pos.Y;

                TM.Draw(currSpriteSheet, (int)pos.X, (int)pos.Y, 1.0f, 1.0f,
                    new Rectangle((int)renderRect.X, (int)renderRect.Y, (int)renderRect.Width, (int)renderRect.Height));

                DX.DrawHollowRect(new Rectangle(actColl, new Size((int)collRect.Width, (int)collRect.Height)), Color.Blue, 1);
                actAnchor.X = actAnchor.X + pos.X; actAnchor.Y = actAnchor.Y + pos.Y;
                DX.DrawRect(new Rectangle((int)actAnchor.X - 4, (int)actAnchor.Y - 4, 8, 8),
                    Color.IndianRed);
            }

            DX.SpriteEnd();
            DX.DeviceEnd();
            DX.Present();
            //****//

            switch (sel)
            {
                case SelectedRect.renderRect:
                    RenderRect.ForeColor = Color.Red;
                    ColRect.ForeColor = Color.Black;
                    ActRect.ForeColor = Color.Black;
                    break;
                case SelectedRect.collisionRect:
                    RenderRect.ForeColor = Color.Black;
                    ColRect.ForeColor = Color.Red;
                    ActRect.ForeColor = Color.Black;
                    break;
                case SelectedRect.activeRect:
                    RenderRect.ForeColor = Color.Black;
                    ColRect.ForeColor = Color.Black;
                    ActRect.ForeColor = Color.Red;
                    break;
                case SelectedRect.None:
                    RenderRect.ForeColor = Color.Black;
                    ColRect.ForeColor = Color.Black;
                    ActRect.ForeColor = Color.Black;
                    break;
            }

            if (selPt == SelectedRect.anchorPt)
            {
                Anchor.ForeColor = Color.Red;
                Weapon.ForeColor = Color.Black;
            }
            else if (selPt == SelectedRect.weaponPt)
            {
                Anchor.ForeColor = Color.Black;
                Weapon.ForeColor = Color.Red;
            }
            else
            {
                Anchor.ForeColor = Color.Black;
                Weapon.ForeColor = Color.Black;
            }

        }

        public new void Update()
        {
            if (AnimationsList.SelectedIndex > -1 && isPlayin)
            {
                DateTime currTime = DateTime.Now;
                double deltaTime = (currTime - lastTime).TotalSeconds;
                timeOnFrame += (float)(deltaTime * (double)PlaySpeed.Value);
                lastTime = currTime;
                if (timeOnFrame > system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].duration)
                {
                    timeOnFrame = 0;
                    if (FrameList.SelectedIndex >= FrameList.Items.Count - 1)
                    {

                        if (isLoop.Checked)
                        {
                            FrameList.SelectedIndex = 0;
                        }
                        else
                        {
                            isPlayin = false;
                        }
                    }
                    else
                        FrameList.SelectedIndex += 1;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Looping = false;
        }

        private void AnimationsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                FrameList.Items.Clear();
                for (int x = 0; x < system.animations[AnimationsList.SelectedItem.ToString()].frameList.Count; x++)
                    FrameList.Items.Add(x);

                currSpriteSheet = system.animations[AnimationsList.SelectedItem.ToString()].imgID;
                RenameBox.Text = AnimationsList.SelectedItem.ToString();
                isLoop.Checked = system.animations[AnimationsList.SelectedItem.ToString()].isLooping;
                FrameList.SelectedIndex = 0;
            }
        }

        private void RenameButton_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                string tempIndex = AnimationsList.SelectedItem.ToString();
                AnimationEditor.AnimationSystem.Animation tempAnim = system.animations[tempIndex];
                tempAnim.aniName = RenameBox.Text;

                system.animations.Remove(tempIndex);
                system.animations.Add(RenameBox.Text, tempAnim);
                AnimationsList.Items[AnimationsList.SelectedIndex] = RenameBox.Text;
            }
        }

        private void isLoop_CheckedChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                system.animations[AnimationsList.SelectedItem.ToString()].isLooping = isLoop.Checked;
            }
        }

        private void NewAnim_Click(object sender, EventArgs e)
        {

            if (AnimationsList.SelectedIndex > -1)
            {
                AnimationEditor.AnimationSystem.Animation newAnim =
                             new AnimationEditor.AnimationSystem.Animation();
                newAnim.aniImg = system.animations[AnimationsList.SelectedItem.ToString()].aniImg;
                AnimationEditor.AnimationSystem.Frame tempFrame =
                    new AnimationEditor.AnimationSystem.Frame();

                newAnim.frameList.Add(tempFrame);
                int count = system.animations.Count + 1;
                newAnim.imgID = currSpriteSheet;
                newAnim.aniName = count.ToString();
                newAnim.isLooping = true;
                system.animations.Add(newAnim.aniName, newAnim);

                AnimationsList.Items.Add(newAnim.aniName);

                FrameList.Items.Clear();
                int frameName = 0;
                FrameList.Items.Add(frameName);

                AnimationsList.SelectedIndex = AnimationsList.Items.Count - 1;
                FrameList.SelectedIndex = 0;
                isLoop.Checked = newAnim.isLooping;
            }
        }

        private void DelAnim_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                if (AnimationsList.Items.Count == 1)
                {
                    //call new function
                    newToolStripMenuItem_Click(sender, e);
                }
                else
                {
                    system.animations.Remove(AnimationsList.SelectedItem.ToString());
                    int index = AnimationsList.SelectedIndex;
                    AnimationsList.Items.RemoveAt(index);
                    AnimationsList.SelectedIndex = index - 1;
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                TM.UnloadTexture(currSpriteSheet);
                currSpriteSheet = -1;
                for (int x = 0; x < AnimationsList.Items.Count; x++)
                    system.animations.Remove(AnimationsList.Items[x].ToString());
                AnimationsList.Items.Clear();
                FrameList.Items.Clear();
                isLoop.Checked = false;

                sel = SelectedRect.None;
                selPt = SelectedRect.None;
            }
        }

        private void DurationNum_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].duration = (float)DurationNum.Value;
            }
        }

        private void newFrame_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                system.animations[AnimationsList.SelectedItem.ToString()].frameList.Add(new AnimationEditor.AnimationSystem.Frame());
                FrameList.Items.Add(FrameList.Items.Count);
                FrameList.SelectedIndex = FrameList.Items.Count - 1;
                sel = SelectedRect.renderRect;
                selPt = SelectedRect.anchorPt;
            }
        }

        private void FrameList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                UpdateFrameData();
            }
        }

        private void DelFrame_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                int index = FrameList.SelectedIndex;
                if (FrameList.Items.Count == 1)
                {
                    AnimationEditor.AnimationSystem.Frame tempFrame = new AnimationEditor.AnimationSystem.Frame();
                    system.animations[AnimationsList.SelectedItem.ToString()].frameList.Clear();
                    system.animations[AnimationsList.SelectedItem.ToString()].frameList.Add(tempFrame);
                }
                else
                {
                    system.animations[AnimationsList.SelectedItem.ToString()].frameList.RemoveAt(index);
                    FrameList.Items.RemoveAt(index);
                }
            }
        }

        public void UpdateFrameData()
        {
            if (FrameList.SelectedIndex == -1)
                FrameList.SelectedIndex = FrameList.Items.Count -1;

            DurationNum.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].duration;

            RendX.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect.X;
            RendY.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect.Y;
            RendWidth.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect.Width;
            RendHeight.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect.Height;

            ColX.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect.X;
            ColY.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect.Y;
            ColWidth.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect.Width;
            ColHeight.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect.Height;

            ActX.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect.X;
            ActY.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect.Y;
            ActWidth.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect.Width;
            ActHeight.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect.Height;

            AnchorX.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].anchorPoint.X;
            anchorY.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].anchorPoint.Y;

            WeaponX.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].weaponPoint.X;
            WeaponY.Value = (decimal)system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].weaponPoint.Y;

            EventMess.Text = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].eventMess;
        }

        private void RendX_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect.X = (float)RendX.Value;
        }

        private void RendY_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect.Y = (float)RendY.Value;
        }

        private void RendWidth_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect.Width = (float)RendWidth.Value;
        }

        private void RendHeight_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect.Height = (float)RendHeight.Value;
        }

        private void ColX_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect.X
                    = (float)ColX.Value;
        }

        private void ColY_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect.Y
                    = (float)ColY.Value;
        }

        private void ColWidth_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect.Width
                    = (float)ColWidth.Value;
        }

        private void ColHeight_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect.Height
                    = (float)ColHeight.Value;
        }

        private void ActX_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect.X
                    = (float)ActX.Value;
        }

        private void ActY_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect.Y
                    = (float)ActY.Value;
        }

        private void ActWidth_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect.Width
                    = (float)ActWidth.Value;
        }

        private void ActHeight_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect.Height
                    = (float)ActHeight.Value;
        }

        private void AnchorX_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].anchorPoint.X
                    = (float)AnchorX.Value;
        }

        private void anchorY_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].anchorPoint.Y
                    = (float)anchorY.Value;
        }

        private void WeaponX_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].weaponPoint.X
                    = (float)WeaponX.Value;
        }

        private void WeaponY_ValueChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].weaponPoint.Y
                    = (float)WeaponY.Value;
        }

        private void EventMess_TextChanged(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].eventMess
                    = EventMess.Text;
        }

        private void RenderRect_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                sel = SelectedRect.renderRect;
        }

        private void ColRect_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                sel = SelectedRect.collisionRect;
        }

        private void ActRect_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                sel = SelectedRect.activeRect;
        }

        private void Anchor_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                selPt = SelectedRect.anchorPt;
        }

        private void Weapon_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
                selPt = SelectedRect.weaponPt;
        }

        private void panel1_MouseEnter(object sender, EventArgs e)
        {
            panel1.Focus();
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    Point offset = new Point(0, 0);
                    offset.X -= panel1.AutoScrollPosition.X;
                    offset.Y -= panel1.AutoScrollPosition.Y;

                    if (selPt == SelectedRect.anchorPt)
                    {
                        system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].anchorPoint
                            = new PointF(e.X + offset.X, e.Y + offset.Y);
                        UpdateFrameData();
                    }
                    else if (selPt == SelectedRect.weaponPt)
                    {
                        system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].weaponPoint
                            = new PointF(e.X + offset.X, e.Y + offset.Y);
                        UpdateFrameData();
                    }
                }
            }
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    if (sel == SelectedRect.renderRect)
                    {
                        PointF startPt = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect.Location;
                        float right = e.X - startPt.X, bottom = e.Y - startPt.Y;
                        right -= panel1.AutoScrollPosition.X;
                        bottom -= panel1.AutoScrollPosition.Y;
                        if (right < 0) right = 0;
                        if (bottom < 0) bottom = 0;
                        RectangleF newRendRect = new RectangleF(startPt.X, startPt.Y, right, bottom);
                        system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect = newRendRect;
                        UpdateFrameData();
                    }
                    else if (sel == SelectedRect.collisionRect)
                    {
                        PointF startPt = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect.Location;
                        float right = e.X - startPt.X, bottom = e.Y - startPt.Y;
                        right -= panel1.AutoScrollPosition.X;
                        bottom -= panel1.AutoScrollPosition.Y;
                        if (right < 0) right = 0;
                        if (bottom < 0) bottom = 0;
                        RectangleF newCollRect = new RectangleF(startPt.X, startPt.Y, right, bottom);
                        system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect = newCollRect;
                        UpdateFrameData();
                    }
                    else if (sel == SelectedRect.activeRect)
                    {
                        PointF startPt = system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect.Location;
                        float right = e.X - startPt.X, bottom = e.Y - startPt.Y;
                        right -= panel1.AutoScrollPosition.X;
                        bottom -= panel1.AutoScrollPosition.Y;
                        if (right < 0) right = 0;
                        if (bottom < 0) bottom = 0;
                        RectangleF newActRect = new RectangleF(startPt.X, startPt.Y, right, bottom);
                        system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect = newActRect;
                        UpdateFrameData();
                    }
                }
            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK && dlg.FileName.Contains(".xml"))
            {
                newToolStripMenuItem_Click(sender, e);
                StreamReader stream = new StreamReader(dlg.FileName);
                XmlReader read = XmlReader.Create(stream);

                while (read.ReadToFollowing("Animation"))
                {
                    int temp = 0;
                    bool loopin = true;

                    read.MoveToFirstAttribute();
                    temp = int.Parse(read.Value);
                    loopin = (temp == 1);

                    read.ReadToFollowing("ImgPath");
                    read.MoveToFirstAttribute();
                    string imgPath = read.ReadElementContentAsString();

                    read.ReadToFollowing("AniName");
                    read.MoveToFirstAttribute();
                    string aniName = read.ReadElementContentAsString();

                    AnimationEditor.AnimationSystem.Animation anim = new AnimationEditor.AnimationSystem.Animation();
                    anim.aniName = aniName;
                    anim.aniImg = imgPath;
                    anim.isLooping = loopin;
                    List<AnimationEditor.AnimationSystem.Frame> frames = new List<AnimationSystem.Frame>();
                    while (read.IsStartElement("Frame"))
                    {
                        read.MoveToFirstAttribute();
                        float frameDur = float.Parse(read.Value);

                        read.ReadToFollowing("Event");
                        string eventName = read.ReadElementContentAsString();
                        read.ReadToFollowing("rendRect");

                        read.MoveToNextAttribute();
                        int x = int.Parse(read.Value);
                        read.MoveToNextAttribute();
                        int y = int.Parse(read.Value);
                        read.MoveToNextAttribute();
                        int wid = int.Parse(read.Value);
                        read.MoveToNextAttribute();
                        int hei = int.Parse(read.Value);

                        RectangleF rendRect = new RectangleF(x, y, wid, hei);

                        read.ReadToFollowing("collRect");

                        read.MoveToNextAttribute();
                        x = int.Parse(read.Value) + (int)rendRect.X;
                        read.MoveToNextAttribute();
                        y = int.Parse(read.Value) + (int)rendRect.Y;
                        read.MoveToNextAttribute();
                        wid = int.Parse(read.Value);
                        read.MoveToNextAttribute();
                        hei = int.Parse(read.Value);

                        RectangleF collRect = new RectangleF(x, y, wid, hei);

                        read.ReadToFollowing("actRect");

                        read.MoveToNextAttribute();
                        x = int.Parse(read.Value) + (int)rendRect.X;
                        read.MoveToNextAttribute();
                        y = int.Parse(read.Value) + (int)rendRect.Y;
                        read.MoveToNextAttribute();
                        wid = int.Parse(read.Value);
                        read.MoveToNextAttribute();
                        hei = int.Parse(read.Value);

                        RectangleF actRect = new RectangleF(x, y, wid, hei);

                        read.ReadToFollowing("anchor");

                        read.MoveToNextAttribute();
                        x = int.Parse(read.Value) + (int)rendRect.X;
                        read.MoveToNextAttribute();
                        y = int.Parse(read.Value) + (int)rendRect.Y;

                        PointF anchor = new PointF(x, y);

                        read.ReadToFollowing("weaponPoint");

                        read.MoveToNextAttribute();
                        x = int.Parse(read.Value) + (int)rendRect.X;
                        read.MoveToNextAttribute();
                        y = int.Parse(read.Value) + (int)rendRect.Y;

                        PointF weapon = new PointF(x, y);

                        AnimationEditor.AnimationSystem.Frame newFrame = new AnimationEditor.AnimationSystem.Frame();
                        newFrame.rendRect = rendRect;
                        newFrame.collRect = collRect;
                        newFrame.actRect = actRect;
                        newFrame.anchorPoint = anchor;
                        newFrame.weaponPoint = weapon;
                        newFrame.duration = frameDur;
                        newFrame.eventMess = eventName;

                        frames.Add(newFrame);

                        read.ReadToNextSibling("Frame");
                        read.ReadToNextSibling("Frame");
                    }
                    anim.frameList = frames;
                    anim.imgID = TM.LoadTexture("resource//" + imgPath); //have check if can't fine image

                    bit = new Bitmap("resource//" + imgPath);
                    Graphics g = panel1.CreateGraphics();
                    bit.SetResolution(g.DpiX, g.DpiY);
                    g.Dispose();
                    panel1.AutoScrollMinSize = bit.Size;

                    system.animations.Add(aniName, anim);

                    AnimationsList.Items.Add(anim.aniName);
                    AnimationsList.SelectedIndex = AnimationsList.Items.Count - 1;
                    UpdateFrameData();

                    read.MoveToElement();
                }

                sel = SelectedRect.renderRect;
                selPt = SelectedRect.anchorPt;
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    if (sel == SelectedRect.renderRect)
                    {
                        PointF startPoint = new PointF(e.X, e.Y);
                        startPoint.X -= panel1.AutoScrollPosition.X;
                        startPoint.Y -= panel1.AutoScrollPosition.Y;
                        system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].rendRect.Location = startPoint;
                    }
                    else if (sel == SelectedRect.collisionRect)
                    {
                        PointF startPoint = new PointF(e.X, e.Y);
                        startPoint.X -= panel1.AutoScrollPosition.X;
                        startPoint.Y -= panel1.AutoScrollPosition.Y;
                        system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].collRect.Location = startPoint;
                    }
                    else if (sel == SelectedRect.activeRect)
                    {
                        PointF startPoint = new PointF(e.X, e.Y);
                        startPoint.X -= panel1.AutoScrollPosition.X;
                        startPoint.Y -= panel1.AutoScrollPosition.Y;
                        system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex].actRect.Location = startPoint;
                    }
                }
            }
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            DX.Resize(panel1, false);
        }

        private void Play_Click(object sender, EventArgs e)
        {
            isPlayin = !isPlayin;
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            isPlayin = false;
            FrameList.SelectedIndex = 0;
        }

        private void loadSpriteSheetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "png files (*.png)|*.png|All files (*.*)|*.*";
            if (DialogResult.OK == dlg.ShowDialog() && dlg.FileName.Contains(".png"))
            {
                string path = dlg.FileName;
                int ind = path.LastIndexOf('\\');
                path = path.Substring(ind + 1);
                bit = new Bitmap(dlg.FileName);
                Graphics g = panel1.CreateGraphics();
                bit.SetResolution(g.DpiX, g.DpiY);
                g.Dispose();

                AnimationEditor.AnimationSystem.Animation newAnim =
                     new AnimationEditor.AnimationSystem.Animation();
                newAnim.aniImg = path;
                AnimationEditor.AnimationSystem.Frame newFrame =
                    new AnimationEditor.AnimationSystem.Frame();

                currSpriteSheet = TM.LoadTexture("resource\\" + newAnim.aniImg);
                panel1.AutoScrollMinSize = bit.Size;

                newAnim.frameList.Add(newFrame);
                int count = system.animations.Count + 1;
                newAnim.imgID = currSpriteSheet;
                newAnim.aniName = count.ToString();
                newAnim.isLooping = true;
                system.animations.Add(newAnim.aniName, newAnim);

                AnimationsList.Items.Add(newAnim.aniName);

                FrameList.Items.Clear();
                int frameName = 0;
                FrameList.Items.Add(frameName);

                AnimationsList.SelectedIndex = AnimationsList.Items.Count - 1;
                FrameList.SelectedIndex = 0;
                isLoop.Checked = newAnim.isLooping;

                sel = SelectedRect.renderRect;
                selPt = SelectedRect.anchorPt;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Looping = false;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            save.AddExtension = true;
            save.DefaultExt = ".xml";

            if (save.ShowDialog() == DialogResult.OK)
            {
                XElement root = new XElement("Animation_List");

                for (int x = 0; x < AnimationsList.Items.Count; x++)
                {
                    AnimationEditor.AnimationSystem.Animation tempAni = system.animations[AnimationsList.Items[x].ToString()];
                    XElement aniEle = new XElement("Animation");
                    int temp = 1;
                    if (tempAni.isLooping == false)
                        temp = 0;
                    XAttribute loopAtt = new XAttribute("loop", temp);
                    aniEle.Add(loopAtt);
                    XElement imgEle = new XElement("ImgPath", tempAni.aniImg);
                    aniEle.Add(imgEle);
                    XElement nameEle = new XElement("AniName", tempAni.aniName);
                    aniEle.Add(nameEle);

                    for (int y = 0; y < tempAni.frameList.Count; y++)
                    {
                        XElement frameEle = new XElement("Frame");
                        XAttribute durAtt = new XAttribute("duration", tempAni.frameList[y].duration);
                        frameEle.Add(durAtt);

                        XElement eveEle = new XElement("Event", tempAni.frameList[y].eventMess);
                        
                        RectangleF tempRRect = tempAni.frameList[y].rendRect;
                        XElement rendEle = new XElement("rendRect");
                        rendEle.Add(new XAttribute("x", tempRRect.X));
                        rendEle.Add(new XAttribute("y", tempRRect.Y));
                        rendEle.Add(new XAttribute("width", tempRRect.Width));
                        rendEle.Add(new XAttribute("heigth", tempRRect.Height));

                        RectangleF tempCRect = tempAni.frameList[y].collRect;
                        tempCRect.X -= tempRRect.X;
                        tempCRect.Y -= tempRRect.Y;
                        XElement collEle = new XElement("collRect");
                        collEle.Add(new XAttribute("x", tempCRect.X));
                        collEle.Add(new XAttribute("y", tempCRect.Y));
                        collEle.Add(new XAttribute("width", tempCRect.Width));
                        collEle.Add(new XAttribute("heigth", tempCRect.Height));

                        RectangleF tempARect = tempAni.frameList[y].actRect;
                        tempARect.X -= tempRRect.X;
                        tempARect.Y -= tempRRect.Y;
                        XElement actEle = new XElement("actRect");
                        actEle.Add(new XAttribute("x", tempARect.X));
                        actEle.Add(new XAttribute("y", tempARect.Y));
                        actEle.Add(new XAttribute("width", tempARect.Width));
                        actEle.Add(new XAttribute("heigth", tempARect.Height));

                        PointF tempAPt = tempAni.frameList[y].anchorPoint;
                        tempAPt.X -= tempRRect.X;
                        tempAPt.Y -= tempRRect.Y;
                        XElement ancEle = new XElement("anchor");
                        ancEle.Add(new XAttribute("x", tempAPt.X));
                        ancEle.Add(new XAttribute("y", tempAPt.Y));

                        PointF tempWPt = tempAni.frameList[y].weaponPoint;
                        tempWPt.X -= tempRRect.X;
                        tempWPt.Y -= tempRRect.Y;
                        XElement weaEle = new XElement("weaponPoint");
                        weaEle.Add(new XAttribute("x", tempWPt.X));
                        weaEle.Add(new XAttribute("y", tempWPt.Y));

                        frameEle.Add(eveEle);
                        frameEle.Add(rendEle);
                        frameEle.Add(collEle);
                        frameEle.Add(actEle);
                        frameEle.Add(ancEle);
                        frameEle.Add(weaEle);

                        aniEle.Add(frameEle);
                    }
                    root.Add(aniEle);
                }

                root.Save(save.FileName);
            }
        }

        private void Duplicate_Click(object sender, EventArgs e)
        {
            if (AnimationsList.SelectedIndex > -1)
            {
                AnimationEditor.AnimationSystem.Frame tempFrame = new AnimationEditor.AnimationSystem.Frame(system.animations[AnimationsList.SelectedItem.ToString()].frameList[FrameList.SelectedIndex]);
                system.animations[AnimationsList.SelectedItem.ToString()].frameList.Add(tempFrame);
                FrameList.Items.Add(FrameList.Items.Count);
                FrameList.SelectedIndex = FrameList.Items.Count - 1;
                sel = SelectedRect.renderRect;
                selPt = SelectedRect.anchorPt;
            }
        }

        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files =  (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files[0].Contains(".png"))
            {
                string path;
                int ind = files[0].LastIndexOf('\\');
                path = files[0].Substring(ind + 1);
                bit = new Bitmap(files[0]);
                Graphics g = panel1.CreateGraphics();
                bit.SetResolution(g.DpiX, g.DpiY);
                g.Dispose();

                AnimationEditor.AnimationSystem.Animation newAnim =
                         new AnimationEditor.AnimationSystem.Animation();
                newAnim.aniImg = path;
                AnimationEditor.AnimationSystem.Frame newFrame =
                    new AnimationEditor.AnimationSystem.Frame();

                currSpriteSheet = TM.LoadTexture("resource\\" + newAnim.aniImg);
                panel1.AutoScrollMinSize = bit.Size;

                newAnim.frameList.Add(newFrame);
                int count = system.animations.Count + 1;
                newAnim.imgID = currSpriteSheet;
                newAnim.aniName = count.ToString();
                newAnim.isLooping = true;
                system.animations.Add(newAnim.aniName, newAnim);

                AnimationsList.Items.Add(newAnim.aniName);

                FrameList.Items.Clear();
                int frameName = 0;
                FrameList.Items.Add(frameName);

                AnimationsList.SelectedIndex = AnimationsList.Items.Count - 1;
                FrameList.SelectedIndex = 0;
                isLoop.Checked = newAnim.isLooping;

                sel = SelectedRect.renderRect;
                selPt = SelectedRect.anchorPt; 
            }
            else if (files[0].Contains(".xml"))
            {
                newToolStripMenuItem_Click(sender, e);
                StreamReader stream = new StreamReader(files[0]);
                XmlReader read = XmlReader.Create(stream);

                while (read.ReadToFollowing("Animation"))
                {
                    int temp = 0;
                    bool loopin = true;

                    read.MoveToFirstAttribute();
                    temp = int.Parse(read.Value);
                    loopin = (temp == 1);

                    read.ReadToFollowing("ImgPath");
                    read.MoveToFirstAttribute();
                    string imgPath = read.ReadElementContentAsString();

                    read.ReadToFollowing("AniName");
                    read.MoveToFirstAttribute();
                    string aniName = read.ReadElementContentAsString();

                    AnimationEditor.AnimationSystem.Animation anim = new AnimationEditor.AnimationSystem.Animation();
                    anim.aniName = aniName;
                    anim.aniImg = imgPath;
                    anim.isLooping = loopin;
                    List<AnimationEditor.AnimationSystem.Frame> frames = new List<AnimationSystem.Frame>();
                    while (read.IsStartElement("Frame"))
                    {
                        read.MoveToFirstAttribute();
                        float frameDur = float.Parse(read.Value);

                        read.ReadToFollowing("Event");
                        string eventName = read.ReadElementContentAsString();
                        read.ReadToFollowing("rendRect");

                        read.MoveToNextAttribute();
                        int x = int.Parse(read.Value);
                        read.MoveToNextAttribute();
                        int y = int.Parse(read.Value);
                        read.MoveToNextAttribute();
                        int wid = int.Parse(read.Value);
                        read.MoveToNextAttribute();
                        int hei = int.Parse(read.Value);

                        RectangleF rendRect = new RectangleF(x, y, wid, hei);

                        read.ReadToFollowing("collRect");

                        read.MoveToNextAttribute();
                        x = int.Parse(read.Value) + (int)rendRect.X;
                        read.MoveToNextAttribute();
                        y = int.Parse(read.Value) + (int)rendRect.Y;
                        read.MoveToNextAttribute();
                        wid = int.Parse(read.Value);
                        read.MoveToNextAttribute();
                        hei = int.Parse(read.Value);

                        RectangleF collRect = new RectangleF(x, y, wid, hei);

                        read.ReadToFollowing("actRect");

                        read.MoveToNextAttribute();
                        x = int.Parse(read.Value) + (int)rendRect.X;
                        read.MoveToNextAttribute();
                        y = int.Parse(read.Value) + (int)rendRect.Y;
                        read.MoveToNextAttribute();
                        wid = int.Parse(read.Value);
                        read.MoveToNextAttribute();
                        hei = int.Parse(read.Value);

                        RectangleF actRect = new RectangleF(x, y, wid, hei);

                        read.ReadToFollowing("anchor");

                        read.MoveToNextAttribute();
                        x = int.Parse(read.Value) + (int)rendRect.X;
                        read.MoveToNextAttribute();
                        y = int.Parse(read.Value) + (int)rendRect.Y;

                        PointF anchor = new PointF(x, y);

                        read.ReadToFollowing("weaponPoint");

                        read.MoveToNextAttribute();
                        x = int.Parse(read.Value) + (int)rendRect.X;
                        read.MoveToNextAttribute();
                        y = int.Parse(read.Value) + (int)rendRect.Y;

                        PointF weapon = new PointF(x, y);

                        AnimationEditor.AnimationSystem.Frame newFrame = new AnimationEditor.AnimationSystem.Frame();
                        newFrame.rendRect = rendRect;
                        newFrame.collRect = collRect;
                        newFrame.actRect = actRect;
                        newFrame.anchorPoint = anchor;
                        newFrame.weaponPoint = weapon;
                        newFrame.duration = frameDur;
                        newFrame.eventMess = eventName;

                        frames.Add(newFrame);

                        read.ReadToNextSibling("Frame");
                        read.ReadToNextSibling("Frame");
                    }
                    anim.frameList = frames;
                    anim.imgID = TM.LoadTexture("resource//" + imgPath); //have check if can't fine image

                    bit = new Bitmap("resource//" + imgPath);
                    Graphics g = panel1.CreateGraphics();
                    bit.SetResolution(g.DpiX, g.DpiY);
                    g.Dispose();
                    panel1.AutoScrollMinSize = bit.Size;

                    system.animations.Add(aniName, anim);

                    AnimationsList.Items.Add(anim.aniName);
                    AnimationsList.SelectedIndex = AnimationsList.Items.Count - 1;
                    UpdateFrameData();

                    read.MoveToElement();
                }

                sel = SelectedRect.renderRect;
                selPt = SelectedRect.anchorPt;
            }
        }

        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
    }
}
