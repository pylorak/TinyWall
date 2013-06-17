using System;
using NetFwTypeLib;

namespace PKSoft.WindowsFirewall
{
    internal sealed class Product
    {
        private INetFwProduct fwProduct;

        internal INetFwProduct Handle
        {
            get { return fwProduct; }
        }
        
        internal Product(INetFwProduct product)
        {
            fwProduct = product;
        }

        internal Product()
        {
            Type tNetFwProduct = Type.GetTypeFromProgID("HNetCfg.FwProduct");
            fwProduct = (INetFwProduct)Activator.CreateInstance(tNetFwProduct);
        }

        internal string DisplayName
        {
            get { return fwProduct.DisplayName; }
            set { fwProduct.DisplayName = value; }
        }

        internal object[] RuleCategories
        {
            get { return (object[])fwProduct.RuleCategories; }
            set { fwProduct.RuleCategories = value; }
        }
    }
}
