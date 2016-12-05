using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SparrowXNA;

namespace AwesomePirates
{
    class TimeKeeper : SPEventDispatcher
    {
        public const int DAY_CYCLE_IN_SEC = 150;
        public const float DAY_CYCLE_IN_MIN = 2.5f;

        private static List<object> s_timeSettings = null;

        private const double kTickInterval = 0.1;
        private const string kDurationKey = "duration";
        private const string kTransitionsKey = "transitions";
        private static readonly uint[] kWaterColors = new uint []
        {
            4946120, 4946120, 6413567, 6413567, 1029351, 1029351, 1029351,
            2718681, 2718681, 4946120, 4946120, 3486351, 3486351, 3486351
        };

        public TimeKeeper(TimeOfDay timeOfDay, float secondsPassed)
        {
            if (s_timeSettings == null)
                TimeKeeper.LoadTimeSettings();

            mDayIntros = new List<string>();
            mDayIntros.Add("Calm Waters");
            mDayIntros.Add("Contested Seas");
            mDayIntros.Add("All Hands on Deck");
            mDayIntros.Add("Ricochet or Die");
            mDayIntros.Add("Dire Straits");
            mDayIntros.Add("Shiver Me Timbers!");
            mDayIntros.Add("Montgomery's Mutiny");

            mTimerActive = false;
            mTransitions = false;
            mDayShouldIncrease = false;
            mDay = 0;
            mTimeOfDay = timeOfDay;
            mBroadcastEvent = null;
            mPeriodModifier = 1;

            mBusyUpdatingClients = false;
            mClients = new Dictionary<object, Action<TimeOfDayChangedEvent>>(5);
            mSubscribeQueue = new Dictionary<object,Action<TimeOfDayChangedEvent>>(5);
            mUnsubscribeQueue = new List<object>(5);

            // Initialize to same shadow offset as Dawn (the first state)
            mShadowOffsetX = mPrevShadowOffsetX = -0.5f;
            mShadowOffsetY = mPrevShadowOffsetY = 0.5f;

            ApplyTimeOfDayChangeWithTimePassed(secondsPassed);
            UpdateShadowOffset();
            BroadcastTimeOfDayChange();
        }

        #region Fields
        private bool mTimerActive;
        private bool mTransitions;
        private bool mDayShouldIncrease;
        private uint mDay;
        private TimeOfDay mTimeOfDay;
        private TimeOfDayChangedEvent mBroadcastEvent;

        private float mPeriodDuration;
        private float mPeriodModifier;
        private float mTimePassed;
        private float mTimePassedToday;

        private float mShadowOffsetX;
        private float mShadowOffsetY;
        private float mPrevShadowOffsetX;
        private float mPrevShadowOffsetY;

        private double mTimeDelta;
        private List<String> mDayIntros;

        protected bool mBusyUpdatingClients;
        private Dictionary<object, Action<TimeOfDayChangedEvent>> mClients;
        private Dictionary<object, Action<TimeOfDayChangedEvent>> mSubscribeQueue;
        private List<object> mUnsubscribeQueue;
        #endregion

        #region Properties
        public bool TimerActive { get { return mTimerActive; } set { mTimerActive = value; UpdateShadowOffset(); } }
        public bool Transitions { get { return mTransitions; } set { mTransitions = value; } }
        public bool DayShouldIncrease { get { return mDayShouldIncrease; } set { mDayShouldIncrease = value; } }
        public uint Day { get { return mDay; } set { mDay = Math.Min(TimeKeeper.MaxDay, value); } }
        public TimeOfDay TimeOfDay
        {
            get { return mTimeOfDay; }
            set
            {
                mTimeOfDay = value;
                ApplyTimeOfDayChangeWithTimePassed(0);
                BroadcastTimeOfDayChange();
            }
        }
        public float PeriodDuration { get { return mPeriodDuration; } }
        public float PeriodModifier
        {
            get { return mPeriodModifier; }
            set
            {
                if (value < 0.001f)
                    return;
                mPeriodDuration *= 1.0f / mPeriodModifier; // Undo previous modifier
                mPeriodDuration *= value; // Apply new modifier
                mPeriodModifier = value;
            }
        }
        public float TimePassed { get { return mTimePassed; } }
        public float TimeRemaining { get { return (float)Math.Max(0.0, mPeriodDuration - mTimePassed - kTickInterval); } } // kTickInterval to prevent tween-fighting
        public float ProportionPassed { get { return Math.Min(1.0f, mTimePassed / mPeriodDuration); } }
        public float ProportionRemaining { get { return Math.Min(1.0f, TimeRemaining / mPeriodDuration); } }
        public float ShadowOffsetX { get { return mShadowOffsetX; } }
        public float ShadowOffsetY { get { return mShadowOffsetY; } }
        public uint WaterColor { get { return kWaterColors[(int)mTimeOfDay]; } }
        public float TimePassedToday { get { return mTimePassedToday; } }
        public float TimeRemainingToday { get { return DAY_CYCLE_IN_SEC - mTimePassedToday; } }
        public static float TimePerDay { get { return DAY_CYCLE_IN_SEC; } }
        public static uint MaxDay { get { return 7; } }
        #endregion

