using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;
using System.Diagnostics;

namespace AwesomePirates
{
    sealed class ViewParser : IDisposable
    {
        private const float kDefaultMenuFontSize = 32.0f;

        public ViewParser(SceneController scene, object eventListener, SPEventHandler eventHandler, string plistPath)
        {
            mScene = scene;
            mEventHandler = eventHandler;
		    mEventListener = eventListener;
		    mCategory = 0;
		    mViewData = PlistParser.DictionaryFromPlist(plistPath);
		    mFont = scene.FontKey;
        }
        
        #region Fields
        private bool mIsDisposed = false;
        private int mCategory;
        private string mFont;
        private Dictionary<string, object> mViewData;
        private SPEventHandler mEventHandler;
        private object mEventListener;
        private SceneController mScene;
        #endregion

        #region Properties
        public int Category { get { return mCategory; } set { mCategory = value; } }
        public string FontKey { get { return mFont; } set { mFont = value; } }
        public Dictionary<string, object> ViewData { get { return new Dictionary<string, object>(mViewData); } }
        #endregion

        #region Methods
        public void ChangePlistPath(string path)
        {
            mViewData = PlistParser.DictionaryFromPlist(path);
        }

        public MenuDetailView ParseSubviewByName(string name, string viewName, int index = -1)
        {
            MenuDetailView detailView = new MenuDetailView(mCategory);
            return ParseSubview(detailView, name, viewName, index);
        }

        public TitleSubview ParseTitleSubviewByName(string name, string viewName, int index = -1)
        {
            TitleSubview detailView = new TitleSubview(mCategory);
            return ParseSubview(detailView, name, viewName, index) as TitleSubview;
        }

        public Dictionary<string, MenuDetailView> ParseSubviewsByViewName(string viewName)
        {
            Dictionary<string, MenuDetailView> subviews = new Dictionary<string,MenuDetailView>();
            Dictionary<string, object> viewDict = null;

            try { viewDict = mViewData[viewName] as Dictionary<string, object>; }
            catch (Exception e) { NotifyKeyNotFound(viewName, e); throw e; }
	
	        foreach (KeyValuePair<string, object> kvp in viewDict)
            {
                MenuDetailView detailView = new MenuDetailView(mCategory);
                ParseSubview(detailView, kvp.Key, viewName);
			
		        if (detailView != null)
                    subviews[kvp.Key] = detailView;
	        }

	        return subviews;
        }

        public Dictionary<string, TitleSubview> ParseTitleSubviewsByViewName(string viewName)
        {
            Dictionary<string, TitleSubview> subviews = new Dictionary<string, TitleSubview>();
            Dictionary<string, object> viewDict = null;

            try { viewDict = mViewData[viewName] as Dictionary<string, object>; }
            catch (Exception e) { NotifyKeyNotFound(viewName, e); throw e; }

            foreach (KeyValuePair<string, object> kvp in viewDict)
            {
                uint navMap = (kvp.Key == "Query" || kvp.Key == "Alert") ? Globals.kNavHorizontal : Globals.kNavVertical;
                TitleSubview detailView = new TitleSubview(mCategory, navMap);
                ParseSubview(detailView, kvp.Key, viewName);

                if (detailView != null)
                    subviews[kvp.Key] = detailView;
            }

            return subviews;
        }

        private void NotifyKeyNotFound(string key, Exception e)
        {
            Debug.WriteLine("Key \"" + key + "\" not found in ViewParser. " + e.Message);
        }

