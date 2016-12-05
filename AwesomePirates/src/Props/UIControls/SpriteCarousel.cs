using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using SparrowXNA;

namespace AwesomePirates
{
    class SpriteCarousel : Prop
    {
        public SpriteCarousel(int category, float x, float y, float width, float height)
            : base(category)
        {
            mAdvanceable = true;
		    mPosition = 0.0f;
            mAcceleration = 0.075f;
		    mSpinSpeed = 0.0f;
		    mDisplayIndex = 0;
		    mSprites = new List<SPSprite>();
            mSortedSprites = new List<SPSprite>();
		
		    X = x;
		    Y = y;
		    mSpriteWidth = width;
		    mSpriteHeight = height;
        }
        
        #region Fields
        private int mDisplayIndex;
        private float mPosition;
        private float mAcceleration;
        private float mSpinSpeed;
        private float mSpriteWidth;
        private float mSpriteHeight;
        private GamePadDPad mPrevDpad;
        private Vector2 mPrevThumbVec;
        private List<SPSprite> mSprites;
        private List<SPSprite> mSortedSprites;
        #endregion

        #region Properties
        public int Count { get { return mSprites.Count; } }
        public int DisplayIndex
        {
            get { return mDisplayIndex; }
            set
            {
                if (mSprites.Count == 0)
		            return;
	            if (value >= mSprites.Count)
		            value %= mSprites.Count;
                int oldIndex = mDisplayIndex;
	            mDisplayIndex = value;
	            float intervalX = SPMacros.TWO_PI / mSprites.Count;
                TurnToPosition((mSprites.Count - mDisplayIndex) * intervalX);
                DispatchEvent(new NumericValueChangedEvent(NumericValueChangedEvent.CUST_EVENT_TYPE_SPRITE_CAROUSEL_INDEX_CHANGED, mDisplayIndex, oldIndex));
            }
        }
        public float Acceleration { get { return mAcceleration; } set { mAcceleration = value; } }
        public List<SPSprite> Sprites { get { return new List<SPSprite>(mSprites); } }
        #endregion

        #region Methods
        public void AddSprite(SPSprite sprite)
        {
            if (sprite == null)
                return;

            sprite.Touchable = false;
            mSprites.Add(sprite);
            AddChild(sprite);
            TurnToPosition(mPosition);
        }

        public void BatchAddSprite(SPSprite sprite)
        {
            if (sprite == null)
                return;

            sprite.Touchable = false;
            mSprites.Add(sprite);
            AddChild(sprite);
        }

        public void BatchAddCompleted()
        {
            TurnToPosition(mPosition);
        }

        public void RemoveSpriteAtIndex(int index)
        {
            if (index >= mSprites.Count)
                return;

            SPSprite sprite = mSprites[index];
            RemoveChild(sprite);
            mSprites.RemoveAt(index);

            int oldIndex = mDisplayIndex;
	
	        if (mDisplayIndex == mSprites.Count)
		        mDisplayIndex = 0;
	
	        // Spin to new position
	        if (mSprites.Count > 0)
            {
		        float intervalX = SPMacros.TWO_PI / mSprites.Count;
                TurnToPosition(mDisplayIndex * intervalX + 0.4f * intervalX, false);
                mSpinSpeed = 0.4f * intervalX;
                DispatchEvent(new NumericValueChangedEvent(NumericValueChangedEvent.CUST_EVENT_TYPE_SPRITE_CAROUSEL_INDEX_CHANGED, mDisplayIndex, oldIndex));
	        }
        }

        private static int CompareByY(SPSprite a, SPSprite b)
        {
            if (a.Y < b.Y)
                return -1;
            else if (a.Y > b.Y)
                return 1;
            else
                return 0;
        }

        public void TurnToPosition(float position)
        {
            TurnToPosition(position, true);
        }

