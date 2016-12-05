using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;

namespace AwesomePirates
{
    class TownCannon : Prop
    {
        private const float kNozzleOffset = -20.0f;
        private const float kDefaultRange = 1200.0f;
        private const float kDefaultAccuracy = 80.0f;
        private const float kRecoilTime = 0.1f;

        public TownCannon(string shotType = "single-shot_")
            : base(-1)
        {
            mShotType = shotType;
		    mAiModifier = 1.0f;
		    mShotQueue = 0;
            RecalculateAttributes();
        }
        
        #region Fields
        private float mTargetX;
        private float mTargetY;
        private float mRange;
        private float mAccuracy;
        private float mAiModifier;
        private int mShotQueue;
        private string mShotType;
        #endregion

        #region Properties
        public string ShotType { get { return mShotType; } }
        public float TargetX { get { return mTargetX; } }
        public float TargetY { get { return mTargetY; } }
        public Vector2 Nozzle
        {
            get
            {
                Vector2 pos = new Vector2(0, kNozzleOffset);
                Globals.RotatePointThroughAngle(ref pos, Rotation);
                pos.X += X;
                pos.Y += Y;
                return pos;
            }
        }
        public float Range { get { return mRange; } }
        public float RangeSquared { get { return mRange * mRange; } }
        public float Accuracy
        {
            get
            {
                int randInt = GameController.GC.NextRandom(0, 2);
                float accuracy = mAccuracy;

                if (randInt == 0)
                    accuracy = mAccuracy;
                else if (randInt == 1)
                    accuracy = -mAccuracy;
                return accuracy;
            }
        }
        public float AiModifier { get { return mAiModifier; } set { mAiModifier = value; RecalculateAttributes(); } }
        public int ShotQueue { get { return mShotQueue; } set { mShotQueue = value; } }
        #endregion

        #region Methods
        private void RecalculateAttributes()
        {
            if (mAiModifier == 0.0f)
                mAiModifier = 1.0f;
            mRange = kDefaultRange * mAiModifier;
            mAccuracy = kDefaultAccuracy / mAiModifier;
        }

        private void PlayFireCannonSound()
        {
            mScene.PlaySound("TownCannon");
        }

        public bool AimAt(float x, float y)
        {
            bool withinRange = false;
            mTargetX = x;
            mTargetY = y;
            float aimAt = SPMacros.PI - (float)Math.Atan2(x - X, y - Y);

            if (aimAt > SPMacros.SP_D2R(90) && aimAt < SPMacros.SP_D2R(180))
            {
                Rotation = aimAt;
                withinRange = true;
            }

            return withinRange;
        }

        public bool Fire(Vector2 targetVel)
        {
            PlayFireCannonSound();

            // Fire Cannonball
            float x = mTargetX, y = mTargetY;
            Vector2 origin = new Vector2(ResManager.P2MX(Nozzle.X), ResManager.P2MY(Nozzle.Y));
            Vector2 impulse = new Vector2(0.0f,10.0f);
            Box2DUtils.RotateVector(ref impulse, -Rotation);

            Cannonball cannonball = Cannonball.CannonballForNpcShooter(this, ShotType, origin, impulse, 0.75f, -SPMacros.PI_HALF / 2);
            mScene.AddActor(cannonball);
            cannonball.CalculateTrajectoryFrom(x, y);
            cannonball.B2Body.SetLinearVelocity(cannonball.B2Body.GetLinearVelocity() + targetVel);
            cannonball.SetupCannonball();

            // Smoke
            CannonFire smoke = PointMovie.PointMovieWithType(PointMovie.PointMovieType.CannonFire, cannonball.PX, cannonball.PY) as CannonFire;
            smoke.ScaleX = smoke.ScaleY = 1.25f;
            smoke.CannonRotation = Rotation;

            Vector2 smokeVel = new Vector2(0.0f, -0.5f);
            Globals.RotatePointThroughAngle(ref smokeVel, smoke.CannonRotation);
            smoke.SetLinearVelocity(smokeVel.X, smokeVel.Y);

            return true;
        }

        public void Idle()
        {
            Rotation = SPMacros.SP_D2R(135);
        }

        public static int ShotQueueCompare(TownCannon a, TownCannon b)
        {
            if (a.ShotQueue < b.ShotQueue)
                return 1;
            else if (a.ShotQueue > b.ShotQueue)
                return -1;
            else
                return 0;
        }
        #endregion
    }
}