        private MenuDetailView ParseSubview(MenuDetailView view, string name, string viewName, int index = -1)
        {
            List<object> array = null;
	        Dictionary<string, object> viewDict = mViewData[viewName] as Dictionary<string, object>;
	
	        if (index != -1)
            {
                array = viewDict[name] as List<object>;
		        viewDict = array[index] as Dictionary<string, object>;
	        }
            else
            {
		        viewDict = viewDict[name] as Dictionary<string, object>;
	        }
	
	        float x = 0, y = 0;
            
            if (viewDict.ContainsKey("x"))
                x = Globals.ConvertToSingle(viewDict["x"]);
            if (viewDict.ContainsKey("y"))
                y = Globals.ConvertToSingle(viewDict["y"]);

            view.X = x;
            view.Y = y;
	
            Dictionary<string, object> offsetDict = null;

            if (viewDict.ContainsKey("ResOffset"))
                offsetDict = viewDict["ResOffset"] as Dictionary<string, object>;

            ResManager.RESM.PushOffset(ParseResOffset(offsetDict));

            List<object> scrolls = null;

            if (viewDict.ContainsKey("Scrolls"))
            {
                scrolls = viewDict["Scrolls"] as List<object>;

                if (scrolls != null)
                    ParseQuarterFoldouts(scrolls, view);
            }

            if (view is TitleSubview)
            {
                TitleSubview titleSubview = view as TitleSubview;

                string selString = null;
                if (viewDict.ContainsKey("closeSelector"))
                    selString = viewDict["closeSelector"] as string;

                if (selString != null)
                    titleSubview.CloseSelector = mEventListener.GetType().GetMethod(selString);
#if true
                SPSprite scrollSprite = titleSubview.ControlForKey("Scroll") as SPSprite;
                float closeAdjustX = 0;

                if (viewDict.ContainsKey("closeAdjustX"))
                    closeAdjustX = Globals.ConvertToSingle(viewDict["closeAdjustX"]);

                if (scrollSprite != null)
                    titleSubview.ClosePosition = new CCPoint(
                        scrollSprite.X + scrollSprite.Width / 2 - (0.85f * ResManager.RITMFX(-closeAdjustX + 44f) + 32f * scrollSprite.ScaleX),
                        scrollSprite.Y - scrollSprite.Height / 2 + 20f * scrollSprite.ScaleY);

#else
                float closeX = -1, closeY = -1;
                try { closeX = Globals.ConvertToSingle(viewDict["closeX"]); }
                catch (Exception) { }
                try { closeY = Globals.ConvertToSingle(viewDict["closeY"]); }
                catch (Exception) { }

                if (closeX != -1 && closeY != -1)
                    titleSubview.ClosePosition = new CCPoint(2 * closeX, 2 * closeY);
#endif

                if (viewDict.ContainsKey("guidePos"))
                    titleSubview.GuidePos = TitleSubview.GuidePositionForScene((TitleSubview.GuidePosition)Convert.ToInt32(viewDict["guidePos"]), mScene);

                if (viewDict.ContainsKey("gamerPicX") && viewDict.ContainsKey("gamerPicY"))
                    titleSubview.GamerPicPos = new CCPoint
                        (
                            ResManager.RESX(Convert.ToInt32(viewDict["gamerPicX"])),
                            ResManager.RESY(Convert.ToInt32(viewDict["gamerPicY"]))
                        );

                if (viewDict.ContainsKey("scaleToFill"))
                {
                    titleSubview.ScaleToFillThreshold = Globals.ConvertToSingle(viewDict["scaleToFill"]);
                    titleSubview.DoesScaleToFill = true;
                }
            }

            List<object> images = null;

            if (viewDict.ContainsKey("Images"))
                images = viewDict["Images"] as List<object>;

            if (images != null)
                ParseImages(images, view);

            List<object> labels = null;

            if (viewDict.ContainsKey("Labels"))
                labels = viewDict["Labels"] as List<object>;

            if (labels != null)
                ParseLabels(labels, view);

#if (!WINDOWS && !XBOX)
            List<Dictionary<string, object>> touchThumbs = null;

            if (viewDict.ContainsKey("Thumbs"))
                touchThumbs = viewDict["Thumbs"] as List<Dictionary<string, object>>;

            if (touchThumbs != null)
                ParseTouchThumbs(touchThumbs, view);
#endif

            List<object> buttons = null;

            if (viewDict.ContainsKey("Buttons"))
                buttons = viewDict["Buttons"] as List<object>;

            if (buttons != null)
                ParseButtons(buttons, view);

            ResManager.RESM.PopOffset();

	        return view;
        }

        private ResOffset ParseResOffset(Dictionary<string, object> dict)
        {
            float x = 0, y = 0, custX = 0, custY = 0;

            if (dict != null)
            {
                foreach (KeyValuePair<string, object> kvp in dict)
                {
                    if (kvp.Key.Equals("x"))
                        x = Globals.ConvertToSingle(kvp.Value);
                    else if (kvp.Key.Equals("y"))
                        y = Globals.ConvertToSingle(kvp.Value);
                    else if (kvp.Key.Equals("padX"))
                        custX = Globals.ConvertToSingle(kvp.Value);
                    else if (kvp.Key.Equals("padY"))
                        custY = Globals.ConvertToSingle(kvp.Value);
                }
            }

            x *= 2;
            y *= 2;
            custX *= 2;
            custY *= 2;

            if (custX != 0)
                custX *= ResManager.CUSTX / 64f;
            if (custY != 0)
                custY *= ResManager.CUSTY / 128f;

            return new ResOffset(x, y, custX, custY);
        }

