using System;
using System.Collections.Generic;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class ShipFactory : ActorFactory
    {
        private static ShipFactory s_Factory = null;

        private ShipFactory()
        {
            mShipDetails = PlistParser.DictionaryFromPlist("data/plists/ShipDetails.plist");
            mNpcShipDetails = PlistParser.DictionaryFromPlist("data/plists/NpcShipDetails.plist");
            mShipActors = PlistParser.DictionaryFromPlist("data/plists/ShipActors.plist");
            mPrisoners = PlistParser.DictionaryFromPlist("data/plists/Prisoners.plist");

            if (ShipDetails == null || NpcShipDetails == null || ShipActors == null || Prisoners == null)
                throw new NotSupportedException("Could not load ShipFactory plist support files.");
        }

        public static ShipFactory Factory
        {
            get
            {
                if (s_Factory == null)
                    s_Factory = new ShipFactory();
                return s_Factory;
            }
        }

        private Dictionary<string, object> mShipDetails;
        private Dictionary<string, object> mNpcShipDetails;
        private Dictionary<string, object> mShipActors;
        private Dictionary<string, object> mPrisoners;

        private Dictionary<string, object> ShipDetails { get { return mShipDetails; } }
        private Dictionary<string, object> NpcShipDetails { get { return mNpcShipDetails; } }
        private Dictionary<string, object> ShipActors { get { return mShipActors; } }
        private Dictionary<string, object> Prisoners { get { return mPrisoners; } }

        public List<string> AllShipTypes { get { return new List<string>(mShipDetails.Keys); } }
        public Dictionary<string, object> AllShipDetails { get { return mShipDetails; } }
        public List<string> AllNpcShipTypes { get { return new List<string>(mNpcShipDetails.Keys); } }
        public Dictionary<string, object> AllNpcShipDetails { get { return mNpcShipDetails; } }
        public Dictionary<string, object> AllPrisoners { get { return new Dictionary<string, object>(mPrisoners); } }
        public List<string> AllPrisonerNames { get { return new List<string>(mPrisoners.Keys); } }

        public Prisoner CreatePrisonerForName(string name)
        {
            Prisoner prisoner = new Prisoner(name);
            Dictionary<string, object> dict = mPrisoners[name] as Dictionary<string, object>;
            prisoner.Gender = (Gender)Convert.ToInt32(dict["gender"]);
            prisoner.TextureName = dict["textureName"] as string;
            prisoner.InfamyBonus = Convert.ToInt32(dict["infamyBonus"]);
            return prisoner;
        }

        private ShipDetails CreateShipDetailsFromDictionary(Dictionary<string, object> dictionary, string shipType)
        {
            ShipDetails shipDetails = new ShipDetails(shipType);
            Dictionary<string, object> dict = dictionary[shipType] as Dictionary<string, object>;

            if (dict.ContainsKey("speedRating"))
                shipDetails.SpeedRating = Convert.ToInt32(dict["speedRating"]);
            if (dict.ContainsKey("controlRating"))
                shipDetails.ControlRating = Convert.ToInt32(dict["controlRating"]);
            if (dict.ContainsKey("rudderOffset"))
                shipDetails.RudderOffset = Globals.ConvertToSingle(dict["rudderOffset"]);
            if (dict.ContainsKey("reloadInterval"))
                shipDetails.ReloadInterval = Globals.ConvertToSingle(dict["reloadInterval"]);
            if (dict.ContainsKey("infamyBonus"))
                shipDetails.InfamyBonus = Convert.ToInt32(dict["infamyBonus"]);
            if (dict.ContainsKey("mutinyPenalty"))
                shipDetails.MutinyPenalty = Convert.ToInt32(dict["mutinyPenalty"]);
            if (dict.ContainsKey("textureName"))
                shipDetails.TextureName = dict["textureName"] as string;
            if (dict.ContainsKey("textureFutureName"))
                shipDetails.TextureFutureName = dict["textureFutureName"] as string;
            if (dict.ContainsKey("bitmap"))
                shipDetails.Bitmap = Convert.ToUInt32(dict["bitmap"]);

            return shipDetails;
        }

        public ShipDetails CreateShipDetailsForType(string shipType)
        {
            return CreateShipDetailsFromDictionary(mShipDetails, shipType);
        }

        public ShipDetails CreateNpcShipDetailsForType(string shipType)
        {
            return CreateShipDetailsFromDictionary(mNpcShipDetails, shipType);
        }

        public ActorDef CreatePlayerShipDefForShipType(string shipType, float x, float y, float angle)
        {
            Dictionary<string, object> dict = mShipActors[shipType] as Dictionary<string, object>;
            Dictionary<string, object> prev = dict;
            ActorDef actorDef = new ActorDef();
            Int16 filterGroup = (Int16)Convert.ToInt32(dict["filterGroup"]);

            dict = dict["B2BodyDef"] as Dictionary<string, object>;

            actorDef.bd.type = BodyType.Dynamic;
            actorDef.bd.linearDamping = Globals.ConvertToSingle(dict["linearDamping"]);
            actorDef.bd.angularDamping = Globals.ConvertToSingle(dict["angularDamping"]);
            actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
            actorDef.bd.angle = angle;

            dict = prev;
            int index = 0;
            List<object> array = dict["B2Fixtures"] as List<object>;

            actorDef.fixtureDefCount = array.Count;
            actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];

            foreach (Dictionary<string, object> iter in array)
            {
                dict = iter["B2FixtureDef"] as Dictionary<string, object>;

                actorDef.fds[index] = new FixtureDef();
                actorDef.fds[index].density = Globals.ConvertToSingle(dict["density"]);
                actorDef.fds[index].friction = Globals.ConvertToSingle(dict["friction"]);
                actorDef.fds[index].isSensor = Convert.ToBoolean(dict["isSensor"]);
                actorDef.fds[index].filter.groupIndex = filterGroup;

                switch (index)
                {
                    case 0: // Bow
                    case 1: // Middle
                    case 2: // Stern
                        actorDef.fds[index].filter.categoryBits = (COL_BIT_DEFAULT | COL_BIT_PLAYER_SHIP_HULL);
                        actorDef.fds[index].filter.maskBits = (COL_BIT_DEFAULT | COL_BIT_PLAYER_BUFF | COL_BIT_NPC_SHIP_DEFENDER | COL_BIT_NPC_SHIP_HULL);
                        break;
                    case 3: // Left Cannon
                    case 4: // Right Cannon
                        actorDef.fds[index].filter.categoryBits = 0;
                        break;
                    default:
                        break;
                }

                dict = iter["B2Shape"] as Dictionary<string, object>;
                ShapeType shapeType = (ShapeType)Convert.ToInt32(dict["type"]);
                actorDef.fds[index].shape = CreateShapeForType(shapeType, dict);
                ++index;
            }

            return actorDef;
        }

        public ActorDef CreateSkirmishShipDefForShipType(string shipType, float x, float y, float angle)
        {
            Dictionary<string, object> dict = mShipActors[shipType] as Dictionary<string, object>;
            Dictionary<string, object> prev = dict;
            ActorDef actorDef = new ActorDef();
            Int16 filterGroup = (Int16)Convert.ToInt32(dict["filterGroup"]);

            dict = dict["B2BodyDef"] as Dictionary<string, object>;

            actorDef.bd.type = BodyType.Dynamic;
            actorDef.bd.linearDamping = Globals.ConvertToSingle(dict["linearDamping"]);
            actorDef.bd.angularDamping = Globals.ConvertToSingle(dict["angularDamping"]);
            actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
            actorDef.bd.angle = angle;

            dict = prev;
            int index = 0;
            List<object> array = dict["B2Fixtures"] as List<object>;

            actorDef.fixtureDefCount = array.Count;
            actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];

            foreach (Dictionary<string, object> iter in array)
            {
                dict = iter["B2FixtureDef"] as Dictionary<string, object>;

                actorDef.fds[index] = new FixtureDef();
                actorDef.fds[index].density = Globals.ConvertToSingle(dict["density"]);
                actorDef.fds[index].friction = Globals.ConvertToSingle(dict["friction"]);
                actorDef.fds[index].isSensor = Convert.ToBoolean(dict["isSensor"]);
                actorDef.fds[index].filter.groupIndex = filterGroup;

#if true
                switch (index)
                {
                    case 0: // Bow
                    case 1: // Middle
                    case 2: // Stern
                        actorDef.fds[index].filter.categoryBits = (COL_BIT_DEFAULT | COL_BIT_SK_SHIP_HULL);
                        actorDef.fds[index].filter.maskBits = (COL_BIT_DEFAULT | COL_BIT_PLAYER_BUFF | COL_BIT_NPC_SHIP_DEFENDER |
                            COL_BIT_NPC_SHIP_HULL | COL_BIT_VOODOO | COL_BIT_SK_SHIP_HULL | COL_BIT_CANNONBALL_CONE);
                        break;
                    case 3: // Left Cannon
                    case 4: // Right Cannon
                        actorDef.fds[index].filter.categoryBits = 0;
                        break;
                    case 5: // Hit Box
                        actorDef.fds[index].filter.categoryBits = 0;
                        break;
                    default:
                        break;
                }
#else
                switch (index)
                {
                    case 0: // Bow
                    case 1: // Middle
                        actorDef.fds[index].filter.categoryBits = (COL_BIT_DEFAULT | COL_BIT_NPC_SHIP_HULL | COL_BIT_SK_SHIP_HULL);
                        actorDef.fds[index].filter.maskBits = 0xffff;
                        break;
                    case 2: // Stern
                        actorDef.fds[index].filter.categoryBits = (COL_BIT_DEFAULT | COL_BIT_NPC_SHIP_HULL | COL_BIT_NPC_SHIP_STERN | COL_BIT_SK_SHIP_HULL);
                        actorDef.fds[index].filter.maskBits = 0xffff;
                        break;
                    case 3: // Left Cannon
                    case 4: // Right Cannon
                        actorDef.fds[index].filter.categoryBits = 0;
                        break;
                    case 5: // Hit Box
                        actorDef.fds[index].filter.categoryBits = 0;
                        break;
                    default:
                        break;
                }
#endif
                dict = iter["B2Shape"] as Dictionary<string, object>;
                ShapeType shapeType = (ShapeType)Convert.ToInt32(dict["type"]);
                actorDef.fds[index].shape = CreateShapeForType(shapeType, dict);
                ++index;
            }

            return actorDef;
        }

        public ActorDef CreateShipDefForShipType(string shipType, float x, float y, float angle)
        {
            Dictionary<string, object> dict = mShipActors[shipType] as Dictionary<string, object>;
            Dictionary<string, object> prev = dict;
            ActorDef actorDef = new ActorDef();
            Int16 filterGroup = (Int16)Convert.ToInt32(dict["filterGroup"]);

            dict = dict["B2BodyDef"] as Dictionary<string, object>;

            actorDef.bd.type = BodyType.Dynamic;
            actorDef.bd.linearDamping = Globals.ConvertToSingle(dict["linearDamping"]);
            actorDef.bd.angularDamping = Globals.ConvertToSingle(dict["angularDamping"]);
            actorDef.bd.position.X = x;
            actorDef.bd.position.Y = y;
            actorDef.bd.angle = angle;

            dict = prev;
            int index = 0;
            List<object> array = dict["B2Fixtures"] as List<object>;

            actorDef.fixtureDefCount = array.Count;
            actorDef.fds = new FixtureDef[actorDef.fixtureDefCount];

            foreach (Dictionary<string, object> iter in array)
            {
                dict = iter["B2FixtureDef"] as Dictionary<string, object>;

                actorDef.fds[index] = new FixtureDef();
                actorDef.fds[index].density = Globals.ConvertToSingle(dict["density"]);
                actorDef.fds[index].friction = Globals.ConvertToSingle(dict["friction"]);
                actorDef.fds[index].isSensor = Convert.ToBoolean(dict["isSensor"]);
                actorDef.fds[index].filter.groupIndex = filterGroup;

                switch (index)
                {
                    case 0: // Bow
                    case 1: // Middle
                        actorDef.fds[index].filter.categoryBits = (COL_BIT_DEFAULT | COL_BIT_NPC_SHIP_HULL);
                        actorDef.fds[index].filter.maskBits = 0xffff;
                        break;
                    case 2: // Stern
                        actorDef.fds[index].filter.categoryBits = (COL_BIT_DEFAULT | COL_BIT_NPC_SHIP_HULL | COL_BIT_NPC_SHIP_STERN);
                        actorDef.fds[index].filter.maskBits = 0xffff;
                        break;
                    case 3: // Left Cannon
                    case 4: // Right Cannon
                        actorDef.fds[index].filter.categoryBits = 0;
                        break;
                    case 5: // Hit Box
                        actorDef.fds[index].filter.categoryBits = 0;
                        break;
                    case 6: // Feeler
                        actorDef.fds[index].filter.categoryBits = COL_BIT_NPC_SHIP_FEELER;
                        actorDef.fds[index].filter.maskBits = (COL_BIT_NPC_SHIP_HULL | COL_BIT_NPC_SHIP_FEELER);
                        break;
                    //case 7: // Defender
                    //    actorDef.fds[index].filter.categoryBits = COL_BIT_NPC_SHIP_DEFENDER;
                    //    actorDef.fds[index].filter.maskBits = (COL_BIT_PLAYER_SHIP_HULL | COL_BIT_SK_SHIP_HULL);
                    //    break;
                    default:
                        break;
                }

                dict = iter["B2Shape"] as Dictionary<string, object>;
                ShapeType shapeType = (ShapeType)Convert.ToInt32(dict["type"]);
                actorDef.fds[index].shape = CreateShapeForType(shapeType, dict);
                ++index;
            }

            return actorDef;
        }
    }
}
