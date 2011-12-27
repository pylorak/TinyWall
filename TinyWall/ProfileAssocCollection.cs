using System;
using System.Collections.ObjectModel;

namespace PKSoft
{
    public class ProfileAssocCollection : Collection<ProfileAssoc>
    {
        public bool Contains(string desc)
        {
            foreach (ProfileAssoc app in this)
            {
                if (app.Description == desc)
                    return true;
            }
            return false;
        }
        public void Remove(string desc)
        {
            foreach (ProfileAssoc app in this)
            {
                if (app.Description == desc)
                {
                    this.Remove(app);
                    return;
                }
            }
        }
        public ProfileAssoc Search(string desc)
        {
            foreach (ProfileAssoc app in this)
            {
                if (app.Description == desc)
                    return app;
            }
            return null;
        }
    }
}
