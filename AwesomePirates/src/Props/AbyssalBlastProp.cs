using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class AbyssalBlastProp : BlastProp
    {
        public AbyssalBlastProp(int category)
            : base(category, "Abyssal")
        {
            mBlastWave = null;
            mBlastScale = mAbyssalFactor = 1.5f * ResManager.RESM.GameFactorArea; // (ResManager.RES_BACKBUFFER_WIDTH / ResManager.kResStandardWidth);
            SetupProp();
        }

        public static AbyssalBlastProp GetAbyssalBlastProp(int category)
        {
            return BlastProp.BlastPropWithType(BlastPropType.Abyssal, category) as AbyssalBlastProp;
        }

        private float mAbyssalFactor;
        private BlastQueryCallback mBlastWave;

        public override uint ReuseKey { get { return (uint)BlastPropType.Abyssal; } }

        protected override void SetupProp()
        {
            if (mBlastSound == null)
                mBlastSound = "AbyssalBlast";
            if (mBlastTexture == null)
                mBlastTexture = mScene.TextureByName("abyssal-surge");
            base.SetupProp();
        }

        public override void BlastDamage()
        {
            if (mBlastWave == null)
                mBlastWave = BlastProp.BlastQueryCB;
            mBlastWave.SinkerID = SinkerID;

            AABB aabb;
            // Base image: 256x232 ; mBlastScale = 1f ; Hit Area: 206x192.
            aabb.lowerBound = new Vector2(ResManager.P2MX(X - (103 * mAbyssalFactor)), ResManager.P2MY(Y + (96 * mAbyssalFactor)));
            aabb.upperBound = new Vector2(ResManager.P2MX(X + (103 * mAbyssalFactor)), ResManager.P2MY(Y - (96 * mAbyssalFactor)));
    
            if (mScene is PlayfieldController)
            {
                PlayfieldController playfieldScene = mScene as PlayfieldController;
                playfieldScene.World.QueryAABB(mBlastWave.ReportFixture, ref aabb);

                if (mScene.GameMode == GameMode.Career)
                    mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_BLAST_VICTIMS, mBlastWave.ShipVictimCount);
            }

            base.BlastDamage();
        }
    }
}
