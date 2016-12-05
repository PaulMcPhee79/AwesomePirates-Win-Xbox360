using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class MiscFactory : ActorFactory
    {
        private const int kSharkHead = 0;
        private const int kSharkNose = 1;

        private const int kPool = 0;
        private const int kEye = 1;

        private static MiscFactory s_Factory = null;

        private MiscFactory()
        {
            mRaceTrack = PlistParser.DictionaryFromPlist("data/plists/RaceTrack.plist");

#if DEBUG
            if (mRaceTrack == null)
                throw new NotSupportedException("Could not load MiscFactory plist support files.");
#endif
        }

        public static MiscFactory Factory
        {
            get
            {
                if (s_Factory == null)
                    s_Factory = new MiscFactory();
                return s_Factory;
            }
        }

        private Dictionary<string, object> mRaceTrack;

        public ActorDef CreateLootDefinition(float x, float y, float radius)
        {
            ActorDef actorDef = new ActorDef();
	        actorDef.bd.type = BodyType.Dynamic;
	        actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
	
            CircleShape shape = new CircleShape();
            shape._radius = radius;
            shape._p.X = 0;
            shape._p.Y = 0;

            actorDef.fixtureDefCount = 1;
            actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
            actorDef.fds[0] = new FixtureDef();
            actorDef.fds[0].shape = shape;
            actorDef.fds[0].density = 1f;
            actorDef.fds[0].isSensor = true;
            actorDef.fds[0].filter.groupIndex = CGI_ENEMY_EXCLUDED; // We're not using CGI_ENEMY_EXCLUDED elsewhere, so this seems redundant...
            actorDef.fds[0].filter.categoryBits = COL_BIT_PLAYER_BUFF;
            actorDef.fds[0].filter.maskBits = COL_BIT_PLAYER_SHIP_HULL | COL_BIT_SK_SHIP_HULL;
	
	        return actorDef;
        }

        public ActorDef CreatePoolDefinition(float x, float y)
        {
            ActorDef actorDef = new ActorDef();
	        actorDef.bd.type = BodyType.Dynamic;
	        actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
            actorDef.bd.linearDamping = 5.0f;
            actorDef.bd.angularDamping = 3.0f;
	
            CircleShape shape = new CircleShape();
            shape._radius = ResManager.P2M(36f);
            shape._p.X = 0;
            shape._p.Y = 0;

            actorDef.fixtureDefCount = 1;
            actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
            actorDef.fds[0] = new FixtureDef();
            actorDef.fds[0].shape = shape;
            actorDef.fds[0].density = 1f;
            actorDef.fds[0].isSensor = true;
            actorDef.fds[0].filter.groupIndex = CGI_PLAYER_EXCLUDED;
            actorDef.fds[0].filter.categoryBits = COL_BIT_VOODOO;
            actorDef.fds[0].filter.maskBits = (COL_BIT_VOODOO | COL_BIT_NPC_SHIP_STERN | COL_BIT_OVERBOARD | COL_BIT_SK_SHIP_HULL);
	
	        return actorDef;
        }

        public ActorDef CreateTownDockDefinition(float x, float y, float angle)
        {
            ActorDef actorDef = new ActorDef();

            actorDef.bd.type = BodyType.Static;
            actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
            actorDef.bd.angle = angle;

            actorDef.fixtureDefCount = 1;
            actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];

            CircleShape shape = new CircleShape();
            shape._radius = ResManager.P2M(56.0f);
            shape._p.X = 0;
            shape._p.Y = 0;

            actorDef.fds[0] = new FixtureDef();
            actorDef.fds[0].density = 0.0f;
            actorDef.fds[0].shape = shape;
            actorDef.fds[0].isSensor = true;

            return actorDef;
        }

        public ActorDef CreateSharkDef(float x, float y, float angle)
        {
            ActorDef actorDef = new ActorDef();
	
	        actorDef.bd.type = BodyType.Dynamic;
	
	        actorDef.bd.linearDamping = 5.0f;
	        actorDef.bd.angularDamping = 3.0f;
	        actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
	        actorDef.bd.angle = angle;
	
	        actorDef.fixtureDefCount = 2;
	        actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
	
	        CircleShape shape = new CircleShape();
	        shape._radius = 2.0f;
            shape._p.X = 0;
            shape._p.Y = 0;

            actorDef.fds[kSharkHead] = new FixtureDef();
	        actorDef.fds[kSharkHead].density = 0.25f;
	        actorDef.fds[kSharkHead].shape = shape;
	        actorDef.fds[kSharkHead].isSensor = true;
            actorDef.fds[kSharkHead].filter.categoryBits = 0;

            shape = new CircleShape();
            shape._radius = ResManager.P2M(8.0f);
            shape._p.X = 0;
            shape._p.Y = 2.0f;

            actorDef.fds[kSharkNose] = new FixtureDef();
	        actorDef.fds[kSharkNose].density = 0.25f;
	        actorDef.fds[kSharkNose].shape = shape;
	        actorDef.fds[kSharkNose].isSensor = true;
	        actorDef.fds[kSharkNose].filter.categoryBits = COL_BIT_SHARK;
            actorDef.fds[kSharkNose].filter.maskBits = COL_BIT_OVERBOARD;
    
	        return actorDef;
        }

        public ActorDef CreatePersonOverboardDef(float x, float y, float angle)
        {
            ActorDef actorDef = new ActorDef();
	
	        actorDef.bd.type = BodyType.Dynamic;
	        actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
	        actorDef.bd.angle = angle;
	        actorDef.bd.linearDamping = 5.0f;
	        actorDef.bd.angularDamping = 3.0f;
	
	        actorDef.fixtureDefCount = 1;
	        actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
	
            CircleShape shape = new CircleShape();
	        shape._radius = ResManager.P2M(8.0f);
            shape._p.X = 0;
            shape._p.Y = 0;

            actorDef.fds[0] = new FixtureDef();
	        actorDef.fds[0].density = 1.0f;
	        actorDef.fds[0].shape = shape;
	        actorDef.fds[0].isSensor = true;
	        actorDef.fds[0].filter.categoryBits = COL_BIT_OVERBOARD;
            actorDef.fds[0].filter.maskBits = (COL_BIT_VOODOO | COL_BIT_CANNONBALL_CORE | COL_BIT_SHARK);
    
	        return actorDef;
        }

        public ActorDef CreatePowderKegDef(float x, float y, float angle)
        {
            ActorDef actorDef = new ActorDef();
	
	        actorDef.bd.type = BodyType.Dynamic;
	        actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
	        actorDef.bd.angle = angle;
	        actorDef.bd.linearDamping = 5.0f;
	        actorDef.bd.angularDamping = 3.0f;
	
	        actorDef.fixtureDefCount = 1;
	        actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
	
            CircleShape shape = new CircleShape();
	        shape._radius = ResManager.P2M(8.0f);
            shape._p.X = 0;
            shape._p.Y = 0;

            actorDef.fds[0] = new FixtureDef();
	        actorDef.fds[0].density = 1.0f;
	        actorDef.fds[0].shape = shape;
	        actorDef.fds[0].isSensor = true;
            actorDef.fds[0].filter.categoryBits = COL_BIT_VOODOO;
            actorDef.fds[0].filter.maskBits = (COL_BIT_VOODOO | COL_BIT_NPC_SHIP_HULL | COL_BIT_CANNONBALL_CORE | COL_BIT_OVERBOARD | COL_BIT_SK_SHIP_HULL);
    
	        // Need this to interact with itself
	        //actorDef->fds[0].filter.groupIndex = CGI_PLAYER_EXCLUDED;
	        return actorDef;
        }

        public ActorDef CreateNetDef(float x, float y, float angle, float scale)
        {
            ActorDef actorDef = new ActorDef();
	
	        actorDef.bd.type = BodyType.Dynamic;
	        actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
	        actorDef.bd.angle = angle;
	        actorDef.bd.linearDamping = 2;
	        actorDef.bd.angularDamping = 1;
	
	        actorDef.fixtureDefCount = 2;
	        actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
	
	        // Shrunk Fixture
            CircleShape shape = new CircleShape();
            shape._radius = ResManager.P2M(130.0f * scale * 0.15f);
            shape._p.X = 0;
            shape._p.Y = 0;

            actorDef.fds[0] = new FixtureDef();
	        actorDef.fds[0].density = 0.1f;
	        actorDef.fds[0].shape = shape;
	        actorDef.fds[0].isSensor = true;
	        actorDef.fds[0].filter.groupIndex = CGI_PLAYER_EXCLUDED; 
	        actorDef.fds[0].filter.categoryBits = COL_BIT_VOODOO;
            actorDef.fds[0].filter.maskBits = (COL_BIT_VOODOO | COL_BIT_NPC_SHIP_HULL | COL_BIT_SK_SHIP_HULL);
    
	        // Full-size fixture
            shape = new CircleShape();
	        shape._radius = ResManager.P2M(130.0f * scale);
            shape._p.X = 0;
            shape._p.Y = 0;
            actorDef.fds[1] = new FixtureDef();
	        actorDef.fds[1].density = 0.1f;
	        actorDef.fds[1].shape = shape;
	        actorDef.fds[1].isSensor = true;
	        actorDef.fds[1].filter.groupIndex = CGI_PLAYER_EXCLUDED;
            actorDef.fds[1].filter.categoryBits = COL_BIT_VOODOO;
            actorDef.fds[1].filter.maskBits = (COL_BIT_VOODOO | COL_BIT_NPC_SHIP_HULL | COL_BIT_SK_SHIP_HULL);

	        return actorDef;
        }

        public ActorDef CreateBrandySlickDef(float x, float y, float angle, float scale)
        {
            ActorDef actorDef = new ActorDef();
	
	        actorDef.bd.type = BodyType.Dynamic;
	        actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
	        actorDef.bd.angle = angle;
	
	        actorDef.fixtureDefCount = 1;
	        actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
	
            PolygonShape shape = new PolygonShape();
            shape.SetAsBox(0.75f * scale, 7.25f * scale, new Vector2(0, 0), 0);

            actorDef.fds[0] = new FixtureDef();
	        actorDef.fds[0].density = 1.0f;
	        actorDef.fds[0].shape = shape;
	        actorDef.fds[0].isSensor = true;
            actorDef.fds[0].filter.categoryBits = COL_BIT_VOODOO;
            actorDef.fds[0].filter.maskBits = (COL_BIT_VOODOO | COL_BIT_NPC_SHIP_STERN | COL_BIT_CANNONBALL_CORE | COL_BIT_OVERBOARD | COL_BIT_SK_SHIP_HULL);
    
	        // Need this to interact with powder kegs, so can't put in same exclusion group.
	        //actorDef->fds[0].filter.groupIndex = CGI_PLAYER_EXCLUDED;
	        return actorDef;
        }

        public ActorDef CreateTempestDef(float x, float y, float angle)
        {
            ActorDef actorDef = new ActorDef();
	
	        actorDef.bd.type = BodyType.Dynamic;
	        actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
	        actorDef.bd.angle = angle;
	
	        actorDef.fixtureDefCount = 1;
	        actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
	
            CircleShape shape = new CircleShape();
	        shape._radius = ResManager.P2M(16.0f);
            shape._p.X = 0;
            shape._p.Y = 0;

            actorDef.fds[0] = new FixtureDef();
	        actorDef.fds[0].density = 1.0f;
	        actorDef.fds[0].shape = shape;
	        actorDef.fds[0].isSensor = true;
            actorDef.fds[0].filter.categoryBits = COL_BIT_VOODOO;
            actorDef.fds[0].filter.maskBits = (COL_BIT_NPC_SHIP_HULL | COL_BIT_VOODOO | COL_BIT_OVERBOARD | COL_BIT_SK_SHIP_HULL);
	        return actorDef;
        }

        public ActorDef CreateWhirlpoolDef(float x, float y, float angle, float scale)
        {
            ActorDef actorDef = new ActorDef();
	        actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
	
	        actorDef.fixtureDefCount = 2;
	        actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
	
            CircleShape shape = new CircleShape();
            shape._radius = ResManager.P2M(320.0f * scale);
            shape._p.X = 0;
            shape._p.Y = 0;

            actorDef.fds[kPool] = new FixtureDef();
	        actorDef.fds[kPool].density = 1.0f;
	        actorDef.fds[kPool].shape = shape;
	        actorDef.fds[kPool].isSensor = true;
            actorDef.fds[kPool].filter.categoryBits = COL_BIT_VOODOO;
            actorDef.fds[kPool].filter.maskBits = (COL_BIT_NPC_SHIP_HULL | COL_BIT_VOODOO | COL_BIT_OVERBOARD);

            shape = new CircleShape();
            shape._radius = 0.75f * scale;
            shape._p.X = 0;
            shape._p.Y = 0;

            actorDef.fds[kEye] = new FixtureDef();
	        actorDef.fds[kEye].density = 1.0f;
	        actorDef.fds[kEye].shape = shape;
	        actorDef.fds[kEye].isSensor = true;
	        actorDef.fds[kEye].filter.categoryBits = COL_BIT_VOODOO;
            actorDef.fds[kEye].filter.maskBits = (COL_BIT_NPC_SHIP_HULL | COL_BIT_VOODOO | COL_BIT_OVERBOARD);
    
	        return actorDef;
        }

        public ActorDef CreateRaceTrackDefWithDictionary(Dictionary<string, object> dictionary)
        {
            List<object> checkpoints = dictionary["Checkpoints"] as List<object>;
	
	        int i = 0;
            float x, y, angle;
	        ActorDef actorDef = new ActorDef();
	        actorDef.bd.position.X = 0;
            actorDef.bd.position.Y = 0;

            actorDef.fixtureDefCount = checkpoints.Count + 1; // +1 for finishLine
            actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];
	
	        ResOffset offset = ResManager.RESM.ItemOffsetWithAlignment(ResManager.ResAlignment.Center);
	
	        for (i = 0; i < actorDef.fixtureDefCount - 1; ++i)
            {
		        Dictionary<string, object> checkpoint = checkpoints[i] as Dictionary<string, object>;
                x = 2 * Globals.ConvertToSingle(checkpoint["x"]);
                y = 2 * Globals.ConvertToSingle(checkpoint["y"]);
		
		        x += offset.X; y += offset.Y;
		
		        CircleShape circle = new CircleShape();
                circle._radius = ResManager.P2M(40.0f);
                circle._p.X = ResManager.P2MX(x);
                circle._p.Y = ResManager.P2MY(y);

                actorDef.fds[i] = new FixtureDef();
		        actorDef.fds[i].density = 0.0f;
                actorDef.fds[i].shape = circle;
		        actorDef.fds[i].isSensor = true;
                actorDef.fds[i].filter.categoryBits = COL_BIT_PLAYER_BUFF;
                actorDef.fds[i].filter.maskBits = COL_BIT_PLAYER_SHIP_HULL;
	        }
	
	        Dictionary<string, object> finishLine = dictionary["FinishLine"] as Dictionary<string, object>;
	        x = 2 * Globals.ConvertToSingle(finishLine["x"]);
	        y = 2 * Globals.ConvertToSingle(finishLine["y"]);
	        angle = Globals.ConvertToSingle(finishLine["rotation"]);
	
	        x += offset.X; y += offset.Y;

            PolygonShape poly = new PolygonShape();
            poly.SetAsBox(ResManager.P2M(40.0f), ResManager.P2M(16.0f), new Vector2(ResManager.P2MX(x), ResManager.P2MY(y)), angle);

            actorDef.fds[i] = new FixtureDef();
	        actorDef.fds[i].density = 0.0f;
            actorDef.fds[i].shape = poly;
	        actorDef.fds[i].isSensor = true;
            actorDef.fds[i].filter.categoryBits = COL_BIT_PLAYER_BUFF;
            actorDef.fds[i].filter.maskBits = COL_BIT_PLAYER_SHIP_HULL;

	        return actorDef;
        }
    }
}
