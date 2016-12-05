using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Box2D.XNA;

namespace AwesomePirates
{
    class ActorFactory
    {
        protected enum ShapeType
        {
            Circle = 0,
            Box,
            Edge,
            Poly
        }

        protected const Int16 CGI_PLAYER_EXCLUDED = -1;
        protected const Int16 CGI_ENEMY_EXCLUDED = -2;
        protected const Int16 CGI_CANNONBALLS = -3;
        
        protected const UInt16 COL_BIT_DEFAULT = 0x0001;
        protected const UInt16 COL_BIT_PLAYER_SHIP_HULL = 0x0002;
        protected const UInt16 COL_BIT_NPC_SHIP_HULL = 0x0004;
        protected const UInt16 COL_BIT_NPC_SHIP_FEELER = 0x0008;
        protected const UInt16 COL_BIT_NPC_SHIP_DEFENDER = 0x0010;
        protected const UInt16 COL_BIT_CANNONBALL_CORE = 0x0020;
        protected const UInt16 COL_BIT_CANNONBALL_CONE = 0x0040;
        protected const UInt16 COL_BIT_VOODOO = 0x0080;
        protected const UInt16 COL_BIT_OVERBOARD = 0x0100;
        protected const UInt16 COL_BIT_SHARK = 0x0200;
        protected const UInt16 COL_BIT_PLAYER_BUFF = 0x0400;
        protected const UInt16 COL_BIT_NPC_SHIP_STERN = 0x0800;
        protected const UInt16 COL_BIT_SK_SHIP_HULL = 0x1000;

        protected static Shape CreateShapeForType(ShapeType shapeType, Dictionary<string, object> dict)
        {
            Shape shape = null;

            switch (shapeType)
            {
                case ShapeType.Circle:
                    {
                        CircleShape circle = new CircleShape();
                        circle._radius = Globals.ConvertToSingle(dict["radius"]);
                        circle._p.X = Globals.ConvertToSingle(dict["x"]);
                        circle._p.Y = Globals.ConvertToSingle(dict["y"]);
                        shape = circle;
                    }
                    break;
                case ShapeType.Box:
                    {
                        PolygonShape box = new PolygonShape();
                        float x = Globals.ConvertToSingle(dict["x"]);
                        float y = Globals.ConvertToSingle(dict["y"]);
                        float hw = Globals.ConvertToSingle(dict["hw"]);
                        float hh = Globals.ConvertToSingle(dict["hh"]);
                        float rotation =  Globals.ConvertToSingle(dict["rotation"]);
                        box.SetAsBox(hw, hh, new Vector2(x, y), rotation);
                        shape = box;
                    }
                    break;
                case ShapeType.Edge:
                    {
                        EdgeShape edge = new EdgeShape();
                        float v1x = Globals.ConvertToSingle(dict["v1x"]);
                        float v1y = Globals.ConvertToSingle(dict["v1y"]);
                        float v2x = Globals.ConvertToSingle(dict["v2x"]);
                        float v2y = Globals.ConvertToSingle(dict["v2y"]);
                        edge.Set(new Vector2(v1x, v2x), new Vector2(v2x, v2y));
                        shape = edge;
                    }
                    break;
                case ShapeType.Poly:
                    {
                        PolygonShape poly = new PolygonShape();

                        List<object> array = dict["vertices"] as List<object>;
                        Vector2[] vertices = new Vector2[8];
                        int vertexCount = Math.Min(8, array.Count / 2);

                        for (int i = 0; i < vertexCount; ++i)
                        {
                            vertices[i].X = Globals.ConvertToSingle(array[i*2]);
                            vertices[i].Y = Globals.ConvertToSingle(array[i*2+1]);
                        }

                        poly.Set(vertices, vertexCount);
                        shape = poly;
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid shape type requested in ActorFactory.");
            }

            return shape;
        }
    }
}
