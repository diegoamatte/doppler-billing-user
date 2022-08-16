using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Mappers.PaymentStatus
{
    public class PaymentStatusMapper : IPaymentStatusMapper
    {
        public Enums.PaymentStatus MapToPaymentStatus(MercadoPagoPaymentStatus status)
        {
            return status switch
            {
                MercadoPagoPaymentStatus.Approved or MercadoPagoPaymentStatus.Authorized => Enums.PaymentStatus.Approved,
                MercadoPagoPaymentStatus.In_Mediation or MercadoPagoPaymentStatus.In_Process or MercadoPagoPaymentStatus.Pending => Enums.PaymentStatus.Pending,
                _ => Enums.PaymentStatus.DeclinedPaymentTransaction,
            };
        }
    }
}
