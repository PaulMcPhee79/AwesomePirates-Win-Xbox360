using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SparrowXNA;

namespace AwesomePirates
{
    class VoodooManager : Prop
    {
        public const string CUST_EVENT_TYPE_POWDER_KEG_DROPPING = "powderKegDroppingEvent";
        public const string CUST_EVENT_TYPE_NET_DEPLOYED = "netDeployedEvent";
        public const string CUST_EVENT_TYPE_BRANDY_SLICK_DEPLOYED = "brandySlickDeployedEvent";
        public const string CUST_EVENT_TYPE_TEMPEST_SUMMONED = "tempestSummonedEvent";
        public const string CUST_EVENT_TYPE_WHIRLPOOL_SUMMONED = "whirlpoolSummonedEvent";
        public const string CUST_EVENT_TYPE_HAND_OF_DAVY_SUMMONED = "handOfDavySummonedEvent";
        public const string CUST_EVENT_TYPE_CAMOUFLAGE_ACTIVATED = "camouflageActivatedEvent";
        public const string CUST_EVENT_TYPE_FLYING_DUTCHMAN_ACTIVATED = "flyingDutchmanActivatedEvent";
        public const string CUST_EVENT_TYPE_SEA_OF_LAVA_SUMMONED = "seaOfLavaSummonedEvent";

        public VoodooManager(int category, List<Idol> trinkets, List<Idol> gadgets)
            : base(category)
        {
            mAdvanceable = true;
		    mHibernating = false;
		    mSuspendedMode = false;
		    mCooldownFactor = 1;
		    mTrinkets = trinkets;
		    mGadgets = gadgets;
            mDurations = new VoodooDuration[Idol.IDOL_KEY_COUNT];
            SetupProp();
        }
        
        #region Fields
        private bool mHibernating;
	    private bool mSuspendedMode;
	    private float mCooldownFactor;
	    private VoodooDuration[] mDurations;

	    private VoodooWheel mMenu;
        private List<Idol> mTrinkets;
        private List<Idol> mGadgets;
        #endregion

        #region Methods
        protected override void SetupProp()
        {
            CreateNewMenu(mTrinkets, mGadgets);
            mScene.AddProp(mMenu);
        }

        private void CreateNewMenu(List<Idol> trinkets, List<Idol> gadgets)
        {
            if (mMenu != null)
            {
                UnhookMenuButtons();
                mMenu.RemoveEventListener(VoodooWheel.CUST_EVENT_TYPE_VOODOO_MENU_CLOSING, (SPEventHandler)OnMenuClosePressed);
                mScene.RemoveProp(mMenu);
                mMenu = null;
	        }

            mMenu = new VoodooWheel((int)PFCat.DECK, trinkets, gadgets);
            mMenu.AddEventListener(VoodooWheel.CUST_EVENT_TYPE_VOODOO_MENU_CLOSING, (SPEventHandler)OnMenuClosePressed);
            HookMenuButtons();

            if (mScene.Flipped)
                mMenu.Flip(true);
        }

        public void EnableSuspendedMode(bool enable)
        {
            if (enable)
            {
                UnhookMenuButtons();
                HideMenu();
	        }
            else
            {
                HookMenuButtons();
	        }

	        mSuspendedMode = enable;
        }

        public override void Flip(bool enable)
        {
            mMenu.Flip(enable);
        }

        private void OnMenuClosePressed(SPEvent ev)
        {
            HideMenu();
        }

        public bool VoodooActive(uint voodooID)
        {
            bool active = false;

            switch (voodooID)
            {
                case Idol.GADGET_SPELL_BRANDY_SLICK: active = mDurations[0].active; break;
                case Idol.GADGET_SPELL_TNT_BARRELS: active = mDurations[1].active; break;
                case Idol.GADGET_SPELL_NET: active = mDurations[2].active; break;
                case Idol.GADGET_SPELL_CAMOUFLAGE: active = mDurations[3].active; break;
                case Idol.VOODOO_SPELL_WHIRLPOOL: active = mDurations[4].active; break;
                case Idol.VOODOO_SPELL_TEMPEST: active = mDurations[5].active; break;
                case Idol.VOODOO_SPELL_HAND_OF_DAVY: active = mDurations[6].active; break;
                case Idol.VOODOO_SPELL_FLYING_DUTCHMAN: active = mDurations[7].active; break;
                case Idol.VOODOO_SPELL_SEA_OF_LAVA: active = mDurations[8].active; break;
                default: throw new ArgumentException("Invalid voodoo key in VoodooManager.");
            }

            return active;
        }