        private void ApplyTransform(SPDisplayObject transform, SPDisplayObject displayObject)
        {
            displayObject.X = transform.X;
            displayObject.Y = transform.Y;
            displayObject.ScaleX = transform.ScaleX;
            displayObject.ScaleY = transform.ScaleY;
            displayObject.Rotation = transform.Rotation;
        }

        private void ParseTransform(Dictionary<string, object> dict, SPDisplayObject displayObject)
        {
            ResOffset offset = null;
	        Dictionary<string, object> offsetDict = null;

            if (dict.ContainsKey("ResOffset"))
                offsetDict = dict["ResOffset"] as Dictionary<string, object>;
	
	        if (offsetDict != null)
            {
		        offset = ParseResOffset(offsetDict);
                ResManager.RESM.PushOffset(offset);
	        }
	
	        foreach (KeyValuePair<string, object> kvp in dict)
            {
		        if (kvp.Key.Equals("x"))
                    displayObject.X = ResManager.RESX(2 * Globals.ConvertToSingle(kvp.Value));
		        else if (kvp.Key.Equals("y"))
                    displayObject.Y = ResManager.RESY(2 * Globals.ConvertToSingle(kvp.Value));
		        else if (kvp.Key.Equals("scaleX"))
                    displayObject.ScaleX = Globals.ConvertToSingle(kvp.Value);
                else if (kvp.Key.Equals("scaleY"))
                    displayObject.ScaleY = Globals.ConvertToSingle(kvp.Value);
                else if (kvp.Key.Equals("pivotX"))
                    displayObject.PivotX = 2 * Globals.ConvertToSingle(kvp.Value);
                else if (kvp.Key.Equals("pivotY"))
                    displayObject.PivotY = 2 * Globals.ConvertToSingle(kvp.Value);
                else if (kvp.Key.Equals("rotation"))
                    displayObject.Rotation = SPMacros.SP_D2R(Globals.ConvertToSingle(kvp.Value));
	        }
	
	        if (offset != null)
                ResManager.RESM.PopOffset();
        }

        private SPTween ParseTween(Dictionary<string, object> dict, SPDisplayObject displayObject)
        {
            SPTween tween = null;
	        ResOffset offset = null;
            Dictionary<string, object> offsetDict = null;

            if (dict.ContainsKey("ResOffset"))
                offsetDict = dict["ResOffset"] as Dictionary<string, object>;
	
	        if (offsetDict != null)
		        offset = ParseResOffset(offsetDict);
            if (offset == null)
                offset = new ResOffset(0, 0);
    
            float duration = 1.0f;
            string transition = SPTransitions.SPLinear;
            SPLoopType loopType = SPLoopType.None;
            float delay = 0, repeatDelay = 0;
            List<Dictionary<string, object>> properties = null;
    
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                if (kvp.Key.Equals("duration"))
                    duration = Globals.ConvertToSingle(kvp.Value);
                else if (kvp.Key.Equals("transition"))
                    transition = kvp.Value as string;
                else if (kvp.Key.Equals("loop"))
                    loopType = (SPLoopType)Convert.ToInt32(kvp.Value);
                else if (kvp.Key.Equals("delay"))
                    delay = Globals.ConvertToSingle(kvp.Value);
                else if (kvp.Key.Equals("repeatDelay"))
                    repeatDelay = Globals.ConvertToSingle(kvp.Value);
                else if (kvp.Key.Equals("Properties"))
                    properties = kvp.Value as List<Dictionary<string, object>>;
	        }
    
