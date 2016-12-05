using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class AcidPoolActor : PoolActor
    {
        public AcidPoolActor(ActorDef def, float duration, string visualStyle)
            : base(def, duration, visualStyle)
        {

        }

        public static AcidPoolActor CreateAcidPoolActor(float x, float y, float duration, string visualStyle)
        {
            return PoolActor.PoolActorWithType(PoolActorType.AcidPool, x, y, duration, visualStyle) as AcidPoolActor;
        }

        public override uint ReuseKey { get { return (uint)PoolActorType.AcidPool; } }
        public override double FullDuration { get { return Globals.ASH_DURATION_ACID_POOL; } }
        public override uint BitmapID { get { return Globals.ASH_SPELL_ACID_POOL; } }
        public override uint DeathBitmap { get { return DeathBitmaps.ACID_POOL; } }
        public override string PoolTextureName { get { return "pool-of-acidR"; } }

        public override void SinkNpcShip(NpcShip ship)
        {
            base.SinkNpcShip(ship);

            if (mScene.GameMode == GameMode.Career)
            {
                PlayerShip playerShip = GameController.GC.PlayerShip;
                if (playerShip != null && playerShip.IsFlyingDutchman)
                    ++mScene.AchievementManager.SlimerCount;
            }
        }
    }
}