        public void SetVoodooActive(uint voodooID, float duration)
        {
            int index = -1;

            switch (voodooID)
            {
                case Idol.GADGET_SPELL_BRANDY_SLICK: index = 0; break;
                case Idol.GADGET_SPELL_TNT_BARRELS: index = 1; break;
                case Idol.GADGET_SPELL_NET: index = 2; break;
                case Idol.GADGET_SPELL_CAMOUFLAGE: index = 3; break;
                case Idol.VOODOO_SPELL_WHIRLPOOL: index = 4; break;
                case Idol.VOODOO_SPELL_TEMPEST: index = 5; break;
                case Idol.VOODOO_SPELL_HAND_OF_DAVY: index = 6; break;
                case Idol.VOODOO_SPELL_FLYING_DUTCHMAN: index = 7; break;
                case Idol.VOODOO_SPELL_SEA_OF_LAVA: index = 8; break;
                default: throw new ArgumentException("Invalid voodoo key in VoodooManager.");
            }

            if (index < 0 || index > Idol.IDOL_KEY_COUNT)
                throw new InvalidOperationException("Invalid voodoo idol index in VoodooManager.");
            mDurations[index].active = true;
            mDurations[index].durationRemaining = duration;
        }

        public double DurationRemainingForID(uint voodooID)
        {
            double durationRemaining = 0;

            switch (voodooID)
            {
                case Idol.GADGET_SPELL_BRANDY_SLICK: durationRemaining = mDurations[0].durationRemaining; break;
                case Idol.GADGET_SPELL_TNT_BARRELS: durationRemaining = mDurations[1].durationRemaining; break;
                case Idol.GADGET_SPELL_NET: durationRemaining = mDurations[2].durationRemaining; break;
                case Idol.GADGET_SPELL_CAMOUFLAGE: durationRemaining = mDurations[3].durationRemaining; break;
                case Idol.VOODOO_SPELL_WHIRLPOOL: durationRemaining = mDurations[4].durationRemaining; break;
                case Idol.VOODOO_SPELL_TEMPEST: durationRemaining = mDurations[5].durationRemaining; break;
                case Idol.VOODOO_SPELL_HAND_OF_DAVY: durationRemaining = mDurations[6].durationRemaining; break;
                case Idol.VOODOO_SPELL_FLYING_DUTCHMAN: durationRemaining = mDurations[7].durationRemaining; break;
                case Idol.VOODOO_SPELL_SEA_OF_LAVA: durationRemaining = mDurations[8].durationRemaining; break;
                default: throw new ArgumentException("Invalid voodoo key in VoodooManager.");
            }

            return durationRemaining;
        }

        private void ResetVoodooDurations()
        {
            for (int i = 0; i < Idol.IDOL_KEY_COUNT; ++i)
            {
                mDurations[i].active = false;
                mDurations[i].durationRemaining = 0;
            }
        }

        public void PrepareForNewGame()
        {
            CreateNewMenu(mTrinkets, mGadgets);
            mScene.AddProp(mMenu);
            ResetVoodooDurations();
            mHibernating = false;
        }

        public void PrepareForGameOver()
        {
            if (mHibernating)
		        return;
            mScene.RemoveProp(mMenu, false);
	        mMenu.Visible = false;
	        mHibernating = true;
        }

        private int IndexForKey(uint key)
        {
            int index = -1;

            switch (key)
            {
                case Idol.GADGET_SPELL_BRANDY_SLICK: index = 0; break;
                case Idol.GADGET_SPELL_TNT_BARRELS: index = 1; break;
                case Idol.GADGET_SPELL_NET: index = 2; break;
                case Idol.GADGET_SPELL_CAMOUFLAGE: index = 3; break;
                case Idol.VOODOO_SPELL_WHIRLPOOL: index = 4; break;
                case Idol.VOODOO_SPELL_TEMPEST: index = 5; break;
                case Idol.VOODOO_SPELL_HAND_OF_DAVY: index = 6; break;
                case Idol.VOODOO_SPELL_FLYING_DUTCHMAN: index = 7; break;
                case Idol.VOODOO_SPELL_SEA_OF_LAVA: index = 8; break;
            }
            return index;
        }

