using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SparrowXNA;
using Box2D.XNA;

namespace AwesomePirates
{
    class RaceTrackActor : Actor
    {
        private enum RaceTrackState
        {
            Stopped = 0,
            Grid,
            Running
        }

        public const string CUST_EVENT_TYPE_88MPH = "88MphEvent";

        public RaceTrackActor(ActorDef def, int laps)
            : base(def)
        {
            mCategory = (int)PFCat.SEA;
		    mAdvanceable = true;
		    mFinishLine = null;
            mLapsPerRace = laps;
		    mBuoys = new List<SPImage>(88);
            mRacers = new SPHashSet<Racer>();
		
		    // Save checkpoints to compare in collision processing
		    mCheckpointCount = def.fixtureDefCount - 1;
            mCheckpoints = new List<SPImage>(mCheckpointCount);
            mCheckpointFixtures = new Fixture[def.fixtures.Length];

            for (int i = 0; i < def.fixtureDefCount; ++i)
                mCheckpointFixtures[i] = def.fixtures[i];

		    mFinishLineFixture = def.fixtures[def.fixtureDefCount-1];
		    mState = RaceTrackState.Stopped;
        }
        
        #region Fields
        private RaceTrackState mState;
        private int mLapsPerRace;
        private SPSprite mFinishLine;
        private List<SPImage> mBuoys;
        private List<SPImage> mCheckpoints;
        private SPHashSet<Racer> mRacers;

        private int mCheckpointCount;
        private Fixture[] mCheckpointFixtures;
        private Fixture mFinishLineFixture;
        #endregion

        #region Methods
        public void SetupRaceTrackWithDictionary(Dictionary<string, object> dictionary)
        {
            List<object> buoys = dictionary["Buoys"] as List<object>;
            List<object> checkpoints = dictionary["Checkpoints"] as List<object>;
            Dictionary<string, object> finishLine = dictionary["FinishLine"] as Dictionary<string, object>;

            SetupPerimeterBuoys(buoys);
            SetupCheckpoints(checkpoints);
            SetupFinishLine(finishLine);

            ResManager.RESM.PushItemOffsetWithAlignment(ResManager.ResAlignment.Center);
            X = ResManager.RESX(X);
            Y = ResManager.RESY(Y);
            ResManager.RESM.PopOffset();
        }

        private void SetupFinishLine(Dictionary<string, object> finishLine)
        {
            bool colorSwitch = true; // For checkered flag
	        SPSprite sprite = new SPSprite();
	        mFinishLine = new SPSprite();
	
	        for (int i = 0; i < 16; i+=8)
            {
		        for (int j = 0; j < 40; j+=8)
                {
			        SPQuad quad = new SPQuad(16f, 16f);
			        quad.X = 2 * j;
			        quad.Y = 2 * i;
			        quad.Color = (colorSwitch) ? Color.White : Color.Black;
                    sprite.AddChild(quad);
			        colorSwitch = !colorSwitch;
		        }
	        }
	        sprite.X = -sprite.Width / 2;
	        sprite.Y = -sprite.Height / 2;
	        mFinishLine.X = 2 * Globals.ConvertToSingle(finishLine["x"]);
	        mFinishLine.Y = 2 * Globals.ConvertToSingle(finishLine["y"]);
	        mFinishLine.Rotation = SPMacros.SP_D2R(Globals.ConvertToSingle(finishLine["rotation"]));
            mFinishLine.AddChild(sprite);
            AddChild(mFinishLine);
        }

        private void SetupPerimeterBuoys(List<object> buoys)
        {
            if (mFinishLine != null)
		        return;
	        SPTexture buoyTexture = mScene.TextureByName("buoy");
	
	        foreach (Dictionary<string, object> dict in buoys)
            {
		        SPImage image = new SPImage(buoyTexture);
                image.X = 2 * Globals.ConvertToSingle(dict["x"]);
                image.Y = 2 * Globals.ConvertToSingle(dict["y"]);
                mBuoys.Add(image);
                AddChild(image);
	        }
        }

        private void SetupCheckpoints(List<object> checkpoints)
        {
            if (mCheckpoints.Count > 0)
		        return;
	        SPTexture arrowTexture = mScene.TextureByName("race-arrow");
	
            foreach (Dictionary<string, object> dict in checkpoints)
            {
		        SPSprite sprite = new SPSprite();
		        SPImage image = new SPImage(arrowTexture);
		        image.X = -image.Width / 2;
		        image.Y = -image.Height / 2;
                mCheckpoints.Add(image);
                sprite.AddChild(image);

                sprite.X = 2 * Globals.ConvertToSingle(dict["x"]);
                sprite.Y = 2 * Globals.ConvertToSingle(dict["y"]);
		        sprite.Rotation = SPMacros.SP_D2R(Globals.ConvertToSingle(dict["rotation"]));
		        AddChild(sprite);
	        }
        }

