using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for interactable objects in the game.
/// Objects in game are also mono behaviours, so this interface is implemented as an abstract class.
/// </summary>
public interface IInteractable
{
    void OnInteract();
    void OnDropped();
}