        private uint KeyForIndex(int index)
        {
            uint key = 0;

            switch (index)
            {
                case 0: key = Idol.GADGET_SPELL_BRANDY_SLICK; break;
                case 1: key = Idol.GADGET_SPELL_TNT_BARRELS; break;
                case 2: key = Idol.GADGET_SPELL_NET; break;
                case 3: key = Idol.GADGET_SPELL_CAMOUFLAGE; break;
                case 4: key = Idol.VOODOO_SPELL_WHIRLPOOL; break;
                case 5: key = Idol.VOODOO_SPELL_TEMPEST; break;
                case 6: key = Idol.VOODOO_SPELL_HAND_OF_DAVY; break;
                case 7: key = Idol.VOODOO_SPELL_FLYING_DUTCHMAN; break;
                case 8: key = Idol.VOODOO_SPELL_SEA_OF_LAVA; break;
            }
            return key;
        }

        public void OnAbilityActivated(SPEvent ev)
        {
            VoodooDial dial = ev.Target as VoodooDial;

            if (dial != null)
            {
                int index = IndexForKey(dial.Tag);

                if (index != -1 && !mDurations[index].active)
                    ActivateItemWithKey(dial.Tag);
            }
        }

        private void ActivateItemWithKey(uint key)
        {
            string eventKey = null;
	        //double cooldown = 0;
	        int index = IndexForKey(key);
	
	        if (index == -1)
                throw new ArgumentException("Invalid index activated in VoodooManager.");
	
	        //Enhancements *enhancements = mScene.enhancements;
	        Idol idol = mScene.IdolForKey(key);
	        double idolDuration = (idol != null) ? Idol.DurationForIdol(idol) : 0;
	        //cooldown = [Idol cooldownDurationForIdol:idol];
	
	        switch (key)
            {
		        case Idol.GADGET_SPELL_BRANDY_SLICK:
			        mDurations[index].active = true;
                    mDurations[index].durationRemaining = idolDuration;
			        //mDurations[index].durationRemaining = idolDuration * [enhancements functionalFactorForEnhancement:ENHANCE_DEN_ONE_FOR_THE_ROAD byCategory:ENHANCE_CAT_DEN];
			        eventKey = CUST_EVENT_TYPE_BRANDY_SLICK_DEPLOYED;
			        break;
		        case Idol.GADGET_SPELL_TNT_BARRELS:
			        eventKey = CUST_EVENT_TYPE_POWDER_KEG_DROPPING;
			        break;
		        case Idol.GADGET_SPELL_NET:
			        mDurations[index].active = true;
			        mDurations[index].durationRemaining = idolDuration;
			        eventKey = CUST_EVENT_TYPE_NET_DEPLOYED;
			        break;
		        case Idol.GADGET_SPELL_CAMOUFLAGE:
			        mDurations[index].active = true;
			        mDurations[index].durationRemaining = idolDuration;
			        eventKey = CUST_EVENT_TYPE_CAMOUFLAGE_ACTIVATED;
			        break;
		        case Idol.VOODOO_SPELL_WHIRLPOOL:
			        mDurations[index].active = true;
                    mDurations[index].durationRemaining = idolDuration;
			        //mDurations[index].durationRemaining = idolDuration; * [enhancements functionalFactorForEnhancement:ENHANCE_HAUNT_ABYSSAL_MAW byCategory:ENHANCE_CAT_HAUNT];
			        eventKey = CUST_EVENT_TYPE_WHIRLPOOL_SUMMONED;
			        break;
		        case Idol.VOODOO_SPELL_TEMPEST:
			        mDurations[index].active = true;
			        mDurations[index].durationRemaining = idolDuration;
			        eventKey = CUST_EVENT_TYPE_TEMPEST_SUMMONED;
			        break;
		        case Idol.VOODOO_SPELL_HAND_OF_DAVY:
			        mDurations[index].active = true;
			        mDurations[index].durationRemaining = idolDuration;
			        eventKey = CUST_EVENT_TYPE_HAND_OF_DAVY_SUMMONED;
			        break;
		        case Idol.VOODOO_SPELL_FLYING_DUTCHMAN:
			        mDurations[index].active = true;
			        mDurations[index].durationRemaining = idolDuration;
			        eventKey = CUST_EVENT_TYPE_FLYING_DUTCHMAN_ACTIVATED;
			        break;
                case Idol.VOODOO_SPELL_SEA_OF_LAVA:
                    mDurations[index].active = true;
			        mDurations[index].durationRemaining = idolDuration;
			        eventKey = CUST_EVENT_TYPE_SEA_OF_LAVA_SUMMONED;
                    break;
		        default:
			        return;
	        }
	
	        //cooldown *= mCooldownFactor;
	
	        //GameController *gc = [GameController GC];
	        //[gc.gameStats setVoodooCooldown:cooldown key:key];
	        //[gc.gameStats setAllVoodooCooldowns:GLOBAL_VOODOO_TIMEOUT];
            mMenu.EnableItem(false, key);
    
            if (Idol.IsMunition(key))
                mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_MUNITION_USED, key);
            else if (Idol.IsSpell(key))
                mScene.ObjectivesManager.ProgressObjectiveWithEventType(ObjectivesManager.OBJ_TYPE_SPELL_USED, key);
    
