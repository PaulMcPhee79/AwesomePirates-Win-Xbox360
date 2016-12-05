using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class DashDial : Prop
    {
        private const int kTopFontSize = 22;
        private const int kMidFontSize = 20;
        private const int kBtmFontSize = 14;
        private const float kTextfieldWidth = 120.0f;
        private const uint kFontColor = 0xbeecff;
        
        public DashDial()
            : base(-1)
        {
            mCanvas = null;
            mFlipCanvas = null;
            SetupProp();
        }

        #region Fields
        private SPTextField mTopText;
        private SPTextField mMidText;
        private SPTextField mBtmText;
        private SPSprite mCanvas;
        private SPSprite mFlipCanvas;
        #endregion

        #region Properties
        public int TopFontSize { get { return mTopText.FontSize; } }
        public int MidFontSize { get { return mMidText.FontSize; } }
        public int BtmFontSize { get { return mBtmText.FontSize; } }

        public string TopFontName { get { return mTopText.FontName; } }
        public string MidFontName { get { return mMidText.FontName; } }
        public string BtmFontName { get { return mBtmText.FontName; } }

        public Color TopTextColor { get { return mTopText.Color; } set { mTopText.Color = value; } }
        public Color MidTextColor { get { return mMidText.Color; } set { mMidText.Color = value; } }
        public Color BtmTextColor { get { return mBtmText.Color; } set { mBtmText.Color = value; } }
        public static Color FontColor { get { return SPUtils.ColorFromColor(kFontColor); } }
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            if (mFlipCanvas != null)
                return;
    
            mFlipCanvas = new SPSprite();
            AddChild(mFlipCanvas);
    
            mCanvas = new SPSprite();
            mFlipCanvas.AddChild(mCanvas);
    
	        // Build dial from quarters
	        float scaleX = 1.0f, scaleY = 1.0f;
	        SPTexture dialTexture = mScene.TextureByName("speedometer");
	
	        for (int i = 0; i < 4; ++i)
            {
                AddDialSectionWithTexture(dialTexture, scaleX, scaleY);
		
		        switch (i)
                {
			        case 0: scaleX = -1.0f; break;
			        case 1: scaleY = -1.0f; break;
			        case 2: scaleX = 1.0f; break;
			        default: break;
		        }
	        }
	
	        // Top Textfield
	        mTopText = new SPTextField(kTextfieldWidth, kTopFontSize + 1, "", mScene.FontKey, kTopFontSize);
            mTopText.Color = DashDial.FontColor;
	        mTopText.X = 24.0f;
	        mTopText.Y = 20.0f;
	        mTopText.HAlign = SPTextField.SPHAlign.Center;
	        mTopText.VAlign = SPTextField.SPVAlign.Center;
            mCanvas.AddChild(mTopText);
	
	        // Mid Textfield
	        mMidText = new SPTextField(kTextfieldWidth, kMidFontSize+1, "", mScene.FontKey, kMidFontSize);
            mMidText.Color = DashDial.FontColor;
	        mMidText.X = 24.0f;
	        mMidText.Y = 52.0f;
	        mMidText.HAlign = SPTextField.SPHAlign.Center;
	        mMidText.VAlign = SPTextField.SPVAlign.Center;
            mCanvas.AddChild(mMidText);
	
	        // Btm Textfield
	        mBtmText = new SPTextField(kTextfieldWidth, kBtmFontSize+1, "", mScene.FontKey, kBtmFontSize);
            mBtmText.Color = DashDial.FontColor;
	        mBtmText.X = 24.0f;
	        mBtmText.Y = 82.0f;
	        mBtmText.HAlign = SPTextField.SPHAlign.Center;
	        mBtmText.VAlign = SPTextField.SPVAlign.Center;
            mCanvas.AddChild(mBtmText);
    
            mCanvas.X = -mCanvas.Width / 2;
            X += mCanvas.Width / 2;
        }

        private void AddDialSectionWithTexture(SPTexture texture, float scaleX, float scaleY)
        {
            SPImage image = new SPImage(texture);
	        image.ScaleX = scaleX;
	        image.ScaleY = scaleY;
	
	        if (scaleX < 0)
		        image.X = 2 * image.Width-2;
	        if (scaleY < 0)
		        image.Y = 2 * image.Height-2;
            mCanvas.AddChild(image);
        }

        public void LoadFromDictionary(Dictionary<string, object> dictionary, List<string> keys)
        {
            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.LowerLeft);
	        X = ResManager.RESX(2 * Globals.ConvertToSingle(dictionary["x"]));
	        Y = ResManager.RESY(2 * Globals.ConvertToSingle(dictionary["y"]));
            ResManager.RESM.PopOffset();
    
            X += mCanvas.Width / 2;
    
	        dictionary = dictionary["Textfields"] as Dictionary<string, object>;
	        mTopText.Text = dictionary["topText"] as string;
	        mTopText.Visible = mTopText.Text != null;
            mMidText.Text = dictionary["midText"] as string;
	        mMidText.Visible = mMidText.Text != null;
            mBtmText.Text = dictionary["btmText"] as string;
	        mBtmText.Visible = mBtmText.Text != null;
        }

        public void SetTopText(string text)
        {
            mTopText.Text = Locale.SanitizeText(text, mTopText.FontName, mTopText.FontSize);
        }

        public void SetMidText(string text)
        {
            mMidText.Text = Locale.SanitizeText(text, mMidText.FontName, mMidText.FontSize);
        }

        public void SetBtmText(string text)
        {
            mBtmText.Text = Locale.SanitizeText(text, mBtmText.FontName, mBtmText.FontSize);
        }

        public override void Flip(bool enable)
        {
            mFlipCanvas.ScaleX = (enable) ? -1 : 1;
        }
        #endregion
    }
}