            if (properties != null && properties.Count > 0)
            {
                tween = new SPTween(displayObject, duration, transition);

                foreach (Dictionary<string, object> property in properties)
                {
                    float targetValue = Globals.ConvertToSingle(property["targetValue"]);
            
                    if (ResManager.RESM.IsCustRes)
                        targetValue += Globals.ConvertToSingle(property["iPadOffset"]);

                    string propName = property["name"] as string;
                    if (propName.Equals("x") || propName.Equals("y"))
                        targetValue *= 2;

                    tween.AnimateProperty(propName, targetValue);
                }
        
                tween.Loop = loopType;
                tween.Delay = delay;
                tween.RepeatDelay = repeatDelay;
            }
    
            return tween;
        }

        public void ParseLabels(List<object> labels, MenuDetailView view)
        {
            foreach (Dictionary<string, object> dict in labels)
            {
		        uint color = 0;
		        float width = 0, height = 0;
		        float fontSize = 0;
		        string prefix = "";
		        string suffix = "";
		        SPTextField.SPHAlign halign = SPTextField.SPHAlign.Left;
                SPTextField.SPVAlign valign = SPTextField.SPVAlign.Top;
		        string viewKey = null;
                List<object> textArray = null;
		        SPSprite transform = new SPSprite();
                List<Dictionary<string, object>> tweens = null;

                ParseTransform(dict, transform);
		
		        foreach (KeyValuePair<string, object> kvp in dict)
                {
			        if (kvp.Key.Equals("color"))
                        color = (uint)Convert.ToInt32(kvp.Value);
                    else if (kvp.Key.Equals("width"))
                        width = 2 * Globals.ConvertToSingle(kvp.Value);
                    else if (kvp.Key.Equals("height"))
                        height = 2 * Globals.ConvertToSingle(kvp.Value);
                    else if (kvp.Key.Equals("fontSize"))
                        fontSize = 1.5f * Globals.ConvertToSingle(kvp.Value);
                    else if (kvp.Key.Equals("halign"))
                        halign = (SPTextField.SPHAlign)Convert.ToInt32(kvp.Value);
                    else if (kvp.Key.Equals("valign"))
                        valign = (SPTextField.SPVAlign)Convert.ToInt32(kvp.Value);
                    else if (kvp.Key.Equals("name"))
                        viewKey = kvp.Value as string;
                    else if (kvp.Key.Equals("text"))
                        textArray = kvp.Value as List<object>;
                    else if (kvp.Key.Equals("prefix"))
                        prefix = kvp.Value as string;
                    else if (kvp.Key.Equals("suffix"))
                        suffix = kvp.Value as string;
                    else if (kvp.Key.Equals("Tweens"))
                        tweens = kvp.Value as List<Dictionary<string, object>>;
		        }
		
		        List<SPTextField> labelArray = null;
		
		        if (textArray == null)
                    textArray = new List<object>() { "" };
		
		        foreach (string text in textArray)
                {
			        if (fontSize == 0)
				        fontSize = kDefaultMenuFontSize;
			        if (width == 0)
				        width = 0.5f * fontSize * text.Length;
			        if (height == 0)
				        height = fontSize;
			
			        SPTextField textField = new SPTextField(width, height, prefix + text + suffix, mFont, (int)fontSize);
                    ApplyTransform(transform, textField);
			        textField.Color = SPUtils.ColorFromColor(color);
			        textField.HAlign = halign;
			        textField.VAlign = valign;
			        textField.Touchable = false;
                    view.AddChild(textField);

                    if (tweens != null)
                    {
                        foreach (Dictionary<string, object> tweenDict in tweens)
                        {
                            SPTween tween = ParseTween(tweenDict, textField);

                            if (tween != null && tween.Loop != SPLoopType.None)
                                view.AddLoopingTween(tween);
                            mScene.Juggler.AddObject(tween);
                        }
                    }
			
			        if (viewKey != null)
                    {
				        if (textArray.Count > 1)
                        {
					        if (labelArray == null)
                            {
						        labelArray = new List<SPTextField>(textArray.Count);
                                view.SetLabelArrayForKey(labelArray, viewKey);
					        }
					
                            labelArray.Add(textField);
				        }
                        else
                        {
                            view.SetControlForKey(textField, viewKey);
				        }
			        }
		        }
	        }
        }

