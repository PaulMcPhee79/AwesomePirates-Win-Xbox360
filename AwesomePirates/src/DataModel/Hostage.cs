using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates
{
    public enum Gender
    {
        Female = 0,
        Male
    }

    class Hostage
    {
        public Hostage(string name)
        {
            mName = name;
            mTextureName = null;
            mGender = Gender.Female;
        }

        protected string mName;
        protected string mTextureName;
        protected Gender mGender;

        public string Name { get { return mName; } }
        public string TextureName { get { return mTextureName; } set { mTextureName = value; } }
        public Gender Gender { get { return mGender; } set { mGender = value; } }
        public virtual int InfamyBonus { get { return 0; } set { } }
    }
}
