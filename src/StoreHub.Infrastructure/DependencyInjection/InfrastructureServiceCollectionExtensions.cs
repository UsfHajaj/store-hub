using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StoreHub.Application.Features.Identity.Interfaces;
using StoreHub.Application.Features.Sales.Interfaces;
using StoreHub.Application.Features.DataImport.Interfaces;
using StoreHub.Application.Features.Reports.Interfaces;
using StoreHub.Infrastructure.Authorization;
using StoreHub.Infrastructure.Excel;
using StoreHub.Infrastructure.Identity;
using StoreHub.Infrastructure.Pdf;
using StoreHub.Infrastructure.Security;
using StoreHub.Shared.Identity;
using StoreHub.Shared.Options;

namespace StoreHub.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<IdentitySeedOptions>(configuration.GetSection(IdentitySeedOptions.SectionName));

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                $"Configuration section '{JwtOptions.SectionName}:SigningKey' must be at least 32 characters.");
        }

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();
        services.AddScoped<IDataImportService, DataImportService>();
        services.AddScoped<IReportExcelExportService, ReportExcelExportService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
                };
            });

        services.AddAuthorization();

        return services;
    }
}