        protected void TurnToPosition(float position, bool enableEvents)
        {
            if (mSprites.Count == 0)
		        return;
	
	        int i = 0;
	        float intervalX = SPMacros.TWO_PI / mSprites.Count;
	        float intervalY = SPMacros.TWO_PI / mSprites.Count;
	        float divX, divY;
	
	        foreach (SPSprite sprite in mSprites)
            {
		        divX = (float)Math.Sin(position + i * intervalX);
		        divY = 1.0f - (float)(1.0f + Math.Cos(position + i * intervalY)) / 2;
		
		        if (SPMacros.SP_IS_FLOAT_EQUAL(divX, 0))
			        divX = 0.001f;
		        if (SPMacros.SP_IS_FLOAT_EQUAL(divY, 0))
			        divY = 0.001f;
		
		        sprite.X = -(2 * mSpriteWidth / 3) * divX;
		        sprite.Y = -(3 * mSpriteHeight / 5) * divY;
		        sprite.ScaleX = (1.0f + (float)Math.Cos(position + i * intervalY)) / 2;
		
		        if (sprite.ScaleX < 0.5f)
			        sprite.ScaleX += (0.5f - sprite.ScaleX) / 2;
		        sprite.ScaleY = sprite.ScaleX;
		        ++i;
	        }
	
	        // Maintain appropriate Z-Ordering
            RemoveAllChildren();
            mSortedSprites.Clear();
            mSortedSprites.AddRange(mSprites);
            mSortedSprites.Sort(SpriteCarousel.CompareByY);

            foreach (SPSprite sprite in mSortedSprites)
                AddChild(sprite);
	
	        // clamp between [+360 deg, +720 deg]
	        mPosition = position;
	
	        if (mPosition < SPMacros.TWO_PI)
		        mPosition += SPMacros.TWO_PI;
	        else if (mPosition > 2 * SPMacros.TWO_PI)
		        mPosition -= SPMacros.TWO_PI;
	
	        // Update display index
	        float subPosition = mPosition / SPMacros.TWO_PI;
	        // Convert ratio to radians
	        subPosition = SPMacros.TWO_PI * (subPosition - (int)subPosition);
	        // Convert radians to display positions
	        subPosition = subPosition / intervalX;
	        // Get expected index for this position
	        int subIndex = (int)subPosition;
	        // Convert to ratio through this current display interval
	        subPosition = subPosition - (int)subPosition;
	
	        if (subIndex != 0)
		        subIndex = mSprites.Count - subIndex;

            int oldIndex = mDisplayIndex;

	        if (subPosition < 0.5f && subIndex != mDisplayIndex)
		        mDisplayIndex = subIndex;
	        else if (subPosition >= 0.5f && subIndex == mDisplayIndex)
		        mDisplayIndex -= 1;
	        else
		        return;	
	
	        if (mDisplayIndex < 0)
		        mDisplayIndex += mSprites.Count;
	        else if (mDisplayIndex >= mSprites.Count)
		        mDisplayIndex -= mSprites.Count;

	        // Notify listeners of display index change.
	        if (enableEvents)
                DispatchEvent(new NumericValueChangedEvent(NumericValueChangedEvent.CUST_EVENT_TYPE_SPRITE_CAROUSEL_INDEX_CHANGED, mDisplayIndex, oldIndex));
        }

        public int IndexOfSprite(SPSprite sprite)
        {
            int index = -1;

            if (sprite != null)
                index = mSprites.IndexOf(sprite);
            return index;
        }

        public SPSprite SpriteAtIndex(int index)
        {
            SPSprite sprite = null;

            if (index >= 0 && index < mSprites.Count)
                sprite = mSprites[index];
            return sprite;
        }

        public Vector2 SpritePositionAtIndex(int index)
        {
            Vector2 point = Vector2.Zero;

            if (index >= 0 && index < mSprites.Count)
            {
                SPSprite sprite = mSprites[index];
                point.X = sprite.X;
                point.Y = sprite.Y;

                if (sprite.Parent != null)
                    point = sprite.Parent.LocalToGlobal(point);
            }

            return point;
        }

        private int NextDisplayIndex()
        {
            int index = mDisplayIndex + 1;

            if (index >= mSprites.Count)
                index -= mSprites.Count;
            return index;
        }

        private int PrevDisplayIndex()
        {
            int index = mDisplayIndex - 1;

            if (index < 0)
                index += mSprites.Count;
            return index;
        }

        public void Update(GamePadState gpState, KeyboardState kbState)
        {
            int dir = 0;

            if (gpState.DPad.Left == ButtonState.Pressed)
                mSpinSpeed -= mAcceleration + mAcceleration * Math.Abs(mSpinSpeed) / 3;
            else if (gpState.DPad.Right == ButtonState.Pressed)
                mSpinSpeed += mAcceleration + mAcceleration * Math.Abs(mSpinSpeed) / 3;
            else if (mPrevDpad.Left == ButtonState.Pressed)
                dir = -1;
            else if (mPrevDpad.Right == ButtonState.Pressed)
                dir = 1;
            else
            {
                Vector2 thumbVec = gpState.ThumbSticks.Left;

                if (thumbVec.X != 0)
                    mSpinSpeed += thumbVec.X * (mAcceleration + mAcceleration * Math.Abs(mSpinSpeed) / 3);
                else if (mPrevThumbVec.X != 0)
                    dir = (mPrevThumbVec.X < 0) ? -1 : 1;

                mPrevThumbVec = thumbVec;
            }

            mPrevDpad = gpState.DPad;

            if (dir != 0)
            {
                float speed = mSpinSpeed;
                speed = Math.Abs(speed);

                float intervalX = SPMacros.TWO_PI / mSprites.Count;
                float posOffset = (-dir * speed + mPosition) / intervalX;
                posOffset = posOffset - (int)posOffset;

                if (dir == -1)
                    posOffset = 1.0f - posOffset;
                posOffset *= intervalX;
                mSpinSpeed = dir * speed + dir * posOffset;

                if (Math.Abs(mSpinSpeed) > 1.25f * intervalX)
                    mSpinSpeed -= dir * intervalX;
            }
        }

        public override void AdvanceTime(double time)
        {
            float newSpinSpeed = mSpinSpeed * 0.9f;
			
	        if (!SPMacros.SP_IS_FLOAT_EQUAL(0, mSpinSpeed))
            {
                TurnToPosition(mPosition - (mSpinSpeed - newSpinSpeed));
                mSpinSpeed = newSpinSpeed;

                //if (SPMacros.SP_IS_FLOAT_EQUAL(0, mSpinSpeed))
			    //    Debug.WriteLine("Carousel Position: " + mPosition);
	        }
        }
        #endregion
    }
}
