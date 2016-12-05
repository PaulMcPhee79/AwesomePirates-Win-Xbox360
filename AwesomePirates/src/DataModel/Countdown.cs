using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SparrowXNA;

namespace AwesomePirates
{
    class Countdown : SPEventDispatcher
    {
        public Countdown(int count, int max)
        {
            loop = true;
            counter = count;
            counterMax = max;
            remainder = 0f;
        }

        private bool loop;
        private int counter;
        private int counterMax;
        private float remainder;

        public bool Loop { get { return loop; } set { loop = value; } }
        public int Counter { get { return counter; } set { counter = value; } }
        public int CounterMax { get { return counterMax; } set { counterMax = value; } }
        public float Remainder { get { return remainder; } set { remainder = value; } }

        #region Methods
        public Countdown copy()
        {
            Countdown copy = new Countdown(Counter, CounterMax);
            copy.Loop = Loop;
            copy.Remainder = Remainder;
            return copy;
        }

        public void Decrement()
        {
            if (counter > 0)
            {
                int oldCounter = counter;
                --counter;
                NotifyListenersWithDelta(counter - oldCounter);
        
                if (counter == 0 && loop)
                {
                    counter = counterMax;
                    NotifyListenersWithDelta(counter - oldCounter);
                }
            }
        }

        public void ReduceBy(float amount)
        {
            remainder += amount;
    
            while (remainder > 1)
            {
                remainder -= 1;
                Decrement();
            }
        }

        public void Reset()
        {
            int oldCounter = counter;
            counter = counterMax;
            NotifyListenersWithDelta(counter - oldCounter);
        }

        public void SoftReset()
        {
            counter = counterMax;
            remainder = 0;
        }

        private void NotifyListenersWithDelta(int delta)
        {
            DispatchEvent(new NumericRatioChangedEvent(ThisTurn.CUST_EVENT_TYPE_MUTINY_COUNTDOWN_CHANGED, counter, 0, counterMax, delta));
        }

        #endregion
    }
}
