using System;
using System.Collections.Generic;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class StaticFactory : ActorFactory
    {
        private static StaticFactory s_Factory = null;

        private StaticFactory()
        {
            mActorDictionary = PlistParser.DictionaryFromPlist("data/plists/StaticActors.plist");

            if (mActorDictionary == null)
                throw new NotSupportedException("Could not load StaticFactory plist support files.");
        }

        public static StaticFactory Factory
        {
            get
            {
                if (s_Factory == null)
                    s_Factory = new StaticFactory();
                return s_Factory;
            }
        }

        private Dictionary<string, object> mActorDictionary;
        private Dictionary<string, object> ActorDictionary { get { return mActorDictionary; } }

        public ActorDef CreateBeachActorDef()
        {
            return CreateActorDefWithKey("Beach");
        }

        public ActorDef CreateTownActorDef()
        {
            return CreateActorDefWithKey("Town");
        }

        private ActorDef CreateActorDefWithKey(string key)
        {
            if (key == null || !s_Factory.ActorDictionary.ContainsKey(key))
                throw new ArgumentException("Bad actor key in StaticFactory.");

            Dictionary<string, object> dictionary = ActorDictionary[key] as Dictionary<string, object>;
            Dictionary<string, object> dict = dictionary["B2BodyDef"] as Dictionary<string, object>;
            float x = Globals.ConvertToSingle(dict["x"]);
            float y = Globals.ConvertToSingle(dict["y"]);
            float angle = Globals.ConvertToSingle(dict["rotation"]);

            x = ResManager.RITMFX(x); y = ResManager.RITMFY(y);

            ActorDef actorDef = new ActorDef();
            actorDef.bd.position.X = x; actorDef.bd.position.Y = y;
            actorDef.bd.angle = angle;

            List<object> array = dictionary["B2Fixtures"] as List<object>;
            actorDef.fixtureDefCount = array.Count;
            actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];

            int index = 0;

            foreach (Dictionary<string, object> d in array)
            {
                Dictionary<string, object> iter = d["B2FixtureDef"] as Dictionary<string, object>;

                actorDef.fds[index] = new FixtureDef();
                actorDef.fds[index].density = 0f;
                actorDef.fds[index].friction = Globals.ConvertToSingle(iter["friction"]);
                actorDef.fds[index].restitution = Globals.ConvertToSingle(iter["restitution"]);
                actorDef.fds[index].isSensor = Convert.ToBoolean(iter["isSensor"]);

                iter = d["B2Shape"] as Dictionary<string, object>;
                ShapeType shapeType = (ShapeType)Convert.ToInt32(iter["type"]);
                actorDef.fds[index].shape = CreateShapeForType(shapeType, iter);
                ++index;
            }

            return actorDef;
        }
    }
}
