using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    /// <summary>
    /// A simple vertex that only contains the position
    /// </summary>
    public struct VertexCube : IVertexType
    {
        public Vector3 Position;

        public static VertexElement[] Elements = { new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0) };

        public VertexCube(Vector3 pos)
        {
            Position = pos;
        }

        private static VertexDeclaration _vd = null;
        public VertexDeclaration VertexDeclaration
        {
            get
            {
                if (_vd == null)
                    _vd = new VertexDeclaration(Elements);
                return _vd;
            }
        }
        public static int SizeInBytes = (3) * sizeof(float);

    }

    /// <summary>
    /// A class to hold skybox geometry
    /// </summary>
    public class SkyBox : SPDisplayObject
    {
        private static VertexCube[] CubeMdl = null;
        private static short[] CubeInd = null;

        public SkyBox()
        {
            // generate the geometry...
            if (CubeMdl == null)
            {
                float dist = 10;
                CubeMdl = new VertexCube[8];
                CubeInd = new short[6 * 6];

                CubeMdl[0] = new VertexCube(new Vector3(-dist, -dist, -dist));
                CubeMdl[1] = new VertexCube(new Vector3(dist, -dist, -dist));
                CubeMdl[2] = new VertexCube(new Vector3(-dist, dist, -dist));
                CubeMdl[3] = new VertexCube(new Vector3(dist, dist, -dist));

                CubeMdl[4] = new VertexCube(new Vector3(-dist, -dist, dist));
                CubeMdl[5] = new VertexCube(new Vector3(dist, -dist, dist));
                CubeMdl[6] = new VertexCube(new Vector3(-dist, dist, dist));
                CubeMdl[7] = new VertexCube(new Vector3(dist, dist, dist));

                CubeInd[0] = 0;
                CubeInd[1] = 1;
                CubeInd[2] = 2;
                CubeInd[3] = 1;
                CubeInd[4] = 2;
                CubeInd[5] = 3;

                CubeInd[6] = 4;
                CubeInd[7] = 5;
                CubeInd[8] = 6;
                CubeInd[9] = 5;
                CubeInd[10] = 6;
                CubeInd[11] = 7;

                CubeInd[12] = 0;
                CubeInd[13] = 4;
                CubeInd[14] = 2;
                CubeInd[15] = 4;
                CubeInd[16] = 2;
                CubeInd[17] = 6;

                CubeInd[18] = 1;
                CubeInd[19] = 5;
                CubeInd[20] = 3;
                CubeInd[21] = 5;
                CubeInd[22] = 3;
                CubeInd[23] = 7;

                CubeInd[24] = 0;
                CubeInd[25] = 1;
                CubeInd[26] = 4;
                CubeInd[27] = 1;
                CubeInd[28] = 4;
                CubeInd[29] = 5;

                CubeInd[30] = 2;
                CubeInd[31] = 3;
                CubeInd[32] = 6;
                CubeInd[33] = 3;
                CubeInd[34] = 6;
                CubeInd[35] = 7;
            }
        }

        public override void Draw(GameTime gameTime, SPRenderSupport support, Matrix parentTransform)
        {
            support.GraphicsDevice.DrawUserIndexedPrimitives<VertexCube>(PrimitiveType.TriangleList, CubeMdl, 0, 8, CubeInd, 0, 12);
        }
    };
}
