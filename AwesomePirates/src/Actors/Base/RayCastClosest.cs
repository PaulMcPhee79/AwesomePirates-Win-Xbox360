using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Box2D.XNA;

namespace AwesomePirates
{
    class RayCastClosest
    {
        public RayCastClosest(Body owner, float pointBlankFraction)
        {
            mOwner = owner;
            mPointBlankFraction = pointBlankFraction;
            mFixture = null;
            mGlancingFixture = null;
            OwnerID = null;
        }

        public SKTeamIndex? OwnerID { get; set; }

        public void ResetFixture()
        {
            mFixture = null;
            mGlancingFixture = null;
        }

        public float ReportFixture(Fixture fixture, Vector2 point, Vector2 normal, float fraction)
        {
            float rayControl = -1;
		    Body body = fixture.GetBody();
		
		    if (body != null && body != mOwner)
            {
			    bool shouldProcessRay = false;
			    Actor actor = body.GetUserData() as Actor;
			
			    if (actor is NpcShip)
                {
				    NpcShip ship = actor as NpcShip;
				
				    if (!ship.InWhirlpoolVortex && fixture != ship.Feeler && !ship.MarkedForRemoval && !ship.Docking)
                    {
                        // Don't process hitbox collisions if we're too close to the other ship
                        // else it's too difficult to thread a long shot between nearby ships.
                        if (fixture != ship.HitBox || fraction > mPointBlankFraction)
                        {
                            shouldProcessRay = true;

                            //if (ship is MerchantShip)
                            //{
                            //    MerchantShip merchantShip = ship as MerchantShip;
                            //    shouldProcessRay = (fixture != merchantShip.Defender);
                            //}
                        }
                        else if (mGlancingFixture == null)
                            mGlancingFixture = fixture;
				    }
			    }
                else if (actor is BrandySlickActor)
                {
                    BrandySlickActor brandySlick = actor as BrandySlickActor;
                    shouldProcessRay = (!OwnerID.HasValue || OwnerID.Value == brandySlick.OwnerID) && !brandySlick.Ignited;
                }
                else if (actor is SkirmishShip)
                {
                    SkirmishShip ship = actor as SkirmishShip;

                    if (!ship.Sinking && !ship.MarkedForRemoval)
                    {
                        if (fixture != ship.HitBox || fraction > 0.05f)
                            shouldProcessRay = true;
                    }
                }
			
			    if (shouldProcessRay)
                {
				    mFixture = fixture;
				    mPoint = point;
				    mNormal = normal;
				    mFraction = fraction;
				    rayControl = fraction;
			    }
		    }
		    return rayControl;
        }

        private float mPointBlankFraction;
        private Body mOwner;
        private Fixture mFixture;
        private Fixture mGlancingFixture; // Better to hit close ones if nothing else gets hit.
        private Vector2 mPoint;
        private Vector2 mNormal;
        private float mFraction;

        public Fixture Fixture { get { return mFixture; } set { mFixture = value; } }
        public Fixture GlancingFixture { get { return mGlancingFixture; } }
    }
}
