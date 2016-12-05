using System;
using System.Collections.Generic;
using Box2D.XNA;

namespace AwesomePirates
{
    class ActorDef
    {
        public ActorDef()
        {
            fixtureDefCount = 0;
            fds = null;
            fixtures = null;
            bd = new BodyDef();
        }

        public int fixtureDefCount;
        public BodyDef bd;
        public FixtureDef[] fds;
        public Fixture[] fixtures;

        public void ResetFixtures()
        {
            if (fixtures != null)
            {
                for (int i = 0; i < fixtures.Length; ++i)
                    fixtures[i] = null;
            }
        }
    }
}
