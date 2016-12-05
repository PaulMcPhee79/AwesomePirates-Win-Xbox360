using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    class AiKnob
    {
        public int merchantShipsMin;
        // Max concurrently active
        public int merchantShipsMax;
        public int pirateShipsMax;
        public int navyShipsMax;

        // Spawn chance per think interval
        public int merchantShipsChance;
        public int pirateShipsChance;
        public int navyShipsChance;
        public int specialShipsChance;

        // Fleet timer
        public bool fleetShouldSpawn;
        public double fleetTimer;

        // Game level attributes and factors
        public int difficulty;				// Difficulty level: gives finer granularity to state changes. Currently increased via TimeOfDay changes in PlayfieldController.
        public int difficultyIncrement;	    // Baseline increase for difficulty changes.
        public float difficultyFactor;		// Scales difficulty changes;
        public float aiModifier;			// Multiplier / Divider for actor speed, control, cannon accuracy. 
        public int stateCeiling;			// Once difficulty surpasses this value, we advance to the next state.
        public int state;					// Initiates stepped variable transitions.
    }
}
