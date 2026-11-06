using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    public readonly struct StickState
    {
        public StickState(bool active, Vector2 origin, Vector2 current)
        {
            Active = active;
            Origin = origin;
            Current = current;
        }

        public bool Active { get; }

        public Vector2 Origin { get; }

        public Vector2 Current { get; }
    }

    /// <summary>
    /// Input snapshot consumed by the game session.
    /// </summary>
    public struct InputFrame
    {
        public Vector2 Move;
        public Vector2 Aim;
        public bool FireHeld;
        public bool PausePressed;
        public StickState MoveStick;
        public StickState AimStick;
    }

    public interface IGameInput
    {
        InputFrame Read(Vector2 playerWorldPosition);
    }
}
