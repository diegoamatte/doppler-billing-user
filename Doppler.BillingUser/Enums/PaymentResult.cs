namespace Doppler.BillingUser.Enums
{
    /// <summary>
    /// The posible results from a payment intend.
    /// </summary>
    public enum PaymentResult
    {
        Successful,
        InvalidCreditCard,
        InvalidExpirationDate,
        InvalidCvv2,
        Failure,
        DoNotHonor,
        Pending
    }
}
