namespace Doppler.BillingUser.Enums
{
    public enum ApplicationErrorCode
    {
        /// <summary>
        /// The payment transaction is duplicated, the OrderId has been used previously.
        /// </summary>
        DuplicatedPaymentTransaction,
        /// <summary>
        /// The payment transaction was declined because of the processor rejected the transaction, for example, for insufficient funds.
        /// </summary>
        DeclinedPaymentTransaction,
        DoNotHonorPaymentResponse,
        /// <summary>
        /// The payment gateway has found an error with the submitted transaction.
        /// </summary>
        ClientPaymentTransactionError,
        /// <summary>
        /// The payment gateway has failed to process the transaction due to an internal system error.
        /// Contact payment gateway support to resolve the problem.
        /// </summary>
        ServerPaymentTransactionError,
        /// <summary>
        /// Fraud detected in the transaction by the payment gateway.
        /// </summary>
        FraudPaymentTransaction,

        InexistentUser,
        InexistentBillingCredit,
        InvalidBilling,
        InexistentPromotion,

        CreateAgreementUserUpdateError
    }
}
