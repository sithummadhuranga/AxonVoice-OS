using AgentService.Domain.Interfaces;
using Microsoft.SemanticKernel;

namespace AgentService.Infrastructure.Plugins;

/// <summary>
/// Semantic Kernel plugin for E-Commerce order taking.
/// Mounted when <see cref="Axon.Contracts.Domain.Enums.Purposes.OrderTaking"/> is active.
///
/// Pattern: Strategy — implements <see cref="IAxonPlugin"/> for DI-based plugin mounting.
///
/// All methods are [KernelFunction] decorated so the LLM can invoke them as tools.
/// </summary>
public sealed class OrderManagementPlugin : IAxonPlugin
{
    private readonly ILogger<OrderManagementPlugin> _logger;

    public OrderManagementPlugin(ILogger<OrderManagementPlugin> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public KernelPlugin BuildPlugin() =>
        KernelPluginFactory.CreateFromObject(this, pluginName: "OrderManagement");

    /// <summary>
    /// Checks inventory for a given product SKU.
    /// Called by the LLM when a caller asks about product availability.
    /// </summary>
    [KernelFunction("check_inventory")]
    [System.ComponentModel.Description(
        "Checks if a product is in stock. Returns the available quantity or 0 if out of stock.")]
    public async Task<string> CheckInventoryAsync(
        [System.ComponentModel.Description("The product SKU or name to check")]
        string productName,
        CancellationToken cancellationToken = default)
    {
        // TODO: Inject and query IInventoryRepository (EF Core / pgvector)
        _logger.LogInformation("Checking inventory for product: {Product}", productName);

        // Stub — replace with real DB query
        await Task.Delay(10, cancellationToken);
        return $"Product '{productName}' has 25 units in stock.";
    }

    /// <summary>
    /// Places an order for the caller. Wrapped in a Unit of Work transaction with Outbox.
    /// </summary>
    [KernelFunction("place_order")]
    [System.ComponentModel.Description(
        "Places an order for the customer. Use only after confirming product availability and customer details.")]
    public async Task<string> PlaceOrderAsync(
        [System.ComponentModel.Description("The customer's full name")]
        string customerName,
        [System.ComponentModel.Description("The product SKU or name to order")]
        string productName,
        [System.ComponentModel.Description("The quantity to order")]
        int quantity,
        CancellationToken cancellationToken = default)
    {
        // TODO: Inject IOrderRepository + IUnitOfWork, execute transaction + Outbox write
        _logger.LogInformation(
            "Placing order: Customer={Customer} Product={Product} Qty={Qty}",
            customerName, productName, quantity);

        await Task.Delay(50, cancellationToken);
        return $"Order placed successfully for {customerName}: {quantity}x {productName}. Order ID: {Guid.NewGuid():N}.";
    }
}
