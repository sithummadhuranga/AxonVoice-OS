using Microsoft.SemanticKernel;

namespace AgentService.Domain.Interfaces;

/// <summary>
/// Marker interface for all Semantic Kernel plugins mounted by the Dynamic Intent Engine.
/// Implementing this interface allows the DI container to discover and register plugins
/// without coupling the plugin mounting logic to concrete types.
///
/// Pattern: Strategy — each plugin is a concrete strategy for a specific purpose.
/// </summary>
public interface IAxonPlugin
{
    /// <summary>
    /// The <see cref="KernelPlugin"/> instance built from this plugin's kernel functions.
    /// Used by <see cref="Kernel.Plugins"/> when mounting.
    /// </summary>
    KernelPlugin BuildPlugin();
}
