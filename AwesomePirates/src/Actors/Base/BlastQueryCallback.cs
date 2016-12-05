using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Box2D.XNA;

namespace AwesomePirates
{
    class BlastQueryCallback
    {
        public BlastQueryCallback()
        {
            ShipVictimCount = 0;
        }

        public int ShipVictimCount { get; private set; }
        public SKTeamIndex SinkerID { get; set; }

        public bool ReportFixture(FixtureProxy fixtureProxy)
        {
            
            if (fixtureProxy != null && fixtureProxy.fixture != null)
            {
                Fixture fixture = fixtureProxy.fixture;

                Body body = fixture.GetBody();
            
                if (body != null)
                {
                    Actor actor = body.GetUserData() as Actor;
                
                    if (actor is NpcShip)
                    {
                        NpcShip ship = actor as NpcShip;
                    
                        if (fixture == ship.Hull && !ship.Docking)
                        {
                            ship.DeathBitmap = DeathBitmaps.ABYSSAL_SURGE;
                            ship.SinkerID = SinkerID;
                            ship.Sink();
                            ++ShipVictimCount;
                        }
                    }
                    else if (actor is SkirmishShip)
                    {
                        SkirmishShip ship = actor as SkirmishShip;

                        if (fixture == ship.Hull && ship.TeamIndex != SinkerID)
                            ship.DamageShip(2);
                    }
                    else if (actor is PowderKegActor)
                    {
                        PowderKegActor keg = actor as PowderKegActor;
                        keg.Ignite();
                    }
                    else if (actor is OverboardActor)
                    {
                        OverboardActor person = actor as OverboardActor;
                        person.EnvironmentalDeath();
                    }
                }
            }
        
            return true;
        }
    }
}