        public void ParseImages(List<object> images, MenuDetailView view)
        {
            foreach (Dictionary<string, object> dict in images)
            {
		        string viewKey = null, textureName = null;
                List<Dictionary<string, object>> tweens = null;
		        SPSprite transform = new SPSprite();

                ParseTransform(dict, transform);
		
		        foreach (KeyValuePair<string, object> kvp in dict)
                {
			        if (kvp.Key.Equals("texture"))
                        textureName = kvp.Value as string;
                    else if (kvp.Key.Equals("name"))
                        viewKey = kvp.Value as string;
                    else if (kvp.Key.Equals("Tweens"))
                        tweens = kvp.Value as List<Dictionary<string, object>>;
		        }
		
		        SPImage image = new SPImage(mScene.TextureByName(textureName));
                ApplyTransform(transform, image);
		        image.Touchable = false;
                view.AddChild(image);

                if (tweens != null)
                {
                    foreach (Dictionary<string, object> tweenDict in tweens)
                    {
                        SPTween tween = ParseTween(tweenDict, image);

                        if (tween != null && tween.Loop != SPLoopType.None)
                            view.AddLoopingTween(tween);
                        mScene.Juggler.AddObject(tween);
                    }
                }
			
		        if (viewKey != null)
                    view.SetControlForKey(image, viewKey);
	        }
        }

        public void ParseButtons(List<object> buttons, MenuDetailView view)
        {
            foreach (Dictionary<string, object> dict in buttons)
            {
                bool isNavigable = true;
		        float scaleWhenDown = 0.9f, alphaWhenDisabled = 1.0f, effectFactor = 1.0f;
		        string viewKey = null, textureName = null, selString = null, sfxKey = "Button";
                List<Dictionary<string, object>> tweens = null;
		        SPSprite transform = new SPSprite();

                ParseTransform(dict, transform);
		
		        foreach (KeyValuePair<string, object> kvp in dict)
                {
                    if (kvp.Key.Equals("scaleWhenDown"))
                        scaleWhenDown = Globals.ConvertToSingle(kvp.Value);
                    else if (kvp.Key.Equals("alphaWhenDisabled"))
                        alphaWhenDisabled = Globals.ConvertToSingle(kvp.Value);
                    else if (kvp.Key.Equals("texture"))
                        textureName = kvp.Value as string;
                    else if (kvp.Key.Equals("selector"))
                        selString = kvp.Value as string;
                    else if (kvp.Key.Equals("name"))
                        viewKey = kvp.Value as string;
                    else if (kvp.Key.Equals("sfxKey"))
                        sfxKey = kvp.Value as string;
                    else if (kvp.Key.Equals("isNavigable"))
                        isNavigable = Convert.ToBoolean(kvp.Value);
                    else if (kvp.Key.Equals("effectFactor"))
                        effectFactor = Globals.ConvertToSingle(kvp.Value);
                    else if (kvp.Key.Equals("Tweens"))
                        tweens = kvp.Value as List<Dictionary<string, object>>;
		        }

		        SPTexture texture = mScene.TextureByName(textureName);
                Action actionSelector = null;

                if (selString != null)
                    actionSelector = delegate() { mEventListener.GetType().GetMethod(selString).Invoke(mEventListener, null); };
                MenuButton button = new MenuButton(actionSelector, texture, texture, isNavigable);
                //MenuButton button = new MenuButton(mEventListener.GetType().GetMethod(selString), texture, texture, isNavigable);
                ApplyTransform(transform, button);
                button.ScaleWhenDown = scaleWhenDown;
                button.AlphaWhenDisabled = alphaWhenDisabled;
                button.SfxKey = sfxKey;

                if (button.SelectedEffecter != null)
                    button.SelectedEffecter.Factor = effectFactor;

                if (mEventHandler != null)
                    button.AddEventListener(SPButton.SP_EVENT_TYPE_TRIGGERED, mEventHandler);
                view.AddChild(button);

                if (tweens != null)
                {
                    foreach (Dictionary<string, object> tweenDict in tweens)
                    {
                        SPTween tween = ParseTween(tweenDict, button);

                        if (tween != null && tween.Loop != SPLoopType.None)
                            view.AddLoopingTween(tween);
                        mScene.Juggler.AddObject(tween);
                    }
                }
		
		        if (viewKey != null)
                    view.SetControlForKey(button, viewKey);
	        }
        }

