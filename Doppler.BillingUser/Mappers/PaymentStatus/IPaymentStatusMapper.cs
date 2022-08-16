using Doppler.BillingUser.Enums;

namespace Doppler.BillingUser.Mappers.PaymentStatus
{
    public interface IPaymentStatusMapper
    {
        Enums.PaymentStatus MapToPaymentStatus(MercadoPagoPaymentStatus status);
    }
}
