using System.Runtime.Serialization;

namespace Doppler.BillingUser.Enums
{
    public enum MercadoPagoPaymentStatus
    {
        /// <summary>
        /// The payment has been approved and accredited.
        /// </summary>
        Approved,

        /// <summary>
        /// The user has not yet completed the payment process.
        /// </summary>
        Pending,

        /// <summary>
        /// The payment has been authorized but not captured yet.
        /// </summary>
        Authorized,

        /// <summary>
        /// Payment is being reviewed.
        /// </summary>
        [EnumMember(Value = "In_Process")]
        InProcess,

        /// <summary>
        /// Users have initiated a dispute.
        /// </summary>
        [EnumMember(Value = "In_Mediation")]
        InMediation,

        /// <summary>
        /// Payment was rejected. The user may retry payment.
        /// </summary>
        Rejected,

        /// <summary>
        /// Payment was cancelled by one of the parties or because
        /// time for payment has expired.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Payment was refunded to the user.
        /// </summary>
        Refunded,

        /// <summary>
        /// Was made a chargeback in the buyer’s credit card.
        /// </summary>
        [EnumMember(Value = "Charged_Back")]
        ChargedBack
    }
}
