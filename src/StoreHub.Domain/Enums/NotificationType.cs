namespace StoreHub.Domain.Enums;

public enum NotificationType : byte
{
    ApprovalAssigned = 1,

    ApprovalSubmitted = 2,

    ApprovalApproved = 3,

    ApprovalRejected = 4,

    General = 99
}
