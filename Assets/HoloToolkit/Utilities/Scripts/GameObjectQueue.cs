// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a queue of similar objects that are set inactive when not in use.
/// This allows us to have a dynamically growing and shrinking set of objects 
/// and avoid creating too much garbage.
/// </summary>
public class GameObjectQueue
{
    /// <summary>
    /// The object that will be instantiated.
    /// </summary>
    private GameObject objectPrefab;

    /// <summary>
    /// The transform to set as parent of the object.
    /// </summary>
    private Transform parentTransform;

    /// <summary>
    /// The objects active on the previous update
    /// </summary>
    private Queue<GameObject> activeQueue = new Queue<GameObject>();

    /// <summary>
    /// The objects set inactive on the previous update
    /// </summary>
    private Queue<GameObject> inactiveQueue = new Queue<GameObject>();

    /// <summary>
    /// The objects set as active on the current update.
    /// </summary>
    private Queue<GameObject> nextActiveQueue = new Queue<GameObject>();

    /// <summary>
    /// Manages a queue of similar objects that are set inactive when not in use.
    /// </summary>
    /// <param name="objectPrefab">The object to spawn</param>
    /// <param name="objectParent">The parent transform for the object.</param>
    public GameObjectQueue(GameObject objectPrefab, Transform objectParent)
    {
        this.objectPrefab = objectPrefab;
        this.parentTransform = objectParent;
    }

    /// <summary>
    /// Gets the next object for this frame.
    /// </summary>
    /// <returns>The object to use.</returns>
    public GameObject GetObject()
    {
        GameObject nextObject = null;

        // First pull from the objects already marked active from the previous frame.
        if (activeQueue.Count > 0)
        {
            nextObject = activeQueue.Dequeue();
        }
        // If there are none, then grab one that was marked inactive
        else if (inactiveQueue.Count > 0)
        {
            nextObject = inactiveQueue.Dequeue();
        }
        // If there are still none, make a new object.
        else
        {
            nextObject = GameObject.Instantiate(objectPrefab, parentTransform);
        }

        // Enable the object.
        nextObject.SetActive(true);
        // Remember that we have set it active.
        nextActiveQueue.Enqueue(nextObject);

        return nextObject;
    }

    /// <summary>
    /// Must be called at the end of an update for bookkeeping.
    /// </summary>
    public void EndUpdate()
    {
        // First, if there are still objects in the active queue
        // they didn't get used this frame and must be deactivated
        while (activeQueue.Count > 0)
        {
            GameObject nextButton = activeQueue.Dequeue();
            // So deactivate the object and put it in the inactive queue.
            nextButton.SetActive(false);
            inactiveQueue.Enqueue(nextButton);
        }

        // Then swap our active queue and our next active queue.
        Queue<GameObject> tmp = nextActiveQueue;
        nextActiveQueue = activeQueue;
        activeQueue = tmp;
    }
}
