using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class MagmaPoolActor : PoolActor
    {
        public MagmaPoolActor(ActorDef def, float duration, string visualStyle)
            : base(def, duration, visualStyle)
        {
            
        }

        public static MagmaPoolActor CreateMagmaPoolActor(float x, float y, float duration, string visualStyle)
        {
            return PoolActor.PoolActorWithType(PoolActorType.MagmaPool, x, y, duration, visualStyle) as MagmaPoolActor;
        }

        public override uint ReuseKey { get { return (uint)PoolActorType.MagmaPool; } }
        public override double FullDuration { get { return Globals.ASH_DURATION_MAGMA_POOL; } }
        public override uint BitmapID { get { return Globals.ASH_SPELL_MAGMA_POOL; } }
        public override uint DeathBitmap { get { return DeathBitmaps.MAGMA_POOL; } }
        public override string PoolTextureName { get { return "pool-of-magmaR"; } }
    }
}
