using System;
using System.Collections.Generic;
using Box2D.XNA;

namespace AwesomePirates
{
    class ActorContactListener : IContactListener
    {
        public void BeginContact(Contact contact)
        {
            Fixture fixtureA = contact.GetFixtureA();
            Fixture fixtureB = contact.GetFixtureB();

            Body bodyA = fixtureA.GetBody();
            Body bodyB = fixtureB.GetBody();

            Actor actorA = bodyA.GetUserData() as Actor;
            Actor actorB = bodyB.GetUserData() as Actor;

            actorA.BeginContact(actorB, fixtureA, fixtureB, contact);
            actorB.BeginContact(actorA, fixtureB, fixtureA, contact);
        }

        public void EndContact(Contact contact)
        {
            Fixture fixtureA = contact.GetFixtureA();
            Fixture fixtureB = contact.GetFixtureB();

            Body bodyA = fixtureA.GetBody();
            Body bodyB = fixtureB.GetBody();

            Actor actorA = bodyA.GetUserData() as Actor;
            Actor actorB = bodyB.GetUserData() as Actor;

            actorA.EndContact(actorB, fixtureA, fixtureB, contact);
            actorB.EndContact(actorA, fixtureB, fixtureA, contact);
        }

        public void PreSolve(Contact contact, ref Manifold oldManifold)
        {
            Fixture fixtureA = contact.GetFixtureA();
            Fixture fixtureB = contact.GetFixtureB();

            Body bodyA = fixtureA.GetBody();
            Body bodyB = fixtureB.GetBody();

            Actor actorA = bodyA.GetUserData() as Actor;
            Actor actorB = bodyB.GetUserData() as Actor;

            bool enabled = actorA.PreSolve(actorB, fixtureA, fixtureB, contact);
            enabled = enabled && actorB.PreSolve(actorA, fixtureB, fixtureA, contact);
            contact.SetEnabled(enabled);
        }

        public void PostSolve(Contact contact, ref ContactImpulse impulse)
        {
            // Do nothing
        }
    }
}
