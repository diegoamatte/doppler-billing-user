using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Doppler.BillingUser.Authorization
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly JwtOptions _jwtOptions;
        private readonly SigningCredentials _signingCredentials;

        public JwtTokenGenerator(
            IOptions<JwtOptions> jwtOptions,
            SigningCredentials signingCredentials,
            JwtSecurityTokenHandler tokenHandler)
        {
            _jwtOptions = jwtOptions.Value;
            _signingCredentials = signingCredentials;
            _tokenHandler = tokenHandler;
        }

        public string GenerateSuperUserJwtToken()
        {
            var now = DateTime.UtcNow;
            var payload = new Dictionary<string, object>
            {
                { "isSU", true }
            };

            var jwtToken = _tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Expires = now.AddDays(_jwtOptions.TokenLifeTime),
                SigningCredentials = _signingCredentials,
                Claims = payload
            }) as JwtSecurityToken;

            return _tokenHandler.WriteToken(jwtToken);
        }
    }
}
