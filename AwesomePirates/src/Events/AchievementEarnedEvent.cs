using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    delegate void AchievementEarnedEventHandler(AchievementEarnedEvent ev);

    class AchievementEarnedEvent : SPEvent
    {
        public const string CUST_EVENT_TYPE_ACHIEVEMENT_EARNED = "achievementEarnedEvent";

        public AchievementEarnedEvent(uint bit, int index, bool bubbles = false)
            : base(CUST_EVENT_TYPE_ACHIEVEMENT_EARNED, bubbles)
        {
            mAchievementBit = bit;
            mAchievementIndex = index;
        }

        private uint mAchievementBit;
        private int mAchievementIndex;

        public uint Bit { get { return mAchievementBit; } }
        public int Index { get { return mAchievementIndex; } }

    }
}
