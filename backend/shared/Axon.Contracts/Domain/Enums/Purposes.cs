namespace Axon.Contracts.Domain.Enums;

/// <summary>
/// Defines the business purposes an AxonVoice agent can be configured for.
/// Used by the Dynamic Intent Engine to mount appropriate Semantic Kernel plugins.
/// </summary>
public enum Purposes
{
    /// <summary>E-commerce order taking and inventory management.</summary>
    OrderTaking = 1,

    /// <summary>Clinic/appointment booking via calendar APIs.</summary>
    Booking = 2,

    /// <summary>General FAQ and knowledge-base Q&amp;A.</summary>
    GeneralFaq = 3,

    /// <summary>Custom tenant-defined purpose using webhook plugins.</summary>
    CustomWebhook = 99
}
