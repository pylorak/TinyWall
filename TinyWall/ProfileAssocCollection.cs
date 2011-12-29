using System;
using System.Collections.ObjectModel;

namespace PKSoft
{
    public class ProfileAssocCollection : Collection<ProfileAssoc>
    {
        public bool Contains(string description)
        {
            foreach (ProfileAssoc app in this)
            {
                if (app.Description == description)
                    return true;
            }
            return false;
        }
        public void Remove(string description)
        {
            foreach (ProfileAssoc app in this)
            {
                if (app.Description == description)
                {
                    this.Remove(app);
                    return;
                }
            }
        }
        public ProfileAssoc Search(string description)
        {
            foreach (ProfileAssoc app in this)
            {
                if (app.Description == description)
                    return app;
            }
            return null;
        }
    }
}
