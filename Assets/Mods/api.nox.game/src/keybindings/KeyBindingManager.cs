using System.Collections.Generic;
using Nox.CCK.Utils;
using UnityEngine.InputSystem;

namespace api.nox.game.keybindings
{
    public class KeyBindingManager : INoxObject
    {
        internal readonly List<KeyBinding> Bindings = new();

        public KeyBinding AddKeyBinding(string id, InputAction action, string category = null)
            => AddKeyBinding(new KeyBinding(id, category, action));

        private KeyBinding AddKeyBinding(KeyBinding binding)
        {
            if (binding == null)
            {
                Logger.LogError("Cannot register a null key binding");
                return null;
            }

            if (string.IsNullOrWhiteSpace(binding.Id))
            {
                Logger.LogError("Cannot register a key binding with an empty id");
                return null;
            }

            if (Bindings.Exists(b => b.Id == binding.Id))
            {
                Logger.LogError($"Key binding with id {binding.Id} already exists");
                return null;
            }


            var config = Config.Load();
            var key = new[] { "settings", "key_bindings", binding.Id };
            if (config.Has(key))
            {
                var action = config.Get<string>(key);
                if (!string.IsNullOrWhiteSpace(action))
                {
                    binding.Action.LoadBindingOverridesFromJson(action);
                    binding.IsOverridden = true;
                }
            }
            
            binding.Action.Enable();
            Bindings.Add(binding);

            GameSystem.Instance.CoreAPI.EventAPI.Emit("key_binding_added", binding);

            return binding;
        }


        public KeyBinding GetKeyBinding(string id)
            => Bindings.Find(b => b.Id == id);

        public void RemoveKeyBinding(string id)
        {
            var binding = GetKeyBinding(id);
            if (binding == null)
            {
                Logger.LogError($"Key binding with id {id} does not exist");
                return;
            }

            var config = Config.Load();
            var key = new[] { "settings", "key_bindings", binding.Id };
            if (binding.IsOverridden)
            {
                var res = binding.Action.SaveBindingOverridesAsJson();
                if (!string.IsNullOrWhiteSpace(res))
                {
                    config.Set(key, res);
                    config.Save();
                }
            }
            else if (config.Has(key))
            {
                config.Remove(key);
                config.Save();
            }

            Bindings.Remove(binding);

            GameSystem.Instance.CoreAPI.EventAPI.Emit("key_binding_removed", binding);
        }


        public bool HasKeyBinding(string id)
            => Bindings.Exists(b => b.Id == id);

        public void Dispose()
        {
            foreach (var binding in Bindings.ToArray())
                RemoveKeyBinding(binding.Id);
        }
    }
}