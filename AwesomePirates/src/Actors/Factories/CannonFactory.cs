using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class CannonFactory : ActorFactory
    {
        private static float kCannonImpulse = 10.0f; // 7.5f; // 7.5f * distance tween left and right cannon fixture on ShipActor
        private static float kNpcCannonImpulse = 7.5f;

        private static CannonFactory s_Factory = null;

        private CannonFactory()
        {
            mBlueprints = PlistParser.DictionaryFromPlist("data/plists/CannonballActors.plist");
            mCannonDetails = PlistParser.DictionaryFromPlist("data/plists/CannonDetails.plist");
            mSpecialCannonDetails = PlistParser.DictionaryFromPlist("data/plists/SpecialCannonDetails.plist");

            if (BluePrints == null || NormalCannonDetails == null || SpecialCannonDetails == null)
                throw new NotSupportedException("Could not load CannonFactory plist support files.");
        }

        public static CannonFactory Factory
        {
            get
            {
                if (s_Factory == null)
                    s_Factory = new CannonFactory();
                return s_Factory;
            }
        }

        private Dictionary<string, object> mBlueprints;
        private Dictionary<string, object> mCannonDetails;
        private Dictionary<string, object> mSpecialCannonDetails;

        private Dictionary<string, object> BluePrints { get { return mBlueprints; } }
        private Dictionary<string, object> NormalCannonDetails { get { return mCannonDetails; } }
        private Dictionary<string, object> SpecialCannonDetails { get { return mSpecialCannonDetails; } }

        public List<string> AllCannonTypes { get { return new List<string>(mCannonDetails.Keys); } }
        public Dictionary<string, object> AllCannonDetails { get { return mCannonDetails; } }
        public static float CannonImpulse { get { return 1.3f * kCannonImpulse; } }

        public CannonDetails CreateCannonDetailsForType(string cannonType)
        {
            Dictionary<string, object> dict = mCannonDetails[cannonType] as Dictionary<string, object>;
            return CreateCannonDetailsForType(cannonType, dict);
        }

        public CannonDetails CreateSpecialCannonDetailsForType(string cannonType)
        {
            Dictionary<string, object> dict = mSpecialCannonDetails[cannonType] as Dictionary<string, object>;
            CannonDetails cannonDetails = CreateCannonDetailsForType(cannonType, dict);
            cannonDetails.TextureNameFlash = "ghost-cannon-flash";
            return cannonDetails;
        }

        private CannonDetails CreateCannonDetailsForType(string cannonType, Dictionary<string, object> dict)
        {
            CannonDetails cannonDetails = new CannonDetails(cannonType);

            if (dict.ContainsKey("rangeRating"))
	            cannonDetails.RangeRating = Convert.ToInt32(dict["rangeRating"]);
            if (dict.ContainsKey("damageRating"))
                cannonDetails.DamageRating = Convert.ToInt32(dict["damageRating"]);
            if (dict.ContainsKey("shotType"))
                cannonDetails.ShotType = dict["shotType"] as string;
            if (dict.ContainsKey("textureNameBase"))
                cannonDetails.TextureNameBase = dict["textureNameBase"] as string;
            if (dict.ContainsKey("textureNameBarrel"))
                cannonDetails.TextureNameBarrel = dict["textureNameBarrel"] as string;
            if (dict.ContainsKey("textureNameWheel"))
                cannonDetails.TextureNameWheel = dict["textureNameWheel"] as string;
            if (dict.ContainsKey("textureNameMenu"))
                cannonDetails.TextureNameMenu = dict["textureNameMenu"] as string;

            cannonDetails.TextureNameFlash = "cannon-flash";

            if (dict.ContainsKey("Deck"))
                cannonDetails.DeckSettings = dict["Deck"] as Dictionary<string, object>;
            if (dict.ContainsKey("bitmap"))
                cannonDetails.Bitmap = Convert.ToUInt32(dict["bitmap"]);
            if (dict.ContainsKey("reload"))
	            cannonDetails.ReloadInterval = Globals.ConvertToSingle(dict["reload"]);
            if (dict.ContainsKey("combo"))
	            cannonDetails.ComboMax = Convert.ToInt32(dict["combo"]);
            if (dict.ContainsKey("ricochet"))
	            cannonDetails.RicochetBonus = Convert.ToInt32(dict["ricochet"]);
            if (dict.ContainsKey("imbues"))
	            cannonDetails.Imbues = Convert.ToUInt32(dict["imbues"]);

	        return cannonDetails;
        }

        public void ReshapeRicochetCone(string shotType, float factor, PolygonShape poly)
        {
            Dictionary<string, object> dict = mBlueprints[shotType] as Dictionary<string, object>;
            Dictionary<string, object> coneDict = dict["Cone"] as Dictionary<string, object>;
            List<object> array = coneDict["vertices"] as List<object>;
            
            for (int i = 0; i < poly._vertexCount; ++i)
            {
                float x, y;

                if (i < 2)
                {
                    x = Globals.ConvertToSingle(array[i * 2]);
                    y = Globals.ConvertToSingle(array[i * 2 + 1]);
                }
                else
                {
                    x = factor * Globals.ConvertToSingle(array[i * 2]);
                    y = factor * Globals.ConvertToSingle(array[i * 2 + 1]);
                }

                poly._vertices[i] = new Vector2(x, y);
            }
        }

        protected static Shape CreateRicochetCone(float factor, Dictionary<string, object> dict)
        {
            PolygonShape poly = new PolygonShape();

            List<object> array = dict["vertices"] as List<object>;
            Vector2[] vertices = new Vector2[8]; // 8 is max because PolygonShape uses a FixedArray8<Vector2> internally.
            int vertexCount = Math.Min(8, array.Count / 2);

            for (int i = 0; i < vertexCount; ++i)
            {
                if (i < 2)
                {
                    vertices[i].X = Globals.ConvertToSingle(array[i * 2]);
                    vertices[i].Y = Globals.ConvertToSingle(array[i * 2 + 1]);
                }
                else
                {
                    vertices[i].X = factor * Globals.ConvertToSingle(array[i * 2]);
                    vertices[i].Y = factor * Globals.ConvertToSingle(array[i * 2 + 1]);
                }
            }

            poly.Set(vertices, vertexCount);
            return poly;
        }

        private ActorDef CreateCannonballDefForShotType(string shotType, float bore, float x, float y, bool ricochets)
        {
            Dictionary<string, object> dict = mBlueprints[shotType] as Dictionary<string, object>;
            ActorDef actorDef = new ActorDef();
            actorDef.bd.type = BodyType.Dynamic;
            actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;

            CircleShape coreShape = new CircleShape();
            coreShape._radius = Globals.ConvertToSingle(dict["radius"]);
            coreShape._radius *= bore;
	
            actorDef.fixtureDefCount = (ricochets) ? 2 : 1;
            actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
            actorDef.fds[0] = new FixtureDef();
            actorDef.fds[0].shape = coreShape;
	
            float density = Globals.ConvertToSingle(dict["density"]);
	        float massNormalizer = coreShape._radius * coreShape._radius;

	        actorDef.fds[0].density = (massNormalizer > 0) ? density * (1 / massNormalizer) : density;
	        actorDef.fds[0].isSensor = true;
            actorDef.fds[0].filter.groupIndex = CGI_CANNONBALLS;
            actorDef.fds[0].filter.categoryBits = (COL_BIT_DEFAULT | COL_BIT_CANNONBALL_CORE);
            actorDef.fds[0].filter.maskBits = (COL_BIT_DEFAULT | COL_BIT_VOODOO | COL_BIT_NPC_SHIP_HULL | COL_BIT_PLAYER_SHIP_HULL | COL_BIT_SK_SHIP_HULL | COL_BIT_OVERBOARD);
	
	        if (ricochets)
            {
		        Dictionary<string, object> coneDict = dict["Cone"] as Dictionary<string, object>;
                //ShapeType shapeType = (ShapeType)Convert.ToInt32(coneDict["type"]);

                GameController gc = GameController.GC;
                float coneSizeFactor = ResManager.RESM.GameFactorArea;
                if ((gc.MasteryManager.MasteryBitmap & CCMastery.CANNON_CANNONEER) != 0)
                    coneSizeFactor *= 1.2f;

                actorDef.fds[1] = new FixtureDef();
                actorDef.fds[1].shape = CreateRicochetCone(coneSizeFactor, coneDict);
                actorDef.fds[1].density = 0;
                actorDef.fds[1].isSensor = true;
                actorDef.fds[1].filter.groupIndex = CGI_CANNONBALLS;
                actorDef.fds[1].filter.categoryBits = COL_BIT_CANNONBALL_CONE;
                actorDef.fds[1].filter.maskBits = COL_BIT_NPC_SHIP_HULL | COL_BIT_SK_SHIP_HULL;
	        }
	
	        return actorDef;
        }

        public void ApplyForcesToTargetedCannonball(Cannonball cannonball, ShipActor ship, Vector2 shipVector, Vector2 origin, Vector2 target)
        {
            Vector2 impulse = target - origin;
            impulse.Normalize();

            if (ship is PlayableShip)
            {
                PlayableShip playableShip = ship as PlayableShip;
                cannonball.InfamyBonus = playableShip.CannonInfamyBonus.Copy();
                impulse *= 1.3f * kCannonImpulse;
            }
            else
            {
                impulse *= 1.3f * kNpcCannonImpulse;
            }

            // Calculate angle for shot
            Vector2 shotVector = target - origin;
            float shotAngle = Box2DUtils.SignedAngle(ref shipVector, ref shotVector);

            cannonball.B2Body.SetTransform(origin, ship.B2Body.GetAngle() + shotAngle);
            cannonball.B2Body.ApplyLinearImpulse(impulse, cannonball.B2Body.GetPosition());
        }

        public void ApplyForcesToCannonball(Cannonball cannonball, ShipActor ship, ShipDetails.ShipSide side, Vector2 origin)
        {
            // Npc ships don't need the hassle of compensating for their own velocity.
            float impulseFactor = kNpcCannonImpulse;
            if (ship is PlayableShip)
            {
                PlayableShip playableShip = ship as PlayableShip;
                cannonball.InfamyBonus = playableShip.CannonInfamyBonus.Copy();

                if (!playableShip.AssistedAiming)
                {
                    Vector2 linearVelocity = ship.B2Body.GetLinearVelocity();

                    // It looks better if we shave off some of the initial velocity
                    // to compensate for our lack of wind resistance in flight.
                    linearVelocity.X /= 2.0f;
                    linearVelocity.Y /= 2.0f;
                    cannonball.B2Body.SetLinearVelocity(linearVelocity);
                }

                impulseFactor = kCannonImpulse;
            }

            Vector2 impulse;
            CircleShape portShape = ship.Port.GetShape() as CircleShape;
            CircleShape starboardShape = ship.Starboard.GetShape() as CircleShape;

            if (side == ShipDetails.ShipSide.Port)
                impulse = ship.B2Body.GetWorldPoint(portShape._p) - ship.B2Body.GetWorldPoint(starboardShape._p);
            else
                impulse = ship.B2Body.GetWorldPoint(starboardShape._p) - ship.B2Body.GetWorldPoint(portShape._p);

            impulse *= impulseFactor;
            cannonball.B2Body.SetTransform(origin, ship.B2Body.GetAngle() + ((side == ShipDetails.ShipSide.Port) ? SPMacros.PI_HALF : -SPMacros.PI_HALF));
            cannonball.B2Body.ApplyLinearImpulse(impulse, cannonball.B2Body.GetPosition());
        }

        public Cannonball CreateCannonballForCache(string shotType, bool ricochets)
        {
            float bore = CannonDetails.DefaultBore;
            ActorDef actorDef = CreateCannonballDefForShotType(shotType, bore, 0, 0, ricochets);
            Cannonball cannonball = new Cannonball(actorDef, shotType, null, bore, 1f);
            return cannonball;
        }

        public Cannonball CreateCannonballForShip(ShipActor ship, Vector2 shipVector, ShipDetails.ShipSide side, float trajectory, Vector2 target)
        {
            string shotType = ship.CannonDetails.ShotType;
            float bore = ship.CannonDetails.Bore;
            AABB aabb;
            ship.PortOrStarboard(side).GetAABB(out aabb, 0);
	        Vector2 pos = aabb.GetCenter();
            ActorDef actorDef = CreateCannonballDefForShotType(shotType, bore, pos.X, pos.Y, ship is PlayableShip);
	        Cannonball cannonball = new Cannonball(actorDef, shotType, ship, bore, trajectory);

            ApplyForcesToTargetedCannonball(cannonball, ship, shipVector, pos, target);
            return cannonball;
        }

        public Cannonball CreateCannonballForShip(ShipActor ship, ShipDetails.ShipSide side, float trajectory)
        {
            string shotType = ship.CannonDetails.ShotType;
            float bore = ship.CannonDetails.Bore;
            AABB aabb;
            ship.PortOrStarboard(side).GetAABB(out aabb, 0);
	        Vector2 pos = aabb.GetCenter();
            ActorDef actorDef = CreateCannonballDefForShotType(shotType, bore, pos.X, pos.Y, ship is PlayableShip);
	        Cannonball cannonball = new Cannonball(actorDef, shotType, ship, bore, trajectory);

            ApplyForcesToCannonball(cannonball, ship, side, pos);
            return cannonball;
        }

        // Not used, currently. Was used for resuming saved game state on iOS.
        public Cannonball CreateCannonballForShooter(SPSprite shooter, string shotType, float bore, uint ricochetCount, CannonballInfamyBonus infamyBonus, Vector2 loc,
            Vector2 vel, float trajectory, float distRemaining)
        {
            ActorDef actorDef = CreateCannonballDefForShotType(shotType, bore, loc.X, loc.Y, shooter is PlayableShip);
	        Cannonball cannonball = new Cannonball(actorDef, shotType, shooter, bore, trajectory);
	
	        if (infamyBonus != null)
                cannonball.InfamyBonus = infamyBonus.Copy();
	        cannonball.RicochetCount = ricochetCount;
	        cannonball.DistanceRemaining = distRemaining;
	        cannonball.B2Body.SetLinearVelocity(vel);
	        return cannonball;
        }

        public Cannonball CreateCannonballForNpcShooter(SPSprite shooter, string shotType, Vector2 origin, Vector2 impulse, float bore, float trajectory)
        {
            ActorDef actorDef = CreateCannonballDefForShotType(shotType, bore, origin.X, origin.Y, false);
            Cannonball cannonball = new Cannonball(actorDef, shotType, shooter, bore, trajectory);

            //cannonball.body->SetBullet(true);
            cannonball.B2Body.SetTransform(origin, -shooter.Rotation);
            cannonball.B2Body.ApplyLinearImpulse(impulse, cannonball.B2Body.GetPosition());

            return cannonball;
        }

        public Cannonball CreateCannonballForTownCannon(TownCannon cannon, float bore)
        {
            Vector2 nozzle = cannon.Nozzle;
	        ActorDef actorDef = CreateCannonballDefForShotType(cannon.ShotType, bore, ResManager.P2MX(nozzle.X), ResManager.P2MY(nozzle.Y), false);
	        Cannonball cannonball = new Cannonball(actorDef, cannon.ShotType, cannon, bore, -SPMacros.PI_HALF / 2);
	
	        //cannonball.body->SetBullet(true);
            Vector2 loc = new Vector2(ResManager.P2MX(nozzle.X), ResManager.P2MY(nozzle.Y));
	        Vector2 impulse = new Vector2(0.0f,10.0f);
            cannonball.B2Body.SetTransform(loc, -cannon.Rotation);
    
            Box2DUtils.RotateVector(ref impulse, -cannon.Rotation);
            cannonball.B2Body.ApplyLinearImpulse(impulse, cannonball.B2Body.GetPosition());

	        return cannonball;
        }
    }
}
