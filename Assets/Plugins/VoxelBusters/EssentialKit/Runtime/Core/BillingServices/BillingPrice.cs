namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents a billing price with its value, code, symbol, and display text.
    /// </summary>
    /// @ingroup BillingServices
    [System.Serializable]
    public class BillingPrice
    {
        /// <summary>Gets the currency value.</summary>
        public double Value { get; private set; }

        /// <summary>Gets the currency code.</summary>
        public string Code { get; private set; }

        /// <summary>Gets the currency symbol associated with this currency.</summary>
        public string Symbol {get; private set; }

        /// <summary>Gets the displayable text format for this currency.</summary>
        public string LocalizedText { get; private set; }

        public BillingPrice(double value, string currencyCode, string currencySymbol, string localizedText)
        {
            Value = value;
            Code = currencyCode;
            Symbol = currencySymbol;
            LocalizedText = localizedText;
        }

        public override string ToString()
        {
            return $"[Value={Value}, Code={Code}, Symbol={Symbol}, DisplayText={LocalizedText}]";
        }
    }
}