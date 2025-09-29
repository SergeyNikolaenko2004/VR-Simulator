using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static event Action MoveUpPressed;
    public static event Action MoveUpReleased;
    public static event Action MoveDownPressed;
    public static event Action MoveDownReleased;
    public static event Action MoveEastPressed;
    public static event Action MoveEastReleased;
    public static event Action MoveWestPressed;
    public static event Action MoveWestReleased;
    public static event Action MoveNorthPressed;
    public static event Action MoveNorthReleased;
    public static event Action MoveSouthPressed;
    public static event Action MoveSouthReleased;

    public static void PressMoveUp() => MoveUpPressed?.Invoke();
    public static void ReleaseMoveUp() => MoveUpReleased?.Invoke();
    public static void PressMoveDown() => MoveDownPressed?.Invoke();
    public static void ReleaseMoveDown() => MoveDownReleased?.Invoke();
    public static void PressMoveEast() => MoveEastPressed?.Invoke();
    public static void ReleaseMoveEast() => MoveEastReleased?.Invoke();
    public static void PressMoveWest() => MoveWestPressed?.Invoke();
    public static void ReleaseMoveWest() => MoveWestReleased?.Invoke();
    public static void PressMoveNorth() => MoveNorthPressed?.Invoke();
    public static void ReleaseMoveNorth() => MoveNorthReleased?.Invoke();
    public static void PressMoveSouth() => MoveSouthPressed?.Invoke();
    public static void ReleaseMoveSouth() => MoveSouthReleased?.Invoke();
}