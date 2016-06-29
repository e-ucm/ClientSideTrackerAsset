using System.Collections;
using AssetPackage;

public class GameObjectTracker : TrackerAsset.IGameObjectTracker
{

    private TrackerAsset tracker;

    public void setTracker(TrackerAsset tracker)
    {
        this.tracker = tracker;
    }


    /* GAMEOBJECT */

    public enum TrackedGameObject
    {
        Enemy,
        Npc,
        Item,
        GameObject
    }

    /// <summary>
    /// Player interacted with a game object.
    /// Type = GameObject 
    /// </summary>
    /// <param name="gameobjectId">Reachable identifier.</param>
    public void Interacted(string gameobjectId)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Interacted),
            Target = new TrackerAsset.TrackerEvent.TraceObject(TrackedGameObject.GameObject.ToString().ToLower(), gameobjectId)
        });
    }

    /// <summary>
    /// Player interacted with a game object.
    /// </summary>
    /// <param name="gameobjectId">TrackedGameObject identifier.</param>
    public void Interacted(string gameobjectId, TrackedGameObject type)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Interacted),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), gameobjectId)
        });
    }

    /// <summary>
    /// Player interacted with a game object.
    /// Type = GameObject 
    /// </summary>
    /// <param name="gameobjectId">Reachable identifier.</param>
    public void Used(string gameobjectId)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Used),
            Target = new TrackerAsset.TrackerEvent.TraceObject(TrackedGameObject.GameObject.ToString().ToLower(), gameobjectId)
        });
    }

    /// <summary>
    /// Player interacted with a game object.
    /// </summary>
    /// <param name="gameobjectId">TrackedGameObject identifier.</param>
    public void Used(string gameobjectId, TrackedGameObject type)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Used),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), gameobjectId)
        });
    }
}
