using Microsoft.AspNetCore.SignalR;

namespace SnappersRepairShop.Hubs;

/// <summary>
/// SignalR hub for real-time work order updates
/// </summary>
public class WorkOrderHub : Hub
{
    /// <summary>
    /// Broadcast work order created event to all clients
    /// </summary>
    public async Task WorkOrderCreated(int workOrderId, string workOrderNumber)
    {
        await Clients.All.SendAsync("WorkOrderCreated", workOrderId, workOrderNumber);
    }

    /// <summary>
    /// Broadcast work order updated event to all clients
    /// </summary>
    public async Task WorkOrderUpdated(int workOrderId, string workOrderNumber)
    {
        await Clients.All.SendAsync("WorkOrderUpdated", workOrderId, workOrderNumber);
    }

    /// <summary>
    /// Broadcast work order deleted event to all clients
    /// </summary>
    public async Task WorkOrderDeleted(int workOrderId)
    {
        await Clients.All.SendAsync("WorkOrderDeleted", workOrderId);
    }

    /// <summary>
    /// Broadcast work order status changed event to all clients
    /// </summary>
    public async Task WorkOrderStatusChanged(int workOrderId, string newStatus)
    {
        await Clients.All.SendAsync("WorkOrderStatusChanged", workOrderId, newStatus);
    }
}

