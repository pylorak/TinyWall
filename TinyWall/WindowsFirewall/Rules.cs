using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NetFwTypeLib;

namespace PKSoft.WindowsFirewall
{
    /// <summary>
    /// The Read only list of  <see cref="Rule"/> objects.
    /// </summary>
    internal class Rules : Collection<Rule>
    {
        private INetFwRules FwRules;

        internal Rules(INetFwRules rules, bool ignoreExistingRules)
            : base(ignoreExistingRules ? new List<Rule>() : RulesToList(rules))
        {
            FwRules = rules;
        }

        private static IList<Rule> RulesToList(INetFwRules rules)
        {
            List<Rule> list = new List<Rule>(rules.Count);
            foreach (INetFwRule currentFwRule in rules)
                list.Add(new Rule(currentFwRule));
            return list;
        }

        protected override void ClearItems()
        {
            foreach (Rule rule in base.Items)
                FwRules.Remove(rule.Name);

            base.ClearItems();
        }

        protected override void InsertItem(int index, Rule item)
        {
#if DEBUG
            for (int i = 0; i < this.Count; ++i)
            {
                if (this[i].Name.Equals(item.Name))
                    throw new InvalidOperationException("Rule name must be unique.");
                if (this[i].Handle == item.Handle)
                    throw new InvalidOperationException("Rule is already in the collection.");
            }
#endif

            FwRules.Add(item.Handle);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            FwRules.Remove(base.Items[index].Name);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, Rule item)
        {
            this.RemoveItem(index);
            this.InsertItem(index, item);
        }

        internal void Add(List<Rule> ruleset, ref List<Rule> failedRules)
        {
            for (int i = 0; i < ruleset.Count; ++i)
            {
                try
                {
                    Add(ruleset[i]);
                }
                catch
                {
                    failedRules.Add(ruleset[i]);
                }
            }
        }

        internal void DisableAllRules()
        {
            // Note: This method is very slow, it is a performance bottleneck.
            // However, the actual bottleneck is disabling a single firewall rule
            // in unmanaged code. Replacing this method by a fully unmanaged
            // implementation only brings a 0.2% advantage. So the problem lies not
            // in the managed world or in interop. There is nothing we can do,
            // except for trying to avoid calling this method.

            for (int i = 0; i < this.Count; ++i)
                this[i].Enabled = false;
        }
    }
}
