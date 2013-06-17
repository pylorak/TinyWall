using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NetFwTypeLib;

namespace PKSoft.WindowsFirewall
{
    internal sealed class Products : Collection<Product>
    {
        private INetFwProducts fwProducts;

        internal static Products Init()
        {
            Type tNetFwProducts = Type.GetTypeFromProgID("HNetCfg.FwProducts");
            INetFwProducts fwProducts = (INetFwProducts)Activator.CreateInstance(tNetFwProducts);

            return new Products(fwProducts);
        }

        private Products(INetFwProducts products)
            : base(ProductsToList(products))
        {
            fwProducts = products;
        }

        private static IList<Product> ProductsToList(INetFwProducts products)
        {
            List<Product> list = new List<Product>(products.Count);
            foreach (INetFwProduct currentFwProduct in products)
                list.Add(new Product(currentFwProduct));
            return list;
        }

        protected override void ClearItems()
        {
            throw new NotSupportedException();
        }

        protected override void InsertItem(int index, Product item)
        {
            throw new NotSupportedException();
        }

        protected override void RemoveItem(int index)
        {
            throw new NotSupportedException();
        }

        protected override void SetItem(int index, Product item)
        {
            throw new NotSupportedException();
        }

        // Returns an opaque object. As long as the returned object is alive,
        // the product will stay registered.
        internal object Register(Product item)
        {
            base.InsertItem(0, item);
            return this.fwProducts.Register(item.Handle);
        }
    }
}
