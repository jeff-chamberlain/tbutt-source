using UnityEngine;
using System.Collections;

namespace TButt
{
    /// <summary>
    /// Keeps track of game state and state change events.
    /// </summary>
    public static class TBGameStateManager
    {
        public delegate void GameStateEvent();

        /// <summary>
        /// Fires whenever the game is paused.
        /// </summary>
        public static GameStateEvent OnPause;
        /// <summary>
        /// Fires whenever the game resumes from pause.
        /// </summary>
        public static GameStateEvent OnResume;

        private static TBGameState state = TBGameState.InGame;
        private static TBGameState primaryState = TBGameState.InGame;
        private static TBGameState returnState = TBGameState.InGame;

        /// <summary>
        /// Changes the TBGameState to the requested state.
        /// </summary>
        public static void ChangeState(TBGameState newState)
        {
            if (newState == state)  // Early out if we're already in the correct state.
                return;

            // If we're coming out of a pause state, but not going to a system menu, fire an OnResume event.
            if ((state == TBGameState.Paused))
                if (OnResume != null)
                    OnResume();

            switch (newState)
            {
                case TBGameState.InGame:
                    primaryState = newState;
                    TBLogging.LogMessage("Switching gamestate to In-Game");
                    break;
                case TBGameState.Paused:
                    primaryState = state;
                    TBLogging.LogMessage("Switching gamestate to Paused");
                    if (OnPause != null)
                        OnPause();
                    break;
            }

            // Apply the new gamestate.
            state = newState;
        }

        /// <summary>
        /// Returns the current TBGameState
        /// </summary>
        /// <returns></returns>
        public static TBGameState GetState()
        {
            return state;
        }
    }

    /// <summary>
    /// List of valid game states.
    /// </summary>
    public enum TBGameState
    {
        InGame,
        Paused
    }
}