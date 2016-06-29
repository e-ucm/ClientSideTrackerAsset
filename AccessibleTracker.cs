using System.Collections;
using AssetPackage;

public class AccessibleTracker : TrackerAsset.IGameObjectTracker
{

    private TrackerAsset tracker;

    public void setTracker(TrackerAsset tracker)
    {
        this.tracker = tracker;
    }


    /* ACCESSIBLES */

    public enum Accessible
    {
        Screen,
        Area,
        Zone,
        Cutscene,
        Accessible
    }

    /// <summary>
    /// Player accessed a reachable.
    /// Type = Accessible 
    /// </summary>
    /// <param name="reachableId">Reachable identifier.</param>
    public void Accessed(string reachableId)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Accessed),
            Target = new TrackerAsset.TrackerEvent.TraceObject(Accessible.Accessible.ToString().ToLower(), reachableId)
        });
    }

    /// <summary>
    /// Player accessed a reachable.
    /// </summary>
    /// <param name="reachableId">Reachable identifier.</param>
    /// <param name="type">Reachable type.</param>
    public void Accessed(string reachableId, Accessible type)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Accessed),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), reachableId)
        });
    }

    /// <summary>
    /// Player skipped a reachable.
    /// Type = Accessible
    /// </summary>
    /// <param name="reachableId">Reachable identifier.</param>
    public void Skipped(string reachableId)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Skipped),
            Target = new TrackerAsset.TrackerEvent.TraceObject(Accessible.Accessible.ToString().ToLower(), reachableId)
        });
    }

    /// <summary>
    /// Player skipped a reachable.
    /// </summary>
    /// <param name="reachableId">Reachable identifier.</param>
    /// <param name="type">Reachable type.</param>
    public void Skipped(string reachableId, Accessible type)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Skipped),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), reachableId)
        });
    }


}