        #region Methods
        public void SetTimeOfDay(TimeOfDay timeOfDay, float secondsPassed)
        {
            mTimeOfDay = timeOfDay;
            ApplyTimeOfDayChangeWithTimePassed(secondsPassed);
            BroadcastTimeOfDayChange();
        }

        private void ApplyTimeOfDayChangeWithTimePassed(float seconds)
        {
            if (mTimeOfDay == TimeOfDay.SunriseTransition && mDayShouldIncrease)
            {
                ++mDay;
                mTimePassedToday = 0;
            }

            mPrevShadowOffsetX = mShadowOffsetX;
            mPrevShadowOffsetY = mShadowOffsetY;

            Dictionary<string, object> settings = TimeKeeper.SettingsForPeriod(mTimeOfDay);
            mPeriodDuration = mPeriodModifier * (Int32)settings[kDurationKey];
            mTransitions = (bool)settings[kTransitionsKey];
            mTimePassed = seconds;
            mTimeDelta = 0;
        }

        public void Subscribe(Action<TimeOfDayChangedEvent> client)
        {
            if (client == null || client.Target == null)
                return;

            if (mBusyUpdatingClients)
            {
                if (!mClients.ContainsKey(client.Target))
                {
                    if (!mSubscribeQueue.ContainsKey(client.Target))
                        mSubscribeQueue.Add(client.Target, client);
                }

                mUnsubscribeQueue.Remove(client.Target);
            }
            else
            {
                if (!mClients.ContainsKey(client.Target))
                    mClients.Add(client.Target, client);
            }
        }

        public void Unsubscibe(object client)
        {
            if (client != null)
            {
                if (mBusyUpdatingClients)
                {
                    if (!mUnsubscribeQueue.Contains(client))
                        mUnsubscribeQueue.Add(client);

                    mSubscribeQueue.Remove(client);
                }
                else
                    mClients.Remove(client);
            }
        }

        public void BroadcastTimeOfDayChange()
        {
            TimeStruct timeState;
            timeState.day = mDay;
            timeState.timeOfDay = mTimeOfDay;
            timeState.transitions = mTransitions;
            timeState.timePassed = mTimePassed;
            timeState.periodDuration = mPeriodDuration;

            if (mBroadcastEvent == null)
                mBroadcastEvent = new TimeOfDayChangedEvent(timeState);
            else
                mBroadcastEvent.OwnerSetState(timeState);

            mBusyUpdatingClients = true;
            foreach (KeyValuePair<object, Action<TimeOfDayChangedEvent>> kvp in mClients)
                kvp.Value(mBroadcastEvent);
            mBusyUpdatingClients = false;

            if (mSubscribeQueue.Count > 0)
            {
                foreach (KeyValuePair<object, Action<TimeOfDayChangedEvent>> kvp in mSubscribeQueue)
                    mClients[kvp.Key] = kvp.Value;
                mSubscribeQueue.Clear();
            }

            if (mUnsubscribeQueue.Count > 0)
            {
                foreach (object client in mUnsubscribeQueue)
                    mClients.Remove(client);
                mUnsubscribeQueue.Clear();
            }
        }

