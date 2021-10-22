using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public class SapBusinessPartner
    {
        public int Id { get; set; }
        public bool IsClientManager { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BillingAddress { get; set; }
        public string CityName { get; set; }
        public int? StateId { get; set; }
        public string CountryCode { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FederalTaxId { get; set; }
        public string FederalTaxType { get; set; } = "CUIT";
        public int IdConsumerType { get; set; }
        public int GroupCode { get; set; }
        public string[] BillingEmails { get; set; }
        public bool Cancelated { get; set; }
        public object SapProperties { get; set; }
        public bool Blocked { get; set; }
        public bool? IsInbound { get; set; }
        public string BillingZip { get; set; }
        public string BillingStateId { get; set; }
        public string BillingCountryCode { get; set; }
        public int PaymentMethod { get; set; }
        public int? PlanType { get; set; }
        public int BillingSystemId { get; set; }
        public int ClientManagerType { get; set; }
        public string County { get; set; }
        public string BillingCity { get; set; }
    }
}
