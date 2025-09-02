using System;
using System.Text;

namespace VoxelBusters.EssentialKit.BillingServicesCore
{
    public sealed class BillingPayment : IBillingPayment
    {
        #region IBillingPayment implementation

            public string ProductId { get; private set; }

            public string ProductPlatformId { get; private set; }

            public int Quantity { get; private set; }

            public string Tag { get; private set; }

        #endregion

        #region Constructors

        public BillingPayment(string productId, string productPlatformId, int quantity, string tag)
        {
            // set properties
            ProductId           = productId;
            ProductPlatformId   = productPlatformId;
            Quantity            = quantity;
            Tag                 = tag;
        }

        #endregion

        #region Overriden methods

        public override string ToString()
        {
            var     sb  = new StringBuilder();
            sb.Append("BillingPayment { ");
            sb.Append($"ProductId: {ProductId} ");
            sb.Append($"ProductPlatformId: {ProductPlatformId} ");
            sb.Append($"Quantity: {Quantity} ");
            sb.Append($"Tag: {Tag} ");
            sb.Append("}");
            return sb.ToString();
        }


        #endregion


    }
}