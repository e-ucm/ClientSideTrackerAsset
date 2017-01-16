/*
 * Copyright 2016 Open University of the Netherlands
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * This project has received funding from the European Union’s Horizon
 * 2020 research and innovation programme under grant agreement No 644187.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Collections;
using AssetPackage;

public class CompletableTracker : TrackerAsset.IGameObjectTracker
{

    private TrackerAsset tracker;

    public void setTracker(TrackerAsset tracker)
    {
        this.tracker = tracker;
    }

    /* COMPLETABLES */

    public enum Completable
    {
        Game,
        Session,
        Level,
        Quest,
        Stage,
        Combat,
        StoryNode,
        Race,
        Completable
    }

    /// <summary>
    /// Player initialized a completable.
    /// </summary>
    /// <param name="completableId">Completable identifier.</param>
    public void Initialized(string completableId)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Initialized),
            Target = new TrackerAsset.TrackerEvent.TraceObject(Completable.Completable.ToString().ToLower(), completableId)
        });
    }

    /// <summary>
    /// Player initialized a completable.
    /// </summary>
    /// <param name="completableId">Completable identifier.</param>
    /// <param name="type">Completable type.</param>
    public void Initialized(string completableId, Completable type)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Initialized),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), completableId)
        });
    }

    /// <summary>
    /// Player progressed a completable.
    /// Type = Completable
    /// </summary>
    /// <param name="completableId">Completable identifier.</param>
    /// <param name="value">New value for the completable's progress.</param>
    public void Progressed(string completableId, float value)
    {
        tracker.setProgress(value);
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Progressed),
            Target = new TrackerAsset.TrackerEvent.TraceObject(Completable.Completable.ToString().ToLower(), completableId)
        });
    }

    /// <summary>
    /// Player progressed a completable.
    /// </summary>
    /// <param name="completableId">Completable identifier.</param>
    /// <param name="value">New value for the completable's progress.</param>
    /// <param name="type">Completable type.</param>
    public void Progressed(string completableId, Completable type, float value)
    {
        tracker.setProgress(value);
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Progressed),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), completableId)
        });
    }

    /// <summary>
    /// Player completed a completable.
    /// Type = Completable
    /// Success = true
    /// Score = 1
    /// </summary>
    /// <param name="completableId">Completable identifier.</param>
    public void Completed(string completableId)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Completed),
            Target = new TrackerAsset.TrackerEvent.TraceObject(Completable.Completable.ToString().ToLower(), completableId),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Success = true,
                Score = 1f
            }
        });
    }

    /// <summary>
    /// Player completed a completable.
    /// Success = true
    /// Score = 1
    /// </summary>
    /// <param name="completableId">Completable identifier.</param>
    /// <param name="type">Completable type.</param>
    public void Completed(string completableId, Completable type)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Completed),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), completableId),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Success = true,
                Score = 1f
            }
        });
    }

    /// <summary>
    /// Player completed a completable.
    /// Score = 1
    /// </summary>
    /// <param name="completableId">Completable identifier.</param>
    /// <param name="type">Completable type.</param>
    /// <param name="success">Completable success.</param>
    public void Completed(string completableId, Completable type, bool success)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Completed),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), completableId),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Success = success,
                Score = 1f
            }
        });
    }

    /// <summary>
    /// Player completed a completable.
    /// </summary>
    /// <param name="completableId">Completable identifier.</param>
    /// <param name="type">Completable type.</param>
    /// <param name="score">Completable score.</param>
    public void Completed(string completableId, Completable type, float score)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Completed),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), completableId),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Score = score
            }
        });
    }

    /// <summary>
    /// Player completed a completable.
    /// </summary>
    /// <param name="completableId">Completable identifier.</param>
    /// <param name="type">Completable type.</param>
    /// <param name="success">Completable success.</param>
    /// <param name="score">Completable score.</param>
    public void Completed(string completableId, Completable type, bool success, float score)
    {
        tracker.Trace(new TrackerAsset.TrackerEvent()
        {
            Event = new TrackerAsset.TrackerEvent.TraceVerb(TrackerAsset.Verb.Completed),
            Target = new TrackerAsset.TrackerEvent.TraceObject(type.ToString().ToLower(), completableId),
            Result = new TrackerAsset.TrackerEvent.TraceResult()
            {
                Success = success,
                Score = score
            }
        });
    }

}
