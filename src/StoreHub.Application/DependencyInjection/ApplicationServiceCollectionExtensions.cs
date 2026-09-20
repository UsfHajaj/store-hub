using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StoreHub.Application.Features.AuditLogs.Interfaces;
using StoreHub.Application.Features.AuditLogs.Services;
using StoreHub.Application.Features.AuditLogs.Validators;
using StoreHub.Application.Features.Identity.Interfaces;
using StoreHub.Application.Features.Identity.Services;
using StoreHub.Application.Features.Identity.Validators;
using StoreHub.Application.Features.Notifications.Interfaces;
using StoreHub.Application.Features.Notifications.Services;
using StoreHub.Application.Features.Catalog.Interfaces;
using StoreHub.Application.Features.Catalog.Services;
using StoreHub.Application.Features.Inventory.Interfaces;
using StoreHub.Application.Features.Inventory.Services;
using StoreHub.Application.Features.Reports.Interfaces;
using StoreHub.Application.Features.Reports.Services;
using StoreHub.Application.Features.Sales.Interfaces;
using StoreHub.Application.Features.Sales.Services;
using StoreHub.Application.Features.Stores.Interfaces;
using StoreHub.Application.Features.Stores.Services;

namespace StoreHub.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IIdentityDatabaseSeeder, IdentityDatabaseSeeder>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IStocktakeService, StocktakeService>();
        services.AddScoped<IReportService, ReportService>();

        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();

        return services;
    }
}
