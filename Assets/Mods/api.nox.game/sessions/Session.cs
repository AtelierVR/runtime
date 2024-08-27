using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace api.nox.game.sessions
{
    public class Session : IDisposable
    {
        public ushort uid;
        public ushort id;
        public string group;

        public SessionController controller;
        public List<Scene> scenes = new();

        public void Dispose()
        {
            controller.Dispose();
            controller = null;
            scenes.Clear();
        }

        public void SetCurrent() => GameSystem.instance.sessionManager.CurrentSession = this;
    }
}