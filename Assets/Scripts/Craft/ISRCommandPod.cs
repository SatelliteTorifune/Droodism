// Decompiled with JetBrains decompiler
// Type: ModApi.Craft.Parts.ICommandPod
// Assembly: ModApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 3C151665-33A8-41CD-96AE-9CA63C83E16D
// Assembly location: C:\renko\Droodism\Assets\ModTools\Assemblies\ModApi.dll
// XML documentation location: C:\renko\Droodism\Assets\ModTools\Assemblies\ModApi.xml

using ModApi.Automation;
using System.Collections.Generic;
using UnityEngine;

#nullable disable
namespace ModApi.Craft.Parts
{
  /// <summary>Interface for a command pod.</summary>
  public interface ISRCommandPod
  {
    /// <summary>
    /// Occurs when an activation group has changed for this <see cref="T:ModApi.Craft.Parts.ICommandPod" /> instance.
    /// </summary>
    event ActivationGroupChangedHandler ActivationGroupChanged;

    /// <summary>Occurs when the command pod's controls have changed.</summary>
    event ControlsChangedHandler ControlsChanged;

    /// <summary>
    /// Occurs when our <see cref="P:ModApi.Craft.Parts.ICommandPod.IsPlayerControlled" /> value changes.
    /// </summary>
    event CommandPodIsPlayerControlledHandler IsPlayerControlledChanged;

    /// <summary>
    /// Occurs when <see cref="M:ModApi.Craft.Parts.ICommandPod.ActivateStage" /> is called.  The event fires even if the command pod had no stages to activate.
    /// </summary>
    event StageActivatedHandler StageActivated;

    /// <summary>Gets the activation group names.</summary>
    /// <value>The activation group names.</value>
    List<string> ActivationGroupNames { get; }

    /// <summary>Gets the auto-pilot controller.</summary>
    /// <value>The auto-pilot controller.</value>
    IAutoPilot AutoPilot { get; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically recalculate stages in the designer when the
    /// craft's structure changes.
    /// </summary>
    /// <value>
    ///   <c>true</c> if stages should be automatically recalculated; otherwise, <c>false</c>.
    /// </value>
    bool AutoRecalculateStages { get; set; }

    /// <summary>Gets the battery fuel source.</summary>
    /// <value>The battery fuel source.</value>
    IFuelSource BatteryFuelSource { get; }

    /// <summary>Gets the controls.</summary>
    /// <value>The controls.</value>
    CraftControls Controls { get; }

    /// <summary>
    /// Gets the craft configuration associated with this command pod.
    /// </summary>
    /// <value>The craft configuration.</value>
    ICraftConfiguration CraftConfiguration { get; }

    /// <summary>
    /// Gets the current stage number. This is the stage index + 1. A value of zero indicates no
    /// stages have activated yet.
    /// </summary>
    /// <value>The current stage.</value>
    int CurrentStage { get; }

    /// <summary>
    /// Gets the eva script associated with this command pod, or <c>null</c> if there is none.
    /// </summary>
    /// <value>
    /// The eva script associated with this command pod, or <c>null</c> if there is none.
    /// </value>
    IEvaScript EvaScript { get; }

    /// <summary>
    /// Gets a value indicating whether this command pod is contained within an EVA character.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance contained within an EVA character; otherwise, <c>false</c>.
    /// </value>
    bool IsEva { get; }

    /// <summary>
    /// Gets a value indicating whether this command pod is the current player-controlled command pod.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this command pod is the current player-controlled command pod; otherwise, <c>false</c>.
    /// </value>
    bool IsPlayerControlled { get; }

    /// <summary>Gets the jet fuel source.</summary>
    /// <value>The jet fuel source.</value>
    IFuelSource JetFuelSource { get; }

    /// <summary>Gets the mono fuel source.</summary>
    /// <value>The mono fuel source.</value>
    IFuelSource MonoFuelSource { get; }

    IFuelSource OxygenFuelSource{ get; }
    IFuelSource DrinkingWaterFuelSource{ get; }
    IFuelSource FoodFuelSource{ get; }

    /// <summary>Gets the number stages.</summary>
    /// <value>The number stages.</value>
    int NumStages { get; }

    /// <summary>Gets the part.</summary>
    /// <value>The part.</value>
    PartData Part { get; }

    /// <summary>
    /// Gets the pilot seat transform.  Up axis pointing in the upwards direction of the craft, which is the roll axis.
    /// Right is the pitch axis. Forward is the yaw axis.
    /// </summary>
    /// <value>The center of mass transform.</value>
    Transform PilotSeatOrientation { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this command pod will replicate activation groups from the active pod when this command pod isn't active.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this command pod will replicate activation groups from the active command pod when this pod isn't active; otherwise, <c>false</c>.
    /// </value>
    ActivationGroupReplicationMode ReplicateActivationGroups { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this command pod will replicate inputs from the active pod when this command pod isn't active.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this command pod will replicate inputs from the active command pod when this pod isn't active; otherwise, <c>false</c>.
    /// </value>
    bool ReplicateControls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this command pod will replicate stage activations from the active pod when this command pod isn't active.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this command pod will replicate stage activations from the active command pod when this pod isn't active; otherwise, <c>false</c>.
    /// </value>
    bool ReplicateStageActivations { get; set; }

    /// <summary>
    /// Gets or sets the version of stage calculation to use when recalculating the stages for this craft.
    /// </summary>
    /// <value>
    /// The version of stage calculation to use when recalculating the stages for this craft.
    /// </value>
    int StageCalculationVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to suppress the UI message when switching to this command pod.
    /// </summary>
    /// <value>
    ///   <c>true</c> if  the UI message when switching to this command pod should be suppressed; otherwise, <c>false</c>.
    /// </value>
    bool SupressSwitchedToCraftMessage { get; set; }

    /// <summary>Activates the current stage.</summary>
    void ActivateStage();

    /// <summary>Gets the activation group state.</summary>
    /// <param name="activationGroup">The activation group.</param>
    /// <returns>The state of the activation group. True indicates the group is active.</returns>
    bool GetActivationGroupState(int activationGroup);

    /// <summary>Sets the activation group state.</summary>
    /// <param name="activationGroup">The activation group.</param>
    /// <param name="state">if set to <c>true</c> then activate the group. Otherwise deactivate the group.</param>
    void SetActivationGroupState(int activationGroup, bool state);

    /// <summary>
    /// When auto-pilot emulation is enabled, it will make our command pod handle the same as
    /// the specified command pod would with respect to auto pilot and the nav-sphere. Set to <c>null</c> to disable.
    /// </summary>
    /// <param name="commandPodToEmulate">The command pod we want to to emulate auto-pilot/nav-sphere functionality for.  Set to <c>null</c> to clear any override.</param>
    void SetAutopilotEmulation(ISRCommandPod commandPodToEmulate);

    /// <summary>Sets the pilot seat rotation.</summary>
    /// <param name="eulerAngles">The world euler angles.</param>
    /// <param name="updatePartData">If set to <c>true</c> CommandPodData.PilotSeatRotation will be updated.</param>
    void SetPilotSeatRotation(Vector3 eulerAngles, bool updatePartData);
  }
}