        private void UpdateShadowOffset()
        {
            float proportion = ProportionPassed;

            // Calculation method: prevValue + proportion * (targetValue - prevValue);

            switch (mTimeOfDay)
            {
                case TimeOfDay.NewGameTransition:
                    // Tween to Centered
                    mShadowOffsetX = mPrevShadowOffsetX + proportion * (0 - mPrevShadowOffsetX);
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (0 - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.SunriseTransition:
                    mShadowOffsetX = mPrevShadowOffsetX;
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (-0.5f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.Sunrise:
                    // Tween from Centered to Far N
                    mShadowOffsetX = mPrevShadowOffsetX;
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (-1.0f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.NoonTransition:
                    mShadowOffsetX = mPrevShadowOffsetX;
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (-0.25f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.Noon:
                    mShadowOffsetX = mPrevShadowOffsetX;
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (0.1f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.Afternoon:
                    mShadowOffsetX = mPrevShadowOffsetX;
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (0.5f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.SunsetTransition:
                    mShadowOffsetX = mPrevShadowOffsetX;
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (0.75f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.Sunset:
                    // Tween from S to Far S
                    mShadowOffsetX = mPrevShadowOffsetX;
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (1.0f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.DuskTransition:
                    mShadowOffsetX = mPrevShadowOffsetX + proportion * (-0.25f - mPrevShadowOffsetX);
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (0.75f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.Dusk:
                    // Tween from Far S to SW
                    mShadowOffsetX = mPrevShadowOffsetX + proportion * (-0.5f - mPrevShadowOffsetX);
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (0.5f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.EveningTransition:
                    mShadowOffsetX = mPrevShadowOffsetX + proportion * (-0.75f - mPrevShadowOffsetX);
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (0.75f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.Evening:
                    // Tween from SW to Far SW
                    mShadowOffsetX = mPrevShadowOffsetX + proportion * (-0.9f - mPrevShadowOffsetX);
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (0.9f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.Midnight:
                    // Tween from SW to Far SW
                    mShadowOffsetX = mPrevShadowOffsetX + proportion * (-1.0f - mPrevShadowOffsetX);
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (1.0f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.DawnTransition:
                    mShadowOffsetX = mPrevShadowOffsetX + proportion * (-0.5f - mPrevShadowOffsetX);
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (0.5f - mPrevShadowOffsetY);
                    break;
                case TimeOfDay.Dawn:
                    // Tween from Far SW to Centered
                    mShadowOffsetX = mPrevShadowOffsetX + proportion * (0 - mPrevShadowOffsetX);
                    mShadowOffsetY = mPrevShadowOffsetY + proportion * (0 - mPrevShadowOffsetY);
                    break;
                default:
                    break;
            }
        }

        public string IntroForDay(uint day)
        {
            string intro = null;

            if (day > 0 && day <= mDayIntros.Count)
                intro = mDayIntros[(int)day-1];
            return intro;
        }

        public void AdvanceTime(double time)
        {
            if (!mTimerActive)
                return;

            // Don't advance time of day anymore after day 7 is complete
            if (mDay == TimeKeeper.MaxDay && mTimeOfDay == TimeOfDay.Dusk)
                return;

            mTimeDelta += time;
            mTimePassedToday += (float)time;

            if (mTimeDelta >= kTickInterval)
            {
                while (mTimeDelta >= kTickInterval)
                {
                    mTimeDelta -= kTickInterval;
                    mTimePassed += (float)kTickInterval;
                }

                UpdateShadowOffset();

                if (mTimePassed >= mPeriodDuration)
                {
                    if (mTimeOfDay == TimeOfDay.Dawn)
                        mTimeOfDay = TimeOfDay.SunriseTransition;
                    else
                        ++mTimeOfDay;
                    ApplyTimeOfDayChangeWithTimePassed(0);
                    BroadcastTimeOfDayChange();
                }
            }
        }

        public void Reset()
        {
            mDayShouldIncrease = true;
            PeriodModifier = 1;
            Day = 0;
#if IOS_SCREENS
            SetTimeOfDay(TimeOfDay.Sunrise, 0);
#else
            SetTimeOfDay(TimeOfDay.NewGameTransition, 0);
#endif
        }
        
        private static List<object> LoadTimeSettings()
        {
            if (s_timeSettings == null)
                s_timeSettings = PlistParser.ArrayFromPlist("data/plists/TimeSettings.plist");

            return s_timeSettings;
        }

        public static float DurationForPeriod(TimeOfDay period)
        {
            if (s_timeSettings == null)
                TimeKeeper.LoadTimeSettings();
            float duration = 0.0f;

            if (period >= TimeOfDay.NewGameTransition && period <= TimeOfDay.Dawn)
            {
                Dictionary<string, object> settings = TimeKeeper.SettingsForPeriod(period);
                duration = (Int32)settings[kDurationKey];
            }

            return duration;
        }

        public static bool DoesTimePeriodTransition(TimeOfDay period)
        {
            if (s_timeSettings == null)
                TimeKeeper.LoadTimeSettings();

            bool transitions = false;

            if (period >= TimeOfDay.NewGameTransition && period <= TimeOfDay.Dawn)
            {
                Dictionary<string, object> settings = TimeKeeper.SettingsForPeriod(period);
                transitions = (bool)settings[kTransitionsKey];
            }

            return transitions;
        }

        public static Dictionary<string, object> SettingsForPeriod(TimeOfDay period)
        {
            if (s_timeSettings == null)
                TimeKeeper.LoadTimeSettings();
            return s_timeSettings[(int)period % ((int)TimeOfDay.Dawn + 1)] as Dictionary<string, object>;
        }

        #endregion
    }
}
