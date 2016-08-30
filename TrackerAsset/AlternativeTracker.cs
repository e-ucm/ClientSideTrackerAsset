using System.Collections;
using AssetPackage;

public class AlternativeTracker : TrackerAsset.IGameObjectTracker
{

    private TrackerAsset tracker;

    public void setTracker(TrackerAsset tracker)
    {
        this.tracker = tracker;
    }

    /* ALTERNATIVES */

    public enum Alternative
    {
        Question,
        Menu,
        Dialog,
        Path,
        Arena,
        Alternative
    }

    /// <summary>
    /// Player selected an option in a presented alternative
    /// Type = Alternative
    /// </summary>
    /// <param name="alternativeId">Alternative identifier.</param>
    /// <param name="optionId">Option identifier.</param>
    public void Selected(string alternativeId, string optionId)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Selected),
            Target = new TrackerAsset.TrackerEvent.TraceObject(Alternative.Alternative.ToString().ToLower(), alternativeId),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Response = optionId
            }
        });
    }

    /// <summary>
    /// Player selected an option in a presented alternative
    /// </summary>
    /// <param name="alternativeId">Alternative identifier.</param>
    /// <param name="optionId">Option identifier.</param>
    /// <param name="type">Alternative type.</param>
    public void Selected(string alternativeId, string optionId, Alternative type)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Selected),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), alternativeId),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Response = optionId
            }
        });
    }

    /// <summary>
    /// Player unlocked an option
    /// Type = Alternative
    /// </summary>
    /// <param name="alternativeId">Alternative identifier.</param>
    /// <param name="optionId">Option identifier.</param>
    public void Unlocked(string alternativeId, string optionId)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Unlocked),
            Target = new TrackerAsset.TrackerEvent.TraceObject(Alternative.Alternative.ToString().ToLower(), alternativeId),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Response = optionId
            }
        });
    }

    /// <summary>
    /// Player unlocked an option
    /// </summary>
    /// <param name="alternativeId">Alternative identifier.</param>
    /// <param name="optionId">Option identifier.</param>
    /// <param name="type">Alternative type.</param>
    public void Unlocked(string alternativeId, string optionId, Alternative type)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Unlocked),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), alternativeId),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Response = optionId
            }
        });
    }

}
