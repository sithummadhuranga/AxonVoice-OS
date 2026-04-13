using AgentService.Domain.Interfaces;
using Microsoft.SemanticKernel;

namespace AgentService.Infrastructure.Plugins;

/// <summary>
/// Semantic Kernel plugin for clinic appointment booking.
/// Mounted when <see cref="Axon.Contracts.Domain.Enums.Purposes.Booking"/> is active.
/// </summary>
public sealed class CalendarBookingPlugin : IAxonPlugin
{
    private readonly ILogger<CalendarBookingPlugin> _logger;

    public CalendarBookingPlugin(ILogger<CalendarBookingPlugin> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public KernelPlugin BuildPlugin() =>
        KernelPluginFactory.CreateFromObject(this, pluginName: "CalendarBooking");

    /// <summary>Returns available appointment slots for a given date.</summary>
    [KernelFunction("get_available_slots")]
    [System.ComponentModel.Description(
        "Gets available appointment slots for the specified date. Returns a list of time slots.")]
    public async Task<string> GetAvailableSlotsAsync(
        [System.ComponentModel.Description("The date to check, in YYYY-MM-DD format")]
        string date,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking available slots for date: {Date}", date);

        // TODO: Inject ICalendarRepository and query available slots
        await Task.Delay(10, cancellationToken);
        return $"Available slots on {date}: 09:00, 10:30, 14:00, 15:30.";
    }

    /// <summary>Books an appointment slot for the patient.</summary>
    [KernelFunction("book_appointment")]
    [System.ComponentModel.Description(
        "Books an appointment slot. Confirm slot availability first using get_available_slots.")]
    public async Task<string> BookAppointmentAsync(
        [System.ComponentModel.Description("The patient's full name")]
        string patientName,
        [System.ComponentModel.Description("The appointment date in YYYY-MM-DD format")]
        string date,
        [System.ComponentModel.Description("The appointment time in HH:mm format (24h)")]
        string time,
        [System.ComponentModel.Description("Reason for the appointment or type of service")]
        string serviceType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Booking appointment: Patient={Patient} Date={Date} Time={Time} Service={Service}",
            patientName, date, time, serviceType);

        // TODO: Inject IAppointmentRepository + IUnitOfWork, execute transaction + Outbox write
        await Task.Delay(50, cancellationToken);
        return $"Appointment booked for {patientName} on {date} at {time} for {serviceType}. Confirmation: {Guid.NewGuid():N}.";
    }
}
