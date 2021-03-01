﻿using Games.LeagueOfLegends.ChampionModules.Common;
using Games.LeagueOfLegends.Model;
using FirelightCore;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Games.LeagueOfLegends
{
    public abstract class GameElementModule : BaseGameModule
    {
        protected delegate void GameStateUpdatedHandler(GameState newState);
        /// <summary>
        /// Raised when the player info was updated.
        /// </summary>
        protected event GameStateUpdatedHandler GameStateUpdated;

        public string Name;

        protected abstract string ModuleTypeName { get; }
        protected string ModuleAnimationPath => $"Animations/LeagueOfLegends/{ModuleTypeName}s/";

        private char lastPressedKey = '\0';

        private MouseKeyboardHook keyboardHook;

        // TODO: Handle champions with cooldown resets?

        protected GameElementModule(string name, GameState gameState, AbilityCastPreference preferredCastMode, bool preloadAllAnimations = false)
            : base(gameState, preferredCastMode)
        {
            Name = name;

            if (preloadAllAnimations)
                PreloadAllAnimations();
        }

        protected string GetAnimationPath(string animationName) => $"{ModuleAnimationPath}{Name}/{animationName}.txt";

        protected void PreloadAnimation(string animationName)
        {
            Animator.PreloadAnimation(GetAnimationPath(animationName));
        }
        protected void PreloadAllAnimations()
        {
            try
            {
                foreach (var file in Directory.GetFiles($"{ModuleAnimationPath}{Name}/"))
                    Animator.PreloadAnimation(file);
            }
            catch (DirectoryNotFoundException)
            {
                Debug.WriteLine("No animations found for " + Name);
            }
        }

        protected void RunAnimationOnce(string animationName, LightZone zones, float fadeoutAfterDuration = 0, bool priority = true, float timeScale = 1)
        {
            Animator.RunAnimationOnce(GetAnimationPath(animationName), zones, fadeoutAfterDuration, priority, timeScale);
        }
        protected void RunAnimationInLoop(string animationName, LightZone zones, float loopDuration, float fadeoutAfterDuration = 0, bool priority = true, float timeScale = 1)
        {
            Animator.RunAnimationInLoop(GetAnimationPath(animationName), zones, loopDuration, fadeoutAfterDuration, priority, timeScale);
        }

        /// <summary>
        /// Dispatches a frame with the given LED data, raising the NewFrameReady event.
        /// </summary>
        protected override void NewFrameReadyHandler(LEDFrame frame)
        {
            //frame.SenderChain.Add(this);
            InvokeNewFrameReady(frame);
        }

        protected void AddInputHandlers()
        {
            //keyboardHook = new KeyboardHook();
            int id = ProcessListenerService.GetProcessId("League of Legends");
            MouseKeyboardHook.GetInstance(id).OnMouseDown += OnMouseDown;
            MouseKeyboardHook.GetInstance(id).OnMouseUp += OnMouseUp;
            MouseKeyboardHook.GetInstance(id).OnKeyPressed += OnKeyPress;
            MouseKeyboardHook.GetInstance(id).OnKeyReleased += OnKeyRelease;
        }

        protected abstract void OnMouseDown(object s, MouseEventArgs e);
        protected abstract void OnMouseUp(object s, MouseEventArgs e);
        protected abstract void OnKeyRelease(object s, KeyEventArgs e);
        protected abstract void OnKeyPress(object s, KeyEventArgs e);

        /// <summary>
        /// Processes a key press. When overriden, the base versions of this virtual function must be called first.
        /// </summary>
        protected virtual void ProcessKeyPress(object s, char keyChar, bool keyUp = false)
        {
            if (keyChar == lastPressedKey && !keyUp)
                return; // prevent duplicate calls. Without this, this gets called every frame a key is pressed.
            lastPressedKey = keyUp ? '\0' : keyChar;
        }

        protected virtual void OnGameStateUpdated(GameState state) { }

        /// <summary>
        /// Updates player info and raises the appropiate events.
        /// </summary>
        public void UpdateGameState(GameState newState)
        {
            GameState = newState;
            GameStateUpdated?.Invoke(newState);
        }

        public override void Dispose()
        {
            Animator?.Dispose();
            if (keyboardHook != null)
            {
                keyboardHook.OnMouseDown -= OnMouseDown;
                keyboardHook.OnKeyPressed -= OnKeyPress;
                keyboardHook.OnKeyReleased -= OnKeyRelease;
                //keyboardHook.Unhook();
            }
        }

        public void StopAnimations()
        {
            Animator.StopCurrentAnimation();
        }
    }
}