        private void SetState(RaceTrackState state)
        {
            switch (state)
            {
                case RaceTrackState.Stopped:
                    {
                        if (mRacers != null)
                        {
                            List<Racer> racers = new List<Racer>(mRacers.EnumerableSet);

                            foreach (Racer racer in racers)
                            {
                                if (racer.Owner is PlayerShip && racer.RaceTime <= RaceEvent.RequiredRaceTimeForLapCount(racer.TotalLaps) && racer.FinishedRace)
                                    DispatchEvent(SPEvent.SPEventWithType(CUST_EVENT_TYPE_88MPH));
                            }
                        }
                    }
                    break;
		        case RaceTrackState.Grid:
			        foreach (Racer racer in mRacers.EnumerableSet)
                        racer.PrepareForNewRace();
			        break;
		        case RaceTrackState.Running:
			        foreach (Racer racer in mRacers.EnumerableSet)
                        racer.StartRace();
			        break;
	        }

	        mState = state;
        }

        public void PrepareForNewRace()
        {
            foreach (SPImage checkpoint in mCheckpoints)
                UnmarkCheckpoint(checkpoint);
            SetState(RaceTrackState.Grid);
        }

        public void StopRace()
        {
            foreach (SPImage checkpoint in mCheckpoints)
                UnmarkCheckpoint(checkpoint);
            SetState(RaceTrackState.Stopped);
        }

        public void PrepareForNewLap()
        {
            if (mCheckpoints != null && mCheckpoints.Count > 0)
            {
                SPImage checkpoint = mCheckpoints[0];
                MarkCheckpoint(checkpoint);
            }
        }

        private void MarkCheckpointAtIndex(int index)
        {
            if (mCheckpoints != null && index >= 0 && index < mCheckpoints.Count)
                MarkCheckpoint(mCheckpoints[index]);
        }

        private void MarkCheckpoint(SPImage checkpoint)
        {
            checkpoint.Color = SPUtils.ColorFromColor(0x00ff00);
        }

        private void UnmarkCheckpointAtIndex(int index)
        {
            if (mCheckpoints != null && index >= 0 && index < mCheckpoints.Count)
                UnmarkCheckpoint(mCheckpoints[index]);
        }

        private void UnmarkCheckpoint(SPImage checkpoint)
        {
            checkpoint.Color = Color.Red;
        }

        public void AddRacer(ShipActor ship)
        {
            if (mCheckpoints != null && mRacers != null && ship != null && ContainsRacer(ship) == null)
            {
		        Racer racer = new Racer(ship, mLapsPerRace, mCheckpoints.Count);
                mRacers.Add(racer);
                racer.BroadcastRaceUpdate();
	        }
        }

        private Racer ContainsRacer(ShipActor ship)
        {
            Racer foundRacer = null;
	
	        foreach (Racer racer in mRacers.EnumerableSet)
            {
		        if (racer.Owner == ship)
                {
			        foundRacer = racer;
			        break;
		        }
	        }

	        return foundRacer;
        }

        public void RemoveRacer(ShipActor ship)
        {
            if (ship == null || mRacers == null)
                return;

            Racer removeRacer = null;
	
	        foreach (Racer racer in mRacers.EnumerableSet)
            {
		        if (racer.Owner == ship)
                {
			        removeRacer = racer;
			        break;
		        }
	        }
	
	        if (removeRacer != null)
                mRacers.Remove(removeRacer);
        }

        private int FindCheckpointIndex(Fixture fixture)
        {
            int index = -1;

            for (int i = 0; i < mCheckpointCount; ++i)
            {
                if (fixture == mCheckpointFixtures[i])
                {
                    index = i;
                    break;
                }
            }
            //NSLog(@"Checkpoint Index: %d", index);
            return index;
        }

        public override void BeginContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (mRacers == null)
                return;

            Racer racer = ContainsRacer(other as ShipActor);

            if (racer != null && mState == RaceTrackState.Running && fixtureSelf != mFinishLineFixture)
            {
		        int index = FindCheckpointIndex(fixtureSelf);
		        int checkpoint = racer.CheckpointReached(index);
		
		        if (index != checkpoint)
                {
                    MarkCheckpointAtIndex(checkpoint);
                    UnmarkCheckpointAtIndex(racer.PrevCheckpoint);
		        }
	        }
        }

        public override void EndContact(Actor other, Fixture fixtureSelf, Fixture fixtureOther, Contact contact)
        {
            if (mRacers == null)
                return;

            Racer racer = ContainsRacer(other as ShipActor);
	
	        if (racer != null && mState != RaceTrackState.Stopped && fixtureSelf == mFinishLineFixture)
            {
		        if (mState == RaceTrackState.Grid)
                {
                    SetState(RaceTrackState.Running);
		        }
                else
                {
			        bool racing = racer.FinishLineCrossed();
			
			        if (!racing)
                        SetState(RaceTrackState.Stopped);
			        else
                        MarkCheckpointAtIndex(racer.NextCheckpoint);
		        }
	        }
        }

        public override void AdvanceTime(double time)
        {
            if (mState == RaceTrackState.Running)
            {
		        foreach (Racer racer in mRacers.EnumerableSet)
                    racer.AdvanceTime(time);
	        }
        }

        public override void PrepareForNewGame()
        {
            // Do nothing
        }

        protected override void ZeroOutFixtures()
        {
            base.ZeroOutFixtures();

            mFinishLineFixture = null;
            mCheckpointFixtures = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        mFinishLine = null;
                        mBuoys = null;
                        mCheckpoints = null;
                        mRacers = null;
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }
        #endregion
    }
}