        public void ParseQuarterFoldouts(List<object> foldouts, MenuDetailView view)
        {
            string viewKey = "Scroll";

            foreach (Dictionary<string, object> dict in foldouts)
            {
		        SPSprite transform = new SPSprite();
                ParseTransform(dict, transform);
		
		        SPTexture texture = null;
		        string textureName = dict["texture"] as string;
		        string cachedTextureName = textureName + "-cached";
		
		        // Re-use rendered texture memory
		        texture = mScene.TextureByName(cachedTextureName);
		
		        if (texture == null)
                {
			        texture = mScene.TextureByName(textureName);
			        texture = Globals.WholeTextureFromQuarter(texture);
                    mScene.CacheTexture(texture, cachedTextureName);
		        }
		
		        SPImage image = new SPImage(texture);
		        image.X = -image.Width / 2;
		        image.Y = -image.Height / 2;
		
		        SPSprite sprite = new SPSprite();
		        sprite.Touchable = false;

                // Hack so that we can re-use iPhone resources easily.
                if (view.ControlForKey(viewKey) == null)
                {
                    sprite.X = ResManager.RESM.Width / 2;
                    sprite.Y = ResManager.RESM.Height / 2;
                }
                else
                {
                    sprite.X = transform.X + image.Width / 2;
                    sprite.Y = transform.Y + image.Height / 2;
                }
       
		        sprite.ScaleX = transform.ScaleX;
		        sprite.ScaleY = transform.ScaleY;
		        sprite.Rotation = SPMacros.SP_D2R(transform.Rotation);
                sprite.AddChild(image);
                view.AddChild(sprite);
		
		        if (viewKey != null && view.ControlForKey(viewKey) == null)
                    view.SetControlForKey(sprite, viewKey);
	        }
        }

        private void ParseTouchThumbs(List<object> thumbs, MenuDetailView view)
        {
            if (thumbs == null)
                return;
            SPTexture thumbTexture = mScene.TextureByName("touch-thumb");
            SPTexture textTexture = mScene.TextureByName("touch-text");
    
            foreach (Dictionary<string, object> dict in thumbs)
            {
                bool leftThumb = false;
                string viewKey = null;
                List<Dictionary<string, object>> tweens = null;
        
                SPSprite transform = new SPSprite();
                ParseTransform(dict, transform);
        
                foreach (KeyValuePair<string, object> kvp in dict)
                {
                    if (kvp.Key.Equals("name"))
                        viewKey = kvp.Value as string;
                    else if (kvp.Key.Equals("leftThumb"))
                        leftThumb = Convert.ToBoolean(kvp.Value);
                    else if (kvp.Key.Equals("Tweens"))
                        tweens = kvp.Value as List<Dictionary<string, object>>;
                }
        
                SPImage thumbImage = new SPImage(thumbTexture);
                thumbImage.X = -thumbImage.Width / 2;
                thumbImage.Y = -thumbImage.Height / 2;
        
                SPImage thumbText = new SPImage(textTexture);
                thumbText.X = thumbImage.X;
                thumbText.Y = thumbImage.Y;
        
                if (!leftThumb)
                {
                    thumbText.X += 30;
                    thumbText.Y += 46;
                }
                else
                {
                    thumbImage.ScaleX = -1;
                    thumbImage.X += thumbImage.Width;
                    thumbText.X += 82;
                    thumbText.Y += 46;
                }
        
                SPSprite thumbSprite = new SPSprite();
                thumbSprite.X = transform.X;
                thumbSprite.Y = transform.Y;
                thumbSprite.ScaleX = transform.ScaleX;
                thumbSprite.ScaleY = transform.ScaleY;
                thumbSprite.Rotation = SPMacros.SP_D2R(transform.Rotation);
        
                thumbSprite.AddChild(thumbImage);
                thumbSprite.AddChild(thumbText);
                view.AddChild(thumbSprite);

                if (tweens != null)
                {
                    foreach (Dictionary<string, object> tweenDict in tweens)
                    {
                        SPTween tween = ParseTween(tweenDict, thumbSprite);

                        if (tween != null && tween.Loop != SPLoopType.None)
                            view.AddLoopingTween(tween);
                        mScene.Juggler.AddObject(tween);
                    }
                }
        
                if (viewKey != null)
                    view.SetControlForKey(thumbSprite, viewKey);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                if (disposing)
                {
                    mFont = null;
                    mViewData = null;
                    mEventListener = null;
                    mScene = null;
                }

                mIsDisposed = true;
            }
        }

        ~ViewParser()
        {
            Dispose(false);
        }
        #endregion
    }
}
