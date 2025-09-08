// EYEngage.Core.Application/InterfacesServices/INotificationService.cs
using EYEngage.Core.Application.Services;

namespace EYEngage.Core.Application.InterfacesServices;

public interface INotificationService
{
    Task SendNotificationAsync(NotificationDto notification);
    Task SendBulkNotificationsAsync(BulkNotificationDto notifications);
}