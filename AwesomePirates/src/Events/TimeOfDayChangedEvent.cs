using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    delegate void TimeOfDayChangedEventHandler(TimeOfDayChangedEvent ev);

    public enum TimeOfDay
    {
        NewGameTransition = 0,
        SunriseTransition,
        Sunrise,
        NoonTransition,
        Noon,
        Afternoon,
        SunsetTransition,
        Sunset,
        DuskTransition,
        Dusk,
        EveningTransition,
        Evening,
        Midnight,
        DawnTransition,
        Dawn
    }

    struct TimeStruct
    {
        public uint day;
        public TimeOfDay timeOfDay;
        public bool transitions;
        public float timePassed;
        public float periodDuration;
    }

    class TimeOfDayChangedEvent : SPEvent
    {
        public const string CUST_EVENT_TYPE_TIME_OF_DAY_CHANGED = "timeOfDayChangedEvent";

        public TimeOfDayChangedEvent(TimeStruct timeState, bool bubbles = false)
            : base(CUST_EVENT_TYPE_TIME_OF_DAY_CHANGED, bubbles)
        {
            mTimeState = timeState;
        }

        private TimeStruct mTimeState;

        #region Properties
        public uint Day { get { return mTimeState.day; } }
        public TimeOfDay TimeOfDay { get { return mTimeState.timeOfDay; } }
        public bool Transitions { get { return mTimeState.transitions; } }
        public float PeriodDuration { get { return mTimeState.periodDuration; } }
        public float TimePassed { get { return mTimeState.timePassed; } }
        public float TimeRemaining { get { return Math.Max(0.0f, mTimeState.periodDuration - mTimeState.timePassed); } }
        public float ProportionPassed { get { return mTimeState.timePassed / mTimeState.periodDuration; } }
        public float ProportionRemaining { get { return TimeRemaining / mTimeState.periodDuration; } }
        #endregion

        #region Methods
        public void OwnerSetState(TimeStruct timeState)
        {
            mTimeState = timeState;
        }
        #endregion
    }
}
