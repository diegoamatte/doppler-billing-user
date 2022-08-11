using System.Collections.Generic;

namespace Doppler.BillingUser.Enums
{
    public static class SapDictionary
    {
        public static readonly Dictionary<int, string> StatesDictionary = new()
        {
            //AR
            { 2189, "01" }, // Buenos Aires
            { 2190, "02" }, // Catamarca
            { 2191, "16" }, // Chaco
            { 2192, "17" }, // Chubut
            { 2193, "00" }, // Ciudad Autónoma de Buenos Aires
            { 2194, "04" }, // Corrientes
            { 2195, "03" }, // Córdoba
            { 2196, "05" }, // Entre Ríos
            { 2197, "18" }, // Formosa
            { 2198, "06" }, // Jujuy
            { 2199, "21" }, // La Pampa
            { 2200, "08" }, // La Rioja
            { 2201, "07" }, // Mendoza
            { 2202, "19" }, // Misiones
            { 2203, "20" }, // Neuquén
            { 2204, "22" }, // Río Negro
            { 2205, "09" }, // Salta
            { 2206, "10" }, // San Juan
            { 2207, "11" }, // San Luis
            { 2208, "12" }, // Santa Cruz
            { 2209, "13" }, // Santa Fe
            { 2210, "14" }, // Santiago del Estero
            { 2211, "24" }, // Tierra del Fuego
            { 2212, "15" }, // Tucumán

            //US
            {4828,"AL"}, // Alabama
            {4829,"AK"},  // Alaska
            {4830,"AS"}, // American Samoa
            {4831,"AZ"}, // Arizona
            {4832,"AR"},  // Arkansas
            {4833,"CA"},  // California
            {4834,"CO"},  // Colorado
            {4835,"CT"},  // Connecticut
            {4836,"DE"}, // Delaware
            {4838,"FL"},  // Florida
            {4839,"GA"}, // Georgia
            {4840,"GU"},  // Guam
            {4841,"HI"},  // Hawaii
            {4842,"ID"}, // Idaho
            {4843,"IL"}, // Illinois
            {4844,"IN"}, // Indiana
            {4845,"IA"},  // Iowa
            {4846,"KS"}, // Kansas
            {4847,"KY"}, // Kentucky
            {4848,"LA"}, // Louisiana
            {4849,"ME"}, // Maine
            {4850,"MD"}, // Maryland
            {4851,"MA"}, // Massachusetts
            {4852,"MI"}, // Michigan
            {4853,"AL"}, // Minnesota
            {4854,"MN"},  // Mississippi
            {4855,"MO"}, // Missouri
            {4856,"MT"}, // Montana
            {4857,"NE"},  // Nebraska
            {4858,"NV"},  // Nevada
            {4859,"NH"},  // New Hampshire
            {4860,"NJ"},  // New Jersey
            {4861,"NM"}, // New Mexico
            {4862,"NY"},  // New York
            {4863,"NC"}, // North Carolina
            {4864,"ND"},  // North Dakota
            {4865,"MP"},  // Northern Mariana Islands
            {4866,"OH"}, // Ohio
            {4867,"OK"}, // Oklahoma
            {4868,"OR"}, // Oregon
            {4869,"PA"},  // Pennsylvania
            {4870,"PR"}, // Puerto Rico
            {4871,"RI"}, // Rhode Island
            {4872,"SC"}, // South Carolina
            {4873,"SD"}, // South Dakota
            {4874,"TN"}, // Tennessee
            {4875,"TX"}, // Texas
            {4876,"--"}, // United States Minor Outlying Islands
            {4877,"UT"},  // Utah
            {4878,"VT"}, // Vermont
            {4879,"VI"}, // Virgin Islands, U.S.
            {4880,"VA"},  // Virginia
            {4881,"WA"},  // Washington
            {4882,"WV"},  // West Virginia
            {4883,"WI"},  // Wisconsin
            {4884,"WY"},  // Wyoming
        };
    }
}
