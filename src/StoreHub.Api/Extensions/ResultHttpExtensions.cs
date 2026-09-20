using Microsoft.AspNetCore.Mvc;
using StoreHub.Application.Common;
using StoreHub.Shared.Api;
using StoreHub.Shared.Results;

namespace StoreHub.Api.Extensions;

public static class ResultHttpExtensions
{
    private static readonly HashSet<string?> NotFoundCodes = new(StringComparer.Ordinal)
    {
        IdentityErrors.UserNotFound,
        IdentityErrors.RoleNotFound,
        IdentityErrors.PermissionNotFound,
        NotificationErrors.NotFound,
        NotificationErrors.TemplateNotFound,
        AuditLogErrors.NotFound,
        StoreErrors.StoreNotFound,
        StoreErrors.UserNotFound,
        CatalogErrors.CategoryNotFound,
        CatalogErrors.ProductNotFound,
        CatalogErrors.DiscountNotFound
    };

    private static readonly HashSet<string?> ConflictCodes = new(StringComparer.Ordinal)
    {
        IdentityErrors.DuplicateUserName,
        IdentityErrors.DuplicateEmail,
        IdentityErrors.DuplicateRoleName,
        NotificationErrors.DuplicateTemplateCode,
        StoreErrors.DuplicateStoreName,
        CatalogErrors.DuplicateBarcode
    };

    private static readonly HashSet<string?> BadRequestCodes = new(StringComparer.Ordinal)
    {
        IdentityErrors.SystemRoleNameImmutable,
        IdentityErrors.SystemRoleCannotDelete,
        NotificationErrors.NoValidUsersInBatch,
        NotificationErrors.NotOwner,
        NotificationErrors.UserNotFound,
        NotificationErrors.UserInactive,
        CatalogErrors.CategoryHasChildren,
        CatalogErrors.CategoryHasProducts,
        CatalogErrors.InvalidParentCategory,
        CatalogErrors.DiscountProductsRequired,
        CatalogErrors.InvalidDiscountPercent
    };

    private static readonly HashSet<string?> UnauthorizedCodes = new(StringComparer.Ordinal)
    {
        IdentityErrors.InvalidCredentials,
        IdentityErrors.UserInactive,
        NotificationErrors.CurrentUserRequired,
        StoreErrors.CurrentUserRequired
    };

    private static readonly HashSet<string?> ForbiddenCodes = new(StringComparer.Ordinal)
    {
        CatalogErrors.StoreAccessDenied
    };

    public static IActionResult ToApiActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        string? traceId)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(ApiResponse<T>.FromSuccess(result.Value!, traceId));
        }

        return result.ToFailureActionResult(controller, traceId);
    }

    public static IActionResult ToFailureActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        string? traceId)
    {
        var errors = result.Errors;
        var payload = ApiResponse<T>.FromFailure(errors, traceId);

        if (NotFoundCodes.Contains(result.FailureCode))
        {
            return controller.NotFound(payload);
        }

        if (ConflictCodes.Contains(result.FailureCode))
        {
            return controller.Conflict(payload);
        }

        if (BadRequestCodes.Contains(result.FailureCode))
        {
            return controller.BadRequest(payload);
        }

        if (ForbiddenCodes.Contains(result.FailureCode))
        {
            return controller.StatusCode(StatusCodes.Status403Forbidden, payload);
        }

        if (UnauthorizedCodes.Contains(result.FailureCode))
        {
            return controller.Unauthorized(payload);
        }

        return controller.BadRequest(payload);
    }

    public static IActionResult ToApiActionResult(
        this Result result,
        ControllerBase controller,
        string? traceId)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(ApiResponse.FromSuccess(traceId));
        }

        return result.ToFailureActionResult(controller, traceId);
    }

    public static IActionResult ToFailureActionResult(
        this Result result,
        ControllerBase controller,
        string? traceId)
    {
        var payload = ApiResponse.FromFailure(result.Errors, traceId);

        if (NotFoundCodes.Contains(result.FailureCode))
        {
            return controller.NotFound(payload);
        }

        if (ConflictCodes.Contains(result.FailureCode))
        {
            return controller.Conflict(payload);
        }

        if (BadRequestCodes.Contains(result.FailureCode))
        {
            return controller.BadRequest(payload);
        }

        if (ForbiddenCodes.Contains(result.FailureCode))
        {
            return controller.StatusCode(StatusCodes.Status403Forbidden, payload);
        }

        if (UnauthorizedCodes.Contains(result.FailureCode))
        {
            return controller.Unauthorized(payload);
        }

        return controller.BadRequest(payload);
    }
}
