﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Library.API.Auth;

public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthSchemeOptions>
{
    public ApiKeyAuthHandler(
        IOptionsMonitor<ApiKeyAuthSchemeOptions> options, 
        ILoggerFactory logger, UrlEncoder encoder, 
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(HeaderNames.Authorization)) 
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        var header = Request.Headers[HeaderNames.Authorization].ToString();
        if (header != Options.ApiKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "fqanbari919@gmail.com"),
            new Claim(ClaimTypes.Name, "FQanbari")
        };

        var claimsIdentity = new ClaimsIdentity(claims, "ApiKey");

        var ticket = new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(claimsIdentity), Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}