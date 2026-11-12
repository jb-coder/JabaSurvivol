using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace JabaSurvivol.Security;

public static class JwtAuthenticationExtensions
{
    public static JwtOptions AddJabaJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(options);
        options.Validate();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtClaims.UserName
                };

                jwt.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await ApiProblem.Unauthorized()
                            .ExecuteAsync(context.HttpContext)
                            .ConfigureAwait(false);
                    },
                    OnForbidden = async context =>
                    {
                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await ApiProblem.Forbidden()
                            .ExecuteAsync(context.HttpContext)
                            .ConfigureAwait(false);
                    }
                };
            });

        services.AddAuthorization();
        return options;
    }
}