	        //[self refreshCooldowns];
            DispatchEvent(SPEvent.SPEventWithType(eventKey));
        }

        public override void AdvanceTime(double time)
        {
            if (mHibernating)
                return;
            for (int i = 0; i < Idol.IDOL_KEY_COUNT; ++i)
            {
                if (mDurations[i].active)
                {
                    mDurations[i].durationRemaining = Math.Max(0, mDurations[i].durationRemaining - time);

                    if (SPMacros.SP_IS_DOUBLE_EQUAL(0, mDurations[i].durationRemaining))
                        mDurations[i].active = false;
                }
            }

            //[GCTRL.gameStats advanceVoodooCooldowns:time];
        }

        public void BubbleMenuToTop()
        {
            mScene.RemoveProp(mMenu, false);
            mScene.AddProp(mMenu);
        }

        public void ShowMenu()
        {
            ShowMenuAt(mScene.ViewWidth / 2, mScene.ViewHeight / 2);
        }

        public void ShowMenuAt(float x, float y)
        {
            mMenu.ShowAt(x, y);
        }

        public void HideMenu()
        {
            mMenu.Hide();
            DispatchEvent(SPEvent.SPEventWithType(VoodooWheel.CUST_EVENT_TYPE_VOODOO_MENU_CLOSING));
        }

        private void HookMenuButtons()
        {
            if (mMenu == null)
		        return;

            mMenu.HookDialButtons();

            foreach (Idol trinket in mTrinkets)
            {
                VoodooDial dial = mMenu.DialForKey(trinket.Key);
                if (dial != null) dial.AddActionEventListener(VoodooDial.CUST_EVENT_TYPE_VOODOO_DIAL_PRESSED, new Action<SPEvent>(OnAbilityActivated));
            }

            foreach (Idol gadget in mGadgets)
            {
                VoodooDial dial = mMenu.DialForKey(gadget.Key);
                if (dial != null) dial.AddActionEventListener(VoodooDial.CUST_EVENT_TYPE_VOODOO_DIAL_PRESSED, new Action<SPEvent>(OnAbilityActivated));
            }
        }

        private void UnhookMenuButtons()
        {
            if (mMenu == null)
                return;

            mMenu.UnhookDialButtons();

            foreach (Idol trinket in mTrinkets)
            {
                VoodooDial dial = mMenu.DialForKey(trinket.Key);
                if (dial != null) dial.RemoveEventListener(VoodooDial.CUST_EVENT_TYPE_VOODOO_DIAL_PRESSED);
            }

            foreach (Idol gadget in mGadgets)
            {
                VoodooDial dial = mMenu.DialForKey(gadget.Key);
                if (dial != null) dial.RemoveEventListener(VoodooDial.CUST_EVENT_TYPE_VOODOO_DIAL_PRESSED);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (mMenu != null)
                        {
                            UnhookMenuButtons();
                            mScene.RemoveProp(mMenu);
                            mMenu = null;
                        }

                        mTrinkets = null;
                        mGadgets = null;
                    }
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
